const express = require('express');
const swaggerUi = require('swagger-ui-express');
const fs = require('fs');
const path = require('path');

const app = express();

app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Content-Type');
    if (req.method === 'OPTIONS') {
        return res.sendStatus(200);
    }
    next();
});

app.use(express.json());

const swaggerSpec = JSON.parse(
    fs.readFileSync(path.join(__dirname, 'openapi.json'), 'utf8')
);
const swaggerHost = process.env.SWAGGER_HOST || 'localhost:3100';
swaggerSpec.servers = [{ url: `http://${swaggerHost}` }];

app.use('/docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec));
app.get('/openapi.json', (req, res) => res.json(swaggerSpec));

let alerts = [];

app.post('/alert', (req, res) => {
    const { user_id, metric_type, value, min_normal, max_normal, deviation_percent } = req.body;
    const msg = `ALERT: ${metric_type}=${value} (norm ${min_normal}-${max_normal}), dev ${deviation_percent}%`;
    console.log(msg);
    const alert = {
        id: alerts.length + 1,
        user_id,
        metric_type,
        value,
        msg,
        time: new Date().toISOString(),
    };
    alerts.push(alert);
    res.json({ status: 'sent', alert_id: alert.id });
});

app.get('/alerts', (req, res) => {
    const uid = req.query.user_id;
    if (uid) return res.json(alerts.filter(a => a.user_id === uid));
    res.json(alerts);
});

app.get('/health', (req, res) => res.json({ status: 'ok' }));

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Alerting on ${PORT} ? Swagger: /docs`));
