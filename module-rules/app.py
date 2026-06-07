from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import RedirectResponse
from pydantic import BaseModel, Field
from typing import Optional, List
from datetime import datetime, timedelta

app = FastAPI(
    title='Health Monitoring — Rules Engine API',
    description='Проверка показателей по нормам и хранение истории проверок.',
    version='1.0.0',
    docs_url='/docs',
    redoc_url='/redoc',
    openapi_url='/openapi.json',
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=['*'],
    allow_credentials=True,
    allow_methods=['*'],
    allow_headers=['*'],
)

norms_cache = {}
CACHE_TTL_SECONDS = 300
history = []


def get_cached_norms(user_id, metric_type):
    key = f'{user_id}:{metric_type}'
    if key in norms_cache:
        value, timestamp = norms_cache[key]
        if datetime.now() - timestamp < timedelta(seconds=CACHE_TTL_SECONDS):
            return value
        del norms_cache[key]
    return None


def set_cached_norms(user_id, metric_type, min_val, max_val):
    key = f'{user_id}:{metric_type}'
    norms_cache[key] = ((min_val, max_val), datetime.now())


class Measurement(BaseModel):
    id: Optional[int] = Field(None, description='Идентификатор измерения')
    user_id: str = Field(..., example='user123', description='Идентификатор пользователя')
    metric_type: str = Field(..., example='heart_rate', description='Тип показателя')
    value: float = Field(..., example=135, description='Значение показателя')
    timestamp: Optional[str] = Field(None, example='2026-06-08T12:00:00Z', description='Время измерения')


class CheckResult(BaseModel):
    user_id: str
    metric_type: str
    value: float
    min_normal: float
    max_normal: float
    is_out_of_range: bool
    deviation_percent: Optional[float]
    alert_triggered: bool
    cached: str


class HealthResponse(BaseModel):
    status: str = Field(..., example='ok')


@app.get('/swagger', include_in_schema=False)
def swagger_redirect():
    return RedirectResponse(url='/docs')


@app.post('/check', response_model=CheckResult, tags=['Rules'])
def check(m: Measurement):
    """Проверить показатель по нормам (60–100 по умолчанию)."""
    cached = get_cached_norms(m.user_id, m.metric_type)
    if cached:
        min_norm, max_norm = cached
        cached_str = 'да'
    else:
        min_norm = 60
        max_norm = 100
        set_cached_norms(m.user_id, m.metric_type, min_norm, max_norm)
        cached_str = 'нет'

    out = m.value < min_norm or m.value > max_norm
    dev = None
    if out:
        if m.value < min_norm:
            dev = round(((min_norm - m.value) / min_norm) * 100, 2)
        else:
            dev = round(((m.value - max_norm) / max_norm) * 100, 2)

    res = CheckResult(
        user_id=m.user_id,
        metric_type=m.metric_type,
        value=m.value,
        min_normal=min_norm,
        max_normal=max_norm,
        is_out_of_range=out,
        deviation_percent=dev,
        alert_triggered=out,
        cached=cached_str,
    )
    history.append(res.model_dump())
    return res


@app.get('/history', response_model=List[CheckResult], tags=['History'])
def get_history():
    """Получить историю всех проверок норм."""
    return history


@app.get('/health', response_model=HealthResponse, tags=['Health'])
def health():
    """Проверка состояния сервиса."""
    return HealthResponse(status='ok')
