from flask import Flask, request, jsonify
app = Flask(__name__)

@app.route('/test', methods=['POST'])
def test():
    return jsonify({"result": "ok", "data": request.get_json()})

@app.route('/health')
def health():
    return "OK"

app.run(host='0.0.0.0', port=5001)