# Мониторинг здоровья - Интеграционное решение

## Описание
Система мониторинга здоровья для сбора, анализа и уведомлений о физиологических показателях.

## Архитектура

| Сервис | Порт | Технология |
|--------|------|------------|
| Ingestion | 5001 | Python/Flask |
| Rules Engine | 5002 | Python/FastAPI |
| Alerting | 3000 | Node.js/Express |
| Frontend | 8080 | HTML/JS/Bootstrap |
| Integration | консоль | C#/.NET 10 |

## Быстрый запуск

### 1. Запуск модулей (4 окна)

```bash
# Окно 1: Ingestion
cd module-ingestion
python app.py

# Окно 2: Rules Engine
cd module-rules
python -m uvicorn app:app --host 0.0.0.0 --port 5002

# Окно 3: Alerting
cd module-alerting
node app.js

# Окно 4: Frontend
cd frontend
python -m http.server 8080