# Intelligent Inventory Dashboard — Implementation Plans

Four coordinated plans cover the dashboard from end to end, with [`business.md`](./business.md) owning *what* the system does and the technical plans owning *how*.

| Plan | Scope | Key Stack (Real Implementation) |
|---|---|---|
| [`business.md`](./business.md) | **Domain, business rules, KPIs, ubiquitous language** | Single business source of truth; aggregates: `Vehicle`, `VehicleAction`, `Dealership` |
| [`backend.md`](./backend.md) | .NET 10 API + Clean Architecture + CQRS + SignalR | **.NET 10 LTS · FastEndpoints 8 (REPR) · EF Core 10 · SignalR · MediatR 14 · FluentValidation 12 · OpenTelemetry / OpenObserve** |
| [`frontend.md`](./frontend.md) | Single Page Application (SPA) | **Angular 19 (Standalone Components) · TypeScript 5.7 · Angular Signals & RxJS · SCSS · SignalR JS Client 10.x** |
| [`database.md`](./database.md) | SQL Server schema, EF Core migrations & seeders | **SQL Server 2022 (Docker) · EF Core 10 Code-First Migrations · In-App Automated Seeders (~733 vehicles, 10 dealerships)** |

> **Hierarchy:** `business.md` is authoritative for *what* and *why*. The technical plans are authoritative for *how*. If they disagree, **`business.md` wins** until amended.
>
> All backend projects target **.NET 10 LTS** with C# 14 features (primary constructors, modern pattern matching). The frontend is built on **Angular 19** with standalone components and reactive Signals.

---

## High-level Flow

```
┌──────────────────────┐         HTTPS / REST          ┌───────────────────────────────────┐        EF Core 10        ┌──────────────────┐
│   Angular 19 SPA     │ ────────────────────────────► │   Backend API (.NET 10 LTS)       │ ───────────────────────► │  SQL Server 2022 │
│   (Signals + RxJS +  │ ◄──────────────────────────── │   FastEndpoints · MediatR 14 ·    │ ◄─────────────────────── │  (IID Database)  │
│    Standalone SCSS)  │         JSON Responses        │   Domain Event Interceptor        │                          └──────────────────┘
└──────────┬───────────┘                               └─────────────────┬─────────────────┘
           │                       SignalR WebSocket                     │
           │ ◄───────────────────────────────────────────────────────────┘
           │   VehicleAdded / VehicleUpdated / VehicleRemoved /
           │   VehicleAging / VehicleActionLogged / DashboardSummaryUpdated
```

## Cross-cutting Requirements Coverage

| Requirement | Implementation in Code | Plan Reference |
|---|---|---|
| **1. Inventory Visualization & Multi-Branch** | FastEndpoints `GET /api/v1/vehicles` & `GET /api/v1/dashboard`; Angular `VehicleListPageComponent` & `DashboardPageComponent`; multi-dealership transfer & filtering | business §4.1, backend §4.1/§6.1, frontend §2/§4, database §3.3 |
| **2. Aging Stock (>90 days) Identification** | `VehicleAnalyticsService` & `AgingSeverity` tiers (Warning 30d, High 60d, Critical 90d); `GET /api/v1/vehicles/aging-stock`; live aging badges and action center | business §4.3, backend §3.5/§6.1, frontend §4, database §3.3 |
| **3. Action Logging per Aging Vehicle** | `VehicleAction` aggregate (10 action types); `POST /api/v1/vehicles/{id}/actions`; `LogActionModalComponent`; immutable audit trail & real-time SignalR push | business §4.2, backend §3.1/§6.1, frontend §5, database §3.4 |
| **4. Real-time Multi-User Sync** | In-process `DomainEventDispatchInterceptor` → MediatR domain event handlers → `InventoryHub` SignalR broadcasts (`inventory-dashboard` & `dealership:{id}` groups) | backend §5.3/§7, frontend §3 |

## Conventions

- `business.md` is the **single source of truth** for domain rules. Changes propagate in lockstep to `IID.Domain`, EF Core configurations, and technical plans.
- **Backend:** Clean Architecture + DDD + CQRS (MediatR), vertical-slice FastEndpoints (REPR pattern), `Result<T>` pattern, source-generated logging, and automated OpenTelemetry APM traces.
- **Frontend:** Angular 19 standalone components, reactive Angular Signals + RxJS, scoped SCSS design system, route-level code splitting, and typed SignalR service.
- **Database:** Code-first EF Core 10 migrations in `src/IID.Infrastructure/Migrations` applied automatically on startup; comprehensive seeders generate 10 dealerships, 733 vehicles, actions, and test roles.

## Read Order

1. [`business.md`](./business.md) — Domain concepts, ubiquitous language, invariants, KPIs, and vehicle lifecycle.
2. [`backend.md`](./backend.md) — .NET 10 API architecture, domain entities, CQRS slices, SignalR hub, and endpoints.
3. [`database.md`](./database.md) — Relational schema, indexes, query filters, migrations, and seeders.
4. [`frontend.md`](./frontend.md) — Angular 19 SPA architecture, Signals/RxJS state flow, routes, modals, and real-time syncing.