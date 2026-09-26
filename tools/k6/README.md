# k6 Load Testing Suite — Intelligent Inventory Dashboard

Automated performance and load testing suite using [Grafana k6](https://k6.io/) for the `IID.Api` backend.

---

## Directory Structure

```text
tools/k6/
├── config/
│   └── environments.js           # Environment variables & API base config
├── helpers/
│   └── auth.js                   # Reusable JWT authentication helper
├── scripts/
│   └── dashboard/
│       └── get-dashboard-bundle.js  # GET /api/v1/dashboard load test script
├── run.sh                        # Runner script (supports local k6 & Docker)
└── README.md                     # Documentation
```

---

## Quick Start

### Option 1: Using the Runner Script (Recommended)
The runner script will automatically use local `k6` if installed, or fallback to `docker run grafana/k6`:

```bash
# Run smoke test (100 req/s for 5m)
./tools/k6/run.sh smoke

# Run standard load test (150 req/s for 5m)
./tools/k6/run.sh load

# Run stress test (250 req/s for 5m)
./tools/k6/run.sh stress
```

### Option 2: Running directly with local k6
```bash
# Install k6 on macOS: brew install k6
k6 run -e PROFILE=smoke tools/k6/scripts/dashboard/get-dashboard-bundle.js
k6 run -e PROFILE=load tools/k6/scripts/dashboard/get-dashboard-bundle.js
k6 run -e PROFILE=stress tools/k6/scripts/dashboard/get-dashboard-bundle.js
```

### Option 3: Running with Docker directly
```bash
docker run --rm -i \
  --network="host" \
  -v "$PWD/tools/k6:/scripts" \
  -e PROFILE="load" \
  -e BASE_URL="http://localhost:8080" \
  grafana/k6 run /scripts/scripts/dashboard/get-dashboard-bundle.js
```

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `BASE_URL` | `http://localhost:8080` | Target IID API host and port |
| `PROFILE` | `load` | Execution profile: `smoke`, `load`, or `stress` |
| `ADMIN_EMAIL` | `admin@iid.local` | Admin user for JWT login |
| `ADMIN_PASSWORD` | `P@ssw0rd!Admin` | Admin password |
| `AUTH_TOKEN` | *(auto-generated)* | Optional pre-generated Bearer token |

---

## Performance Thresholds & SLAs

The `get-dashboard-bundle.js` test enforces strict production SLOs:
- **`http_req_duration p(95) < 500ms`**: 95% of dashboard requests must respond within 500ms.
- **`http_req_duration p(99) < 1000ms`**: 99% of requests must respond within 1 second.
- **`http_req_failed < 1%`**: Error rate must stay below 1%.
- **`dashboard_bundle_success_rate > 99%`**: Business logic and JSON schema assertions must pass 99%+.
