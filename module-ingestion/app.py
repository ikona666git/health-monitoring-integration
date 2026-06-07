from flask import Flask, request, jsonify
from datetime import datetime
import requests
import os

app = Flask(__name__)

RULES_ENGINE_URL = os.getenv('RULES_ENGINE_URL', 'http://localhost:5002')
received = []

@app.route('/measurements', methods=['POST'])
def add():
    data = request.get_json()
    if not all(k in data for k in ['user_id','metric_type','value','timestamp']):
        return jsonify({'error':'missing fields'}),400
    m = {
        'id': len(received)+1,
        'user_id': data['user_id'],
        'metric_type': data['metric_type'],
        'value': data['value'],
        'timestamp': data['timestamp'],
        'received_at': datetime.now().isoformat()
    }
    received.append(m)
    try:
        r = requests.post(f"{RULES_ENGINE_URL}/check", json=m, timeout=3)
        result = r.json()
    except:
        result = {'error':'rules engine unreachable'}
    return jsonify({'status':'accepted','id':m['id'],'check':result}),202

@app.route('/measurements', methods=['GET'])
def get_all():
    return jsonify(received)

@app.route('/health')
def health():
    return {'status':'ok'}

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)
