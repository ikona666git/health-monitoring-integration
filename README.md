# Мониторинг здоровья — Интеграционное решение

## Описание
Система мониторинга здоровья для сбора, анализа и уведомлений о физиологических показателях.

**Сквозной сценарий:** Датчики → Показатели → Нормы → Уведомления → История

## Архитектура

| Сервис | Порт | Технология | Swagger |
|--------|------|------------|---------|
| Ingestion | 5101 | Python/Flask + Flasgger | http://localhost:5101/docs |
| Rules Engine | 5102 | Python/FastAPI | http://localhost:5102/docs |
| Alerting | 3100 | Node.js/Express | http://localhost:3100/docs |
| Integration API | 5200 | ASP.NET Core + Swashbuckle | http://localhost:5200/swagger |
| Frontend | 8180 | HTML/JS/Bootstrap | — |

## Быстрый запуск (Docker)

```bash
docker compose up -d --build
```

- UI: http://localhost:8180
- Swagger всех сервисов доступен с главной страницы

## OpenAPI спецификации

| Сервис | JSON |
|--------|------|
| Ingestion | http://localhost:5101/openapi.json |
| Rules Engine | http://localhost:5102/openapi.json |
| Alerting | http://localhost:3100/openapi.json |
| Integration | http://localhost:5200/swagger/v1/swagger.json |

## Интеграционный слой (.NET)

```bash
cd IntegrationService
dotnet run
```

Swagger UI: http://localhost:5200/swagger

Пример запроса:

```bash
curl -X POST http://localhost:5200/api/measurements \
  -H "Content-Type: application/json" \
  -d '{"user_id":"user123","metric_type":"heart_rate","value":135}'
```

## Тесты

```bash
dotnet test HealthMonitoring.slnx
```
