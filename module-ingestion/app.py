from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

@app.route('/measurements', methods=['POST'])
def add():
    data = request.get_json()
    print("Received:", data)
    return jsonify({'status': 'accepted', 'data': data}), 200

@app.route('/health')
def health():
    return {'status': 'ok'}

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)