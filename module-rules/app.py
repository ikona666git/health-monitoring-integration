from fastapi import FastAPI
from pydantic import BaseModel
from typing import Dict, Optional

app = FastAPI()

norms_db = {}
history = []

DEFAULT = {
    'heart_rate': {'min':60,'max':100},
    'glucose': {'min':70,'max':140},
    'steps': {'min':0,'max':25000}
}

class Measurement(BaseModel):
    id: Optional[int]=None
    user_id: str
    metric_type: str
    value: float
    timestamp: str

@app.post('/check')
def check(m: Measurement):
    norms = norms_db.get(m.user_id, {}).get(m.metric_type, DEFAULT.get(m.metric_type, {'min':0,'max':999999}))
    out = m.value < norms['min'] or m.value > norms['max']
    dev = None
    if out:
        if m.value < norms['min']:
            dev = round(((norms['min']-m.value)/norms['min'])*100,2)
        else:
            dev = round(((m.value-norms['max'])/norms['max'])*100,2)
    res = {
        'measurement_id':m.id,
        'user_id':m.user_id,
        'metric_type':m.metric_type,
        'value':m.value,
        'min_normal':norms['min'],
        'max_normal':norms['max'],
        'is_out_of_range':out,
        'deviation_percent':dev,
        'alert_triggered':out
    }
    history.append(res)
    return res

@app.get('/norms/{user_id}')
def get_norms(user_id: str):
    return norms_db.get(user_id, {})

@app.put('/norms/{user_id}')
def set_norms(user_id: str, metric: str, min_val: float, max_val: float):
    if user_id not in norms_db:
        norms_db[user_id] = {}
    norms_db[user_id][metric] = {'min':min_val,'max':max_val}
    return {'status':'updated'}

@app.get('/history')
def get_history(user_id: Optional[str] = None):
    if user_id:
        return [h for h in history if h['user_id'] == user_id]
    return history

@app.get('/health')
def health():
    return {'status':'ok'}
