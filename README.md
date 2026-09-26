# Intelligent Inventory Dashboard

Local dev stack: .NET 10 API + MS SQL Server 2022, orchestrated with Docker Compose.

## Prerequisites
- Docker Desktop (or Docker Engine + Compose v2)
- .NET SDK 10.0 (only required to run tests or the API outside containers)

## Quick start
```bash
cp .env.example .env             # tweak secrets if you want
docker compose up -d --build     # build + start mssql + api
docker compose logs -f api       # tail API logs
```

The API listens on http://localhost:8080 (Swagger at `/swagger`).
SQL Server listens on `localhost:1433` (sa / `MSSQL_SA_PASSWORD` from `.env`).

## Project layout
```
IID/
├── docker-compose.yml         # mssql + api services
├── .env.example               # template for local secrets
├── .dockerignore
├── IID.slnx
├── NuGet.config
├── global.json                # pins .NET SDK 10.0
├── docs/
├── frontend/                  # React + TS client
├── src/
│   ├── IID.Api/               # FastEndpoints, JWT, Swagger
│   │   └── Dockerfile         # multi-stage build
│   ├── IID.Application/       # MediatR handlers, validators
│   ├── IID.Domain/            # entities, value objects
│   └── IID.Infrastructure/    # EF Core, repositories, identity, SignalR
└── tests/
```

## Database migrations & seeding
On startup the API applies pending EF migrations and runs idempotent data seeders (identity roles/users, a small demo vehicle set, and a few action log rows). See `database/README.md` for the full list and opt-out flags.

Default dev credentials (override in non-dev via `IID_SEED__ADMINPASSWORD` / `IID_SEED__SALERPASSWORD` env vars):

| Role    | Email             | Password         |
|---------|-------------------|------------------|
| Manager | admin@iid.local   | `P@ssw0rd!Admin` |
| Saler   | saler@iid.local   | `P@ssw0rd!Saler` |

To regenerate migrations after changing the domain model:
```bash
dotnet ef migrations add <Name> \
  --project src/IID.Infrastructure \
  --startup-project src/IID.Api
```

## Useful commands
| Command | Purpose |
|---------|---------|
| `docker compose up -d --build` | Start everything in the background |
| `docker compose logs -f db` | Tail SQL Server logs |
| `docker compose ps` | List service health |
| `docker compose down` | Stop services, keep volume |
| `docker compose down -v` | Stop + delete the SQL data volume |

## Connecting client tools
- Host: `localhost,1433`
- User: `sa`
- Password: value of `MSSQL_SA_PASSWORD` in `.env`
- Trust server certificate: yes (already in the connection string)

## Observability with OpenObserve
The stack includes [OpenObserve](https://openobserve.ai/) for centralized Logs, Traces (APM), and Metrics.
- **Web UI**: [http://localhost:5080](http://localhost:5080)
- **User**: `admin@iid.local`
- **Password**: `P@ssw0rd!OpenObserve`
- **Features**: Real-time log search, distributed HTTP waterfall tracing, memory/CPU metrics, and customizable dashboards.
