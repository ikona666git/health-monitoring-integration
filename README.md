# Health Monitoring - Integration Solution

## Description
System for collecting, analyzing and notifying about physiological parameters.

## Architecture

| Service | Port | Technology |
|---------|------|-------------|
| Ingestion | 5001 | Python/Flask |
| Rules Engine | 5002 | Python/FastAPI |
| Alerting | 3000 | Node.js/Express |
| Frontend | 8080 | HTML/JS/Bootstrap |
| Integration | console | C#/.NET 10 |

## Quick Start

### 1. Run modules (4 windows)

```bash
# Window 1: Ingestion
cd module-ingestion
python app.py

# Window 2: Rules Engine
cd module-rules
python -m uvicorn app:app --host 0.0.0.0 --port 5002

# Window 3: Alerting
cd module-alerting
node app.js

# Window 4: Frontend
cd frontend
python -m http.server 8080
