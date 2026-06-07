const express = require('express');
const app = express();

// CORS для всех запросов
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
        time: new Date().toISOString()
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

app.listen(3000, () => console.log('Alerting on 3000 with CORS'));