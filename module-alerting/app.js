const express = require('express');
const app = express();
app.use(express.json());

let alerts = [];

app.post('/alert', (req, res) => {
    const { user_id, metric_type, value, min_normal, max_normal, deviation_percent } = req.body;
    const msg = `ALERT: ${metric_type}=${value} (norm ${min_normal}-${max_normal}), dev ${deviation_percent}%`;
    console.log(msg);
    const alert = {
        id: alerts.length+1,
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
    if(uid) return res.json(alerts.filter(a => a.user_id === uid));
    res.json(alerts);
});

app.get('/health', (req, res) => res.json({ status: 'ok' }));

app.listen(3000, () => console.log('Alerting running on port 3000'));