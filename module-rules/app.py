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

history = []

class Measurement(BaseModel):
    id: Optional[int] = None
    user_id: str
    metric_type: str
    value: float
    timestamp: str

@app.post('/check')
def check(m: Measurement):
    min_norm = 60
    max_norm = 100
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
        'alert_triggered': out
    }
    history.append(res)
    return res

@app.get('/history')
def get_history():
    return history

@app.get('/health')
def health():
    return {'status': 'ok'}