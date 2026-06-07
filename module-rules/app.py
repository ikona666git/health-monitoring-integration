from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, Dict
from datetime import datetime, timedelta

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Кэш для норм
norms_cache = {}
CACHE_TTL_SECONDS = 300

def get_cached_norms(user_id, metric_type):
    key = f"{user_id}:{metric_type}"
    if key in norms_cache:
        value, timestamp = norms_cache[key]
        if datetime.now() - timestamp < timedelta(seconds=CACHE_TTL_SECONDS):
            return value
        else:
            del norms_cache[key]
    return None

def set_cached_norms(user_id, metric_type, min_val, max_val):
    key = f"{user_id}:{metric_type}"
    norms_cache[key] = ((min_val, max_val), datetime.now())

history = []

class Measurement(BaseModel):
    id: Optional[int] = None
    user_id: str
    metric_type: str
    value: float
    timestamp: str

@app.post('/check')
def check(m: Measurement):
    # Проверяем кэш
    cached = get_cached_norms(m.user_id, m.metric_type)
    if cached:
        min_norm, max_norm = cached
        cached_str = "да"
    else:
        min_norm = 60
        max_norm = 100
        set_cached_norms(m.user_id, m.metric_type, min_norm, max_norm)
        cached_str = "нет"
    
    out = m.value < min_norm or m.value > max_norm
    dev = None
    if out:
        if m.value < min_norm:
            dev = round(((min_norm - m.value) / min_norm) * 100, 2)
        else:
            dev = round(((m.value - max_norm) / max_norm) * 100, 2)
    
    res = {
        'user_id': m.user_id,
        'metric_type': m.metric_type,
        'value': m.value,
        'min_normal': min_norm,
        'max_normal': max_norm,
        'is_out_of_range': out,
        'deviation_percent': dev,
        'alert_triggered': out,
        'cached': cached_str
    }
    history.append(res)
    return res

@app.get('/history')
def get_history():
    return history

@app.get('/health')
def health():
    return {'status': 'ok'}