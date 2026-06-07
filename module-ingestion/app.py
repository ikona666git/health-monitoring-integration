import os
import requests
from flask import Flask, request, jsonify
from flask_cors import CORS
from flasgger import Swagger, swag_from

app = Flask(__name__)
CORS(app)

RULES_ENGINE_URL = os.environ.get('RULES_ENGINE_URL', 'http://localhost:5002')
ALERTING_URL = os.environ.get('ALERTING_URL', 'http://localhost:3000')

measurements = []

swagger_template = {
    'swagger': '2.0',
    'info': {
        'title': 'Health Monitoring — Ingestion API',
        'description': 'Приём показателей с датчиков и сквозная интеграция: Показатели → Нормы → Уведомления',
        'version': '1.0.0',
    },
    'host': os.environ.get('SWAGGER_HOST', 'localhost:5101'),
    'basePath': '/',
    'schemes': ['http'],
    'tags': [
        {'name': 'Measurements', 'description': 'Операции с измерениями'},
        {'name': 'Health', 'description': 'Проверка состояния сервиса'},
    ],
}

swagger_config = {
    'headers': [],
    'specs': [{
        'endpoint': 'apispec',
        'route': '/openapi.json',
        'rule_filter': lambda rule: True,
        'model_filter': lambda tag: True,
    }],
    'swagger_ui': True,
    'specs_route': '/docs',
}

Swagger(app, template=swagger_template, config=swagger_config)


@app.route('/measurements', methods=['POST'])
@swag_from({
    'tags': ['Measurements'],
    'summary': 'Отправить измерение (сквозной сценарий)',
    'description': 'Принимает показатель, проверяет нормы в Rules Engine и при отклонении отправляет уведомление в Alerting.',
    'parameters': [{
        'name': 'body',
        'in': 'body',
        'required': True,
        'schema': {
            'type': 'object',
            'required': ['user_id', 'metric_type', 'value'],
            'properties': {
                'user_id': {'type': 'string', 'example': 'user123'},
                'metric_type': {'type': 'string', 'example': 'heart_rate'},
                'value': {'type': 'number', 'example': 135},
                'timestamp': {'type': 'string', 'format': 'date-time', 'example': '2026-06-08T12:00:00Z'},
            },
        },
    }],
    'responses': {
        200: {
            'description': 'Измерение принято, результат проверки норм и уведомления',
            'schema': {
                'type': 'object',
                'properties': {
                    'status': {'type': 'string', 'example': 'accepted'},
                    'data': {'type': 'object'},
                    'rules_check': {'type': 'object'},
                    'alert': {'type': 'object'},
                },
            },
        },
        400: {'description': 'Некорректный запрос'},
    },
})
def add():
    data = request.get_json()
    if not data:
        return jsonify({'status': 'error', 'message': 'JSON body required'}), 400

    required = ('user_id', 'metric_type', 'value')
    missing = [f for f in required if f not in data]
    if missing:
        return jsonify({'status': 'error', 'message': f'Missing fields: {", ".join(missing)}'}), 400

    measurements.append(data)

    result = {
        'status': 'accepted',
        'data': data,
        'rules_check': None,
        'alert': None,
    }

    try:
        rules_resp = requests.post(f'{RULES_ENGINE_URL}/check', json=data, timeout=5)
        rules_resp.raise_for_status()
        rules_result = rules_resp.json()
        result['rules_check'] = rules_result

        if rules_result.get('alert_triggered'):
            alert_payload = {
                'user_id': rules_result['user_id'],
                'metric_type': rules_result['metric_type'],
                'value': rules_result['value'],
                'min_normal': rules_result['min_normal'],
                'max_normal': rules_result['max_normal'],
                'deviation_percent': rules_result.get('deviation_percent'),
            }
            alert_resp = requests.post(f'{ALERTING_URL}/alert', json=alert_payload, timeout=5)
            alert_resp.raise_for_status()
            result['alert'] = alert_resp.json()
    except requests.RequestException as exc:
        result['status'] = 'partial'
        result['error'] = str(exc)

    return jsonify(result), 200


@app.route('/measurements', methods=['GET'])
@swag_from({
    'tags': ['Measurements'],
    'summary': 'Список принятых измерений',
    'responses': {
        200: {
            'description': 'Массив измерений в памяти',
            'schema': {'type': 'array', 'items': {'type': 'object'}},
        },
    },
})
def list_measurements():
    return jsonify(measurements)


@app.route('/health')
@swag_from({
    'tags': ['Health'],
    'summary': 'Health check',
    'responses': {
        200: {
            'description': 'Сервис работает',
            'schema': {
                'type': 'object',
                'properties': {'status': {'type': 'string', 'example': 'ok'}},
            },
        },
    },
})
def health():
    return jsonify({'status': 'ok'})


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5001))
    app.run(host='0.0.0.0', port=port)
