# Intelligent Inventory Dashboard — Implementation Plans

Three coordinated plans cover the dashboard from end to end, with a fourth document — the **business plan** — that owns *what* the system does.

| Plan | Scope | Key Stack |
|---|---|---|
| [`business.md`](./business.md) | **Domain, business rules, KPIs, glossary** | Business source of truth; owns the ubiquitous language and invariants |
| [`backend.md`](./backend.md) | .NET 10 API + domain + persistence | **Clean Architecture + DDD · FastEndpoints 8 · EF Core 10 · SignalR · MediatR 14 · FluentValidation 12 · Serilog.AspNetCore 10** |
| [`frontend.md`](./frontend.md) | Manager dashboard SPA | React 18 + TS, **MobX** (global / UI), TanStack Query (server cache), SignalR client |
| [`database.md`](./database.md) | MS SQL Server schema, indexes, views, procs | SQL Server 2019+, EF Core 10 migrations + mirrored `.sql` files |

> **Hierarchy:** `business.md` is authoritative for *what* and *why*. The technical plans are authoritative for *how*. If they disagree, **`business.md` wins** until amended.

> **.NET 10 LTS** is supported until **2028-11-14**. C# 14 ships with it. EF Core 10 requires the .NET 10 runtime. All package major versions are aligned with the `net10.0` target framework.

---

## High-level Flow

```
┌──────────┐      HTTPS      ┌────────────────────┐     EF Core 10    ┌──────────────┐
│ Frontend │ ───────────────► │ Backend API        │ ────────────────►│  SQL Server  │
│ (React + │ ◄─── REST/JSON ─ │ (.NET 10 LTS ·     │ ◄────────────────│  (iid DB)    │
│  MobX)   │                  │  FastEndpoints ·   │                  └──────────────┘
└────┬─────┘   SignalR push   │  SignalR ·         │
     │  ← VehicleAdded/Upd…  │  MediatR 14)       │
     │                       └────────────────────┘
```

## Cross-cutting Requirements Coverage

| Requirement | Plan section |
|---|---|---|
| 1. Inventory visualization (filter by make/model/age) | business §4.1 V-001..V-010, backend §6.1 `/api/v1/vehicles`, frontend §7 filters, database §5 `usp_ListVehicles` |
| 2. Aging stock (>90 days) identification | business §4.3 AG-001..AG-004, backend §3.5 `AgingStockIdentifier`, database §4 `vw_AgingStock` + filtered index, frontend §7 `AgingStockPage` + `InventoryStore.agingVehicleIds` |
| 3. Log/persist status or proposed action per aging vehicle | business §4.2 A-001..A-006, backend §4.1 `LogVehicleActionCommand`, frontend §8 `LogVehicleActionModal`, database §3.4 `VehicleAction` table |

## Conventions

- `business.md` is the **single source of truth** for domain rules. Any change to a rule there must propagate to the Domain layer (with tests), the database (`CHECK` constraint migration), and the relevant technical plan in the same PR.
- All backend code follows the workspace standards (Clean Architecture, DDD, `Result<T>`, source-gen logging, no business logic in endpoints) and is compiled against **.NET 10 LTS** with **C# 14** features (primary constructors, `field` keyword).
- All frontend code follows Container/Presentational, one-folder-per-component, CSS Modules, no inline styles, no `any`.
- Database migrations live in `/database` (sibling of `/src`) and are applied by **EF Core 10** in dev / DBA-reviewed scripts in prod.

## Read Order

1. `business.md` — understand *what* and *why*: actors, rules, KPIs, glossary. Start here.
2. `backend.md` — understand the API surface, domain model, and real-time hub. (`.NET 10`, FastEndpoints 8, EF Core 10, MediatR 14, FluentValidation 12, Serilog.AspNetCore 10)
3. `database.md` — understand persistence, indexes, and the aging view (SQL Server, EF Core 10 migrations mirrored as `.sql`).
4. `frontend.md` — understand how the SPA consumes the API and reacts to SignalR events.

**Related plans:** [`business.md`](./business.md) · [`backend.md`](./backend.md) · [`database.md`](./database.md)