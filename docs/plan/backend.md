# Backend Plan — Intelligent Inventory Dashboard

> **Stack:** **.NET 10 (LTS)** · C# 14 · FastEndpoints 8.x (REPR) · EF Core 10 · SignalR · MediatR 14.2 · FluentValidation 12 · Serilog.AspNetCore 10 · OpenTelemetry · OpenObserve
> **Architecture:** Clean Architecture + DDD + CQRS, in-process post-commit domain event dispatching, typed `Result<T>` pattern.

---

## 1. Architecture Decisions

| Area | Implementation Choice | Rationale |
|---|---|---|
| **Runtime** | .NET 10 (LTS) & C# 14 | Target framework `net10.0`; primary constructors, pattern matching, performance gains. |
| **API Endpoints** | FastEndpoints 8.x (REPR pattern) | Vertical slice endpoints (`Endpoint<TRequest, TResponse>`), auto OpenAPI / Swagger schemas, no bloated controllers. |
| **CQRS & Pipeline** | MediatR 14.2 | Strict separation of commands and queries. Global pipeline behaviors: `ValidationBehavior` and `PerformanceBehavior`. |
| **Domain Events** | `DomainEventDispatchInterceptor` | Intercepts EF Core `SaveChangesAsync` and publishes domain events via MediatR **only after** successful transaction commit. |
| **ORM & Persistence** | EF Core 10 | Code-first configurations, owned entity value objects (`Vin`, `Money`), query filters (`DeletedAt == null`), concurrency tokens (`RowVersion`). |
| **Realtime** | ASP.NET Core SignalR (`/hubs/inventory`) | Typed WebSocket push (`IInventoryClient`) with `inventory-dashboard` and `dealership:{id}` groups. |
| **Auth** | ASP.NET Core Identity + JWT | Roles `Manager`, `Saler`, `Sales`. Bearer token auth + refresh token endpoint. |
| **Observability** | OpenTelemetry + OpenObserve | Full-stack distributed traces, EF Core SQL telemetry, and Serilog structured logs exported via OTLP to OpenObserve. |

---

## 2. Solution Structure

```
src/
├── IID.Domain/             # Pure business models: Vehicle, VehicleAction, Dealership, VOs, Events
├── IID.Application/        # CQRS vertical slices: Features (Auth, Dashboard, Vehicles, Actions, Dealerships)
├── IID.Infrastructure/     # EF Core persistence, Identity, Repositories, SignalR Notifiers, Seeders
└── IID.Api/                # FastEndpoints, Configuration, Middleware (GlobalExceptionHandler), Program.cs

tests/
├── IID.Domain.Tests/        # Invariant unit tests (Vin, Vehicle, Aging, DemandScore)
├── IID.Application.Tests/   # CQRS command/query handlers and validators
├── IID.Infrastructure.Tests/# Repository query logic and seed options tests
└── IID.Api.Tests/           # Middleware exception handling and endpoint testing
```

---

## 3. Domain Model (`IID.Domain`)

### 3.1 Aggregate Roots & Entities

- **`Vehicle` (Aggregate Root):**
  - Identifiers: `Id`, `DealershipId`, `Vin`, `StockNumber`.
  - Specs: `Make`, `Model`, `Year` (1980..current+1), `Color`, `Mileage`, `FuelType` (`Petrol`, `Diesel`, `Hybrid`, `PluginHybrid`, `Electric`).
  - Pricing: `PurchasePrice` (`Money`), `AskingPrice` (`Money`), `SoldPrice` (`Money?`), `SoldAt`.
  - State & Audit: `Status` (`Available`, `Pending`, `Sold`, `Wholesale`), `DateAddedToInventory`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `RowVersion`.
  - Methods: `Create`, `Update`, `TransferDealership`, `MarkSold`, `SoftDelete`.
  - Analytics & Computed: `DaysInInventory()`, `IsAging(threshold=90)`, `GetAgingSeverity()`, `GetDemandScore()` (0–100), `GetDemandLevel()`.
- **`VehicleAction` (Aggregate Root):**
  - Members: `Id`, `VehicleId`, `ActionType`, `Notes`, `LoggedByUserId`, `LoggedAt`, audit fields.
  - Action Types (10): `PriceReductionPlanned`, `PriceReductionExecuted`, `TransferToWholesale`, `TradeInCustomer`, `MarketingCampaign`, `DealerAuction`, `ManagerReview`, `Relist`, `Other`, `TransferDealership`.
- **`Dealership` (Aggregate Root):**
  - Members: `Id`, `Name`, `Code`, `City`, `State`, `Phone`, `CreatedAt`, `UpdatedAt`.
- **Supporting Entities:** `InventoryActivity` (audit activity feed), `UserActivityReadStatus`, `Notification`.

### 3.2 Domain Services & Analytics

- **`VehicleAnalyticsService`:** Computes aging severity (`None`, `Warning` $\ge 30$, `High` $\ge 60$, `Critical` $\ge 90$), demand heuristics (0–100), and aging bucket distributions.
- **`AgingStockIdentifier`:** Fast evaluation against `InventoryPolicy.AgingStockThresholdDays = 90`.

---

## 4. Application Layer (`IID.Application`)

### 4.1 Feature Slices & CQRS

- **Dashboard:**
  - `GetDashboardBundleQuery`: Single roundtrip query loading summary cards, quick stats, 12-month sales trend, powertrain breakdown, aging breakdown, action center list, and paginated inventory.
  - `GetDashboardAgingQuery`, `GetLowInventoryAlertsQuery`.
- **Vehicles:**
  - Commands: `CreateVehicleCommand`, `UpdateVehicleCommand`, `TransferDealershipCommand`, `MarkVehicleSoldCommand`, `DeleteVehicleCommand`.
  - Queries: `ListVehiclesQuery` (paginated, sorted, filterable by dealership, make, model, VIN, stockNumber, age, status), `GetVehicleByIdQuery`, `GetAgingStockQuery`.
- **Vehicle Actions:**
  - Commands: `LogVehicleActionCommand`, `UpdateVehicleActionCommand`, `SoftDeleteVehicleActionCommand`.
  - Queries: `GetVehicleActionsQuery`.
- **Dealerships:**
  - Commands: `CreateDealershipCommand`, `UpdateDealershipCommand`.
  - Queries: `GetDealershipsQuery`.
- **Activities & Auth:**
  - `GetActivitiesQuery`, `MarkActivityReadCommand`.
  - `LoginCommand`, `RefreshTokenCommand`.

### 4.2 In-Process Domain Event Handlers

Instead of calling notifiers directly inside handlers, mutations stage domain events on aggregates:

```
Command Handler ──► DbContext.SaveChangesAsync()
                         │
                         ▼
        DomainEventDispatchInterceptor (Post-Commit)
                         │
         ┌───────────────┴───────────────┐
         ▼                               ▼
VehicleRealtimeEventHandler    DashboardRealtimeEventHandler
(notifies vehicle/dealership)  (recalculates KPIs & broadcasts)
```

- **`VehicleRealtimeEventHandler`:** Dispatches `VehicleAdded`, `VehicleUpdated`, `VehicleSold`, `VehicleRemoved` to `IVehicleHubNotifier`.
- **`VehicleTransferRealtimeEventHandler`:** Dispatches `VehicleTransferred` events.
- **`DashboardRealtimeEventHandler`:** Recomputes summary metrics and alert lists upon state changes, pushing `DashboardSummaryUpdated`, `DashboardAlertsUpdated`, and `InventoryChanged`.
- **`DealershipRealtimeEventHandler`:** Dispatches `DealershipAdded` and `DealershipUpdated` to `IDealershipHubNotifier`.
- **`VehicleActionRealtimeEventHandler`:** Dispatches `VehicleActionLogged` to `IVehicleHubNotifier`.

---

## 5. Infrastructure Layer (`IID.Infrastructure`)

### 5.1 Persistence & EF Core 10

- **`IidDbContext`:** Configures `DbSet<Vehicle>`, `DbSet<VehicleAction>`, `DbSet<Dealership>`, `DbSet<UserActivityReadStatus>`, and Identity tables.
- **Interceptors:** `DomainEventDispatchInterceptor` captures domain events before save and publishes them after successful commit.
- **Entity Configurations:**
  - Global query filter `[DeletedAt] IS NULL` on soft-deletable entities.
  - Filtered unique indexes on `UX_Vehicle_Vin_Active` and `UX_Vehicle_StockNumber_Active`.
  - Composite indexes on `Status` + `DateAddedToInventory`, `FuelType`, `DealershipId`.
- **Repositories & UnitOfWork:** Repositories encapsulate database access; `UnitOfWork` handles transactions.

### 5.2 Real-time SignalR Hub (`InventoryHub`)

- **Hub URL:** `/hubs/inventory` (requires JWT authentication).
- **Client Contract (`IInventoryClient`):**
  - `VehicleAdded(VehicleRealtimeDto)`
  - `VehicleUpdated(VehicleRealtimeDto)`
  - `VehicleRemoved(Guid vehicleId)`
  - `VehicleAging(VehicleRealtimeDto)`
  - `VehicleActionLogged(VehicleActionRealtimeDto)`
  - `DashboardSummaryUpdated(DashboardSummaryRealtimeDto)`
  - `DashboardAlertsUpdated(IReadOnlyList<DashboardAlertRealtimeDto>)`
  - `InventoryChanged()`
  - `DealershipAdded(DealershipRealtimeDto)`
  - `DealershipUpdated(DealershipRealtimeDto)`
- **Groups:** Automatically adds connections to `inventory-dashboard`; allows clients to invoke `JoinDealership(dealershipId)` and `LeaveDealership(dealershipId)`.

### 5.3 Automated Seeders (`IidDbInitializer`)

Runs on application startup when database is initialized:
1. `IdentitySeeder` (Order 10): Roles `Manager`, `Saler`, `Sales`. Users `admin@iid.local` and `saler@iid.local`.
2. `DealershipsSeeder` (Order 15): 10 metropolitan dealerships across the US.
3. `VehiclesSeeder` (Order 20): Seeds 733 vehicles distributed across all 10 dealerships with varied age, status, and fuel types.
4. `VehicleActionsSeeder` (Order 30): Attaches initial action history to demo vehicles.

---

## 6. API Endpoints (`IID.Api`)

| HTTP Method | Route | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Public | Authenticates credentials; returns JWT and refresh token |
| `POST` | `/api/v1/auth/refresh-token` | Public | Exchanges refresh token for new access token |
| `GET` | `/api/v1/dashboard` | `Manager`, `Sales`, `Saler` | Complete single-roundtrip dashboard bundle with filters |
| `GET` | `/api/v1/dashboard/aging` | `Manager`, `Sales`, `Saler` | Aging breakdown metrics |
| `GET` | `/api/v1/dashboard/alerts` | `Manager`, `Sales`, `Saler` | Low inventory and aging alerts |
| `GET` | `/api/v1/vehicles` | `Manager`, `Sales`, `Saler` | Paginated, sorted, filtered vehicle roster |
| `GET` | `/api/v1/vehicles/{id}` | `Manager`, `Sales`, `Saler` | Single vehicle detail |
| `POST` | `/api/v1/vehicles` | `Manager` | Create vehicle |
| `PUT` | `/api/v1/vehicles/{id}` | `Manager` | Update vehicle |
| `POST` | `/api/v1/vehicles/{id}/sold` | `Manager` | Mark vehicle sold |
| `POST` | `/api/v1/vehicles/{id}/transfer` | `Manager` | Transfer vehicle to another dealership |
| `GET` | `/api/v1/vehicles/aging-stock` | `Manager`, `Sales`, `Saler` | Filtered query for aging stock units |
| `GET` | `/api/v1/vehicles/{id}/actions` | `Manager`, `Sales`, `Saler` | Action history for specific vehicle |
| `POST` | `/api/v1/vehicles/{id}/actions` | `Manager` | Log action on vehicle |
| `GET` | `/api/v1/vehicle-actions` | `Manager`, `Sales`, `Saler` | List logged actions |
| `PUT` | `/api/v1/vehicle-actions/{id}` | `Manager` | Edit action note or type |
| `DELETE` | `/api/v1/vehicle-actions/{id}` | `Manager` | Soft-delete action |
| `GET` | `/api/v1/dealerships` | `Manager`, `Sales`, `Saler` | List all dealerships |
| `POST` | `/api/v1/dealerships` | `Manager` | Create dealership branch |
| `PUT` | `/api/v1/dealerships/{id}` | `Manager` | Update dealership branch |
| `GET` | `/api/v1/activities` | `Manager`, `Sales`, `Saler` | Live activity feed |
| `POST` | `/api/v1/activities/read` | `Manager`, `Sales`, `Saler` | Mark activity notifications read |

---

## 7. Testing & Verification

- **251 Automated Tests:** Verified across Domain, Application, Infrastructure, and Api layers using `xUnit v3` on `net10.0`.
- **Command:** `dotnet test` executes all unit and integration test suites.
- **Coverage:** Comprehensive coverage of domain invariants (VIN format, plausible year, status transitions), CQRS validation rules, EF Core query filtering, and FastEndpoints exception mappings.

---

**Related plans:** [`business.md`](./business.md) · [`database.md`](./database.md) · [`frontend.md`](./frontend.md)