from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

norms_db = {}
history = []

DEFAULT = {
    'heart_rate': {'min': 60, 'max': 100},
    'glucose': {'min': 70, 'max': 140},
    'steps': {'min': 0, 'max': 25000}
}

class Measurement(BaseModel):
    id: Optional[int] = None
    user_id: str
    metric_type: str
    value: float
    timestamp: str

@app.post('/check')
def check(m: Measurement):
    norms = norms_db.get(m.user_id, {}).get(m.metric_type, DEFAULT.get(m.metric_type, {'min': 0, 'max': 999999}))
    out = m.value < norms['min'] or m.value > norms['max']
    dev = None
    if out:
        if m.value < norms['min']:
            dev = round(((norms['min'] - m.value) / norms['min']) * 100, 2)
        else:
            dev = round(((m.value - norms['max']) / norms['max']) * 100, 2)
    res = {
        'measurement_id': m.id,
        'user_id': m.user_id,
        'metric_type': m.metric_type,
        'value': m.value,
        'min_normal': norms['min'],
        'max_normal': norms['max'],
        'is_out_of_range': out,
        'deviation_percent': dev,
        'alert_triggered': out
    }
    history.append(res)
    return res

@app.get('/health')
def health():
    return {'status': 'ok'}