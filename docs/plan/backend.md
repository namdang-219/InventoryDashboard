# Backend Plan — Intelligent Inventory Dashboard

> **Bounded Context:** Dealership Inventory
> **Stack:** **.NET 10 (LTS, supported until 2028-11-10)** · C# 14 · FastEndpoints 8.x · EF Core 10 · SignalR · MediatR 14 · FluentValidation 12 · Serilog.AspNetCore 10 · ASP.NET Identity · xUnit
> **Style:** Clean Architecture + DDD, vertical slices, source-gen logging, `Result<T>` with typed `ErrorKind`

---

## 1. Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | Three-year support window (2025-11-11 → 2028-11-14), C# 14, latest performance improvements; aligns with EF Core 10 GA requirement |
| Paradigm | Clean Architecture + DDD | Enforces invariants in `Domain`, business logic in `Application`; Infrastructure is replaceable |
| Endpoints | FastEndpoints 8.x | Vertical-slice endpoints, lightweight, first-class Swagger, PATCH/Idempotency-Key friendly. FastEndpoints targets `net8.0` and is binary-compatible with `net10.0`. |
| ORM | EF Core 10 | Migrations, change tracking, owned-entity/value-object mapping, easy SQL Server targeting. EF Core 10 requires the .NET 10 runtime. |
| Real-time | SignalR (`/hubs/inventory`) | Server-push inventory updates to all dashboards |
| CQRS | MediatR 14.2 | Decouples endpoints from handlers; enables pipeline behaviors. MediatR 14 adds first-class `net10.0` targeting. |
| Validation | FluentValidation 12 | Declarative per-command rules; runs in MediatR pipeline. FluentValidation 12 requires ≥ .NET 8 and supports `net10.0`. |
| Auth | ASP.NET Identity + JWT | Roles `Manager`, `Viewer` |
| Logging | Serilog.AspNetCore 10 + `LoggerMessageAttribute` | High-perf, allocation-free logs. Package major version matches target framework (10.x for net10.0). |
| Errors | `Result<T>` + `ErrorKind` enum | No exceptions for domain failures; HTTP status mapped via `ResultExtensions.ToStatusCode()` |

### TFM Pinning

All projects set `<TargetFramework>net10.0</TargetFramework>`. Test projects use `net10.0` with xUnit v3. Solution targets `Microsoft.NET.Sdk` 10.x.

### Package Pinning (minimum)

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore"            Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools"     Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design"    Version="10.0.0" />
<PackageReference Include="FastEndpoints"                            Version="8.*" />
<PackageReference Include="FastEndpoints.Swagger"                    Version="8.*" />
<PackageReference Include="MediatR"                                  Version="14.2.0" />
<PackageReference Include="FluentValidation"                         Version="12.*" />
<PackageReference Include="Serilog.AspNetCore"                       Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR"             Version="2.3.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
```

`global.json` pins the SDK:

```json
{
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestFeature"
  }
}
```

### Layer Rules (per workspace standards)

```
Presentation → Application → Infrastructure
                  ↓
                Domain
```

- Application has **zero** infrastructure references.
- Repository interfaces live in **Application**; implementations in **Infrastructure**.
- Business logic lives in **Application** handlers and **Domain** entities/services.
- Endpoints contain **no** business logic; they only translate HTTP ↔ MediatR.

---

## 2. Solution & Folder Structure

```
src/
├── IID.Domain/                  # Entities, VOs, Domain Events, Domain Services, Exceptions
├── IID.Application/             # Features/, Common/ (Interfaces, Behaviors, Models, Results)
├── IID.Infrastructure/          # Persistence (EF Core), Identity, SignalR, Background services
└── IID.Api/                     # FastEndpoints, Composition root, Middleware, Swagger

tests/
├── IID.Domain.Tests/
├── IID.Application.Tests/
├── IID.Infrastructure.Tests/    # Testcontainers SQL Server
└── IID.IntegrationTests/        # WebApplicationFactory + SignalR smoke

database/                        # sibling of /src, holds migrations, seeds, backups
├── migrations/
├── seeds/
└── scripts/

IID.sln
```

### Application Layer Detail

```
IID.Application/
├── Common/
│   ├── Behaviors/               # LoggingBehavior, ValidationBehavior, PerformanceBehavior, UoWBehavior
│   ├── Interfaces/              # IVehicleRepository, IUnitOfWork, IVehicleHubNotifier, ICurrentUser
│   ├── Models/                  # PagedResult<T>, Result<T>, ErrorKind
│   └── Mappers/                 # VehicleMappingProfile, VehicleActionMappingProfile
└── Features/
    ├── Vehicles/
    │   ├── Commands/            # CreateVehicle, UpdateVehicle, PatchVehicle, DeleteVehicle
    │   ├── Queries/             # GetVehicleById, ListVehicles, GetAgingStock
    │   ├── DTOs/                # VehicleResponse, VehicleListItem
    │   └── Validators/
    └── VehicleActions/
        ├── Commands/            # LogVehicleAction, UpdateVehicleAction, DeleteVehicleAction
        ├── Queries/             # GetVehicleAction, ListVehicleActions
        ├── DTOs/
        └── Validators/
```

---

## 3. Domain Layer (`IID.Domain`)

### 3.1 Aggregates & Entities

| Aggregate Root | Purpose | Key Members |
|---|---|---|
| `Vehicle` | A vehicle in dealership inventory | `Id`, `Vin`, `Make`, `Model`, `Year`, `Color`, `Mileage`, `PurchasePrice`, `AskingPrice`, `Status`, `DateAddedToInventory` |
| `VehicleAction` | A logged proposal/status for a vehicle | `Id`, `VehicleId`, `ActionType`, `Notes`, `LoggedBy`>, `LoggedAt` |

- `Vehicle` is the consistency boundary for stock facts.
- `VehicleAction` is a **separate aggregate** referencing `VehicleId`. Cross-aggregate consistency is **eventual**, propagated via domain events + SignalR.

### 3.2 Value Objects (private setters, factory methods, no public constructors)

| VO | Invariants |
|---|---|
| `Vin` | 17 chars, `[A-HJ-NPR-Z0-9]` (ISO 3779), unique within active vehicles |
| `Year` | `1900 ≤ y ≤ currentYear + 1` |
| `Money` (record struct) | non-negative `Amount` + ISO `Currency` |
| `Mileage` | ≥ 0; warns if > 1,000,000 |
| `DateAddedToInventory` | ≤ `DateTimeOffset.UtcNow` |

### 3.3 Enums & Constants

```csharp
public enum VehicleStatus { Available, Sold, Pending, Wholesale }

public enum VehicleActionType {
    PriceReductionPlanned,
    TradeInEvaluation,
    WholesaleListed,
    ManagerReview,
    Relist,
    Other
}

public static class InventoryPolicy {
    public const int AgingStockThresholdDays = 90;
    public const int MaxListPageSize = 100;
}
```

### 3.4 Domain Events

| Event | Raised when | Side effect |
|---|---|---|
| `VehicleAddedToInventoryEvent` | New `Vehicle` persisted | SignalR `VehicleAdded` |
| `VehicleUpdatedEvent` | `Vehicle` modified | SignalR `VehicleUpdated` |
| `VehicleRemovedEvent` | `Vehicle` soft-deleted | SignalR `VehicleRemoved` |
| `VehicleAgingThresholdReachedEvent` | Vehicle crosses 90-day mark | SignalR `VehicleAging` |
| `VehicleActionLoggedEvent` | `VehicleAction` persisted | SignalR `VehicleActionLogged` |

Dispatched **after commit** by `IDomainEventDispatcher` (wraps MediatR `INotification`).

> **EF Core 10 note:** entities exposing domain events use the **named query filter** capability (`HasQueryFilter("active", e => e.DeletedAtUtc == null)`) so we can attach additional, selectively-disabled filters (e.g., per-tenant) without colliding with the soft-delete filter.

### 3.5 Domain Service

```csharp
public sealed class AgingStockIdentifier(DateTimeOffset now)
{
    public bool IsAging(Vehicle v) =>
        (now - v.DateAddedToInventory).TotalDays
            > InventoryPolicy.AgingStockThresholdDays;
}
```

### 3.6 Repository Interfaces (Application-owned, implemented in Infrastructure)

```csharp
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Vehicle?> GetByVinAsync(string vin, CancellationToken ct);
    Task<bool> VinExistsAsync(string vin, CancellationToken ct);
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task UpdateAsync(Vehicle vehicle, CancellationToken ct);
    Task DeleteAsync(Vehicle vehicle, CancellationToken ct);
}

public interface IVehicleActionRepository
{
    Task<VehicleAction?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(VehicleAction action, CancellationToken ct);
    Task UpdateAsync(VehicleAction action, CancellationToken ct);
    Task DeleteAsync(VehicleAction action, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

---

## 4. Application Layer — Features (Vertical Slices)

### 4.1 Commands & Queries (records)

```csharp
public sealed record CreateVehicleCommand(
    string Vin, string Make, string Model, int Year, string Color,
    int Mileage, decimal PurchasePrice, decimal AskingPrice,
    VehicleStatus Status, DateTimeOffset DateAddedToInventory
) : IRequest<Result<Guid>>;

public sealed record UpdateVehicleCommand(
    Guid Id, string Make, string Model, int Year, string Color,
    int Mileage, decimal PurchasePrice, decimal AskingPrice, VehicleStatus Status
) : IRequest<Result<Guid>>;

public sealed record DeleteVehicleCommand(Guid Id) : IRequest<Result>;

public sealed record ListVehiclesQuery(
    string? Make, string? Model, int? MinAgeDays, int? MaxAgeDays,
    VehicleStatus? Status, int Page = 1, int Limit = 20,
    string Sort = "createdAt", string Order = "desc"
) : IRequest<Result<PagedResult<VehicleListItem>>>;

public sealed record GetAgingStockQuery(int Page = 1, int Limit = 20)
    : IRequest<Result<PagedResult<VehicleListItem>>>;

public sealed record LogVehicleActionCommand(
    Guid VehicleId, VehicleActionType ActionType, string? Notes, Guid LoggedByUserId
) : IRequest<Result<Guid>>;

public sealed record UpdateVehicleActionCommand(
    Guid Id, VehicleActionType ActionType, string? Notes
) : IRequest<Result>;

public sealed record ListVehicleActionsQuery(
    Guid? VehicleId, int Page = 1, int Limit = 20
) : IRequest<Result<PagedResult<VehicleActionResponse>>>;
```

### 4.2 Validators (FluentValidation)

`CreateVehicleCommandValidator`:
- `Vin`: matches VIN regex (ISO 3779), not empty
- `Year`: between 1900 and `currentYear + 1`
- `Make`/`Model`: 1–50 chars
- `Mileage`, `PurchasePrice`, `AskingPrice`: `>= 0`
- `DateAddedToInventory`: `<= now`

`LogVehicleActionCommandValidator`:
- `VehicleId`: not `Guid.Empty`
- `Notes`: ≤ 2000 chars

### 4.3 Handlers (primary constructors, ≤ 30 lines)

```csharp
public sealed class CreateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IVehicleHubNotifier notifier,
    ILogger<CreateVehicleHandler> logger
) : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehicleCommand c, CancellationToken ct)
    {
        if (await vehicles.VinExistsAsync(c.Vin, ct))
            return Result.Failure<Guid>(ErrorKind.Conflict, "VIN already exists.");

        var vehicle = Vehicle.Create(c.Vin, c.Make, c.Model, c.Year, c.Color,
            c.Mileage, Money.Of(c.PurchasePrice), Money.Of(c.AskingPrice),
            c.Status, c.DateAddedToInventory);

        await vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        await notifier.VehicleAddedAsync(vehicle, ct);
        return Result.Success(vehicle.Id);
    }
}
```

Other handlers follow the same shape:
- `GetAgingStockHandler` — uses `AgingStockIdentifier` + filtered query.
- `LogVehicleActionHandler` — verifies vehicle exists, raises `VehicleActionLoggedEvent`, pushes SignalR.

### 4.4 Application Interfaces (implemented in Infrastructure)

```csharp
public interface IVehicleHubNotifier
{
    Task VehicleAddedAsync(Vehicle v, CancellationToken ct);
    Task VehicleUpdatedAsync(Vehicle v, CancellationToken ct);
    Task VehicleRemovedAsync(Guid vehicleId, CancellationToken ct);
    Task VehicleAgingAsync(Vehicle v, CancellationToken ct);
    Task VehicleActionLoggedAsync(VehicleAction a, CancellationToken ct);
}

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsInRole(string role);
}
```

---

## 5. Infrastructure Layer (`IID.Infrastructure`)

### 5.1 Persistence

- `IidDbContext : DbContext` — `DbSet<Vehicle>`, `DbSet<VehicleAction>`, identity `DbSet`s.
- `IEntityTypeConfiguration<Vehicle>` / `IEntityTypeConfiguration<VehicleAction>` — Fluent API only (no data annotations on domain types).
- Soft delete: **named query filter** `HasQueryFilter("active", e => e.DeletedAtUtc == null)` — EF Core 10 feature that allows additional filters (e.g., per-tenant) to be added later without collision.
- Concurrency: `RowVersion` (`rowversion`) token on `Vehicle`.
- Repositories: thin wrappers over `DbContext`; expose async APIs with `CancellationToken`.
- `UnitOfWork` wraps `DbContext.SaveChangesAsync`.
- Value objects (`Money`, `Vin`) mapped as **owned complex types**; `Money` stored as `decimal(18,4)` + `char(3)` (see database plan).
- JSON columns: any free-form snapshot (`VehicleInventoryHistory.OldValuesJson`) stored as native `json` column type (EF Core 10 has first-class JSON column support).

### 5.2 Identity

- `ApplicationUser : IdentityUser<Guid>`.
- Roles seeded: `Manager`, `Viewer`.
- JWT bearer auth + refresh tokens (optional).
- `CurrentUserService` reads from `IHttpContextAccessor`.

### 5.3 Real-time — SignalR

```csharp
public interface IInventoryClient
{
    Task VehicleAdded(VehicleResponse vehicle);
    Task VehicleUpdated(VehicleResponse vehicle);
    Task VehicleRemoved(Guid vehicleId);
    Task VehicleAging(VehicleResponse vehicle);
    Task VehicleActionLogged(VehicleActionResponse action);
}

public sealed class InventoryHub : Hub<IInventoryClient>
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "inventory-dashboard");
        await base.OnConnectedAsync();
    }
}

public sealed class SignalRVehicleHubNotifier(
    IHubContext<InventoryHub, IInventoryClient> hub
) : IVehicleHubNotifier
{
    public Task VehicleAddedAsync(Vehicle v, CancellationToken ct) =>
        hub.Clients.Group("inventory-dashboard")
            .VehicleAdded(VehicleMappingProfile.ToResponse(v));
    // ... similar for others
}
```

### 5.4 Background Aging Job

```csharp
public sealed class AgingStockMonitorService(
    IServiceScopeFactory scopes,
    ILogger<AgingStockMonitorService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // every 15 min scan vehicles aged 80–91 days, raise VehicleAgingThresholdReachedEvent
    }
}
```

---

## 6. Presentation Layer (`IID.Api`)

### 6.1 API Endpoints

| Method | Route | Auth | Success | Failure |
|---|---|---|---|---|
| POST | `/api/v1/auth/login` | public | 200 + token | 401 |
| POST | `/api/v1/auth/refresh` | public | 200 + token | 401 |
| POST | `/api/v1/vehicles` | Manager | 201 + `{ data: { id } }` | 400, 409, 422 |
| GET | `/api/v1/vehicles` | Manager/Viewer | 200 paged | 400, 401 |
| GET | `/api/v1/vehicles/aging-stock` | Manager/Viewer | 200 paged | 400, 401 |
| GET | `/api/v1/vehicles/{id}` | Manager/Viewer | 200 | 404 |
| PUT | `/api/v1/vehicles/{id}` | Manager | 200 | 404, 422 |
| PATCH | `/api/v1/vehicles/{id}` | Manager | 200 | 404, 422 |
| DELETE | `/api/v1/vehicles/{id}` | Manager | 204 | 404 |
| POST | `/api/v1/vehicles/{id}/actions` | Manager | 201 | 404, 422 |
| GET | `/api/v1/vehicles/{id}/actions` | Manager/Viewer | 200 paged | 404 |
| GET | `/api/v1/vehicle-actions` | Manager | 200 paged | 401 |
| GET | `/api/v1/vehicle-actions/{id}` | Manager/Viewer | 200 | 404 |
| PATCH | `/api/v1/vehicle-actions/{id}` | Manager | 200 | 404, 422 |
| DELETE | `/api/v1/vehicle-actions/{id}` | Manager | 204 | 404 |

### 6.2 Response Shape

```jsonc
// Success
{ "data": { ... }, "meta": { "page": 1, "limit": 20, "total": 137 } }

// Failure
{ "error": { "code": 404, "message": "Vehicle not found.", "details": [] } }
```

### 6.3 Endpoint Example

```csharp
public sealed class CreateVehicleEndpoint(ISender sender) : Endpoint<CreateVehicleRequest, CreateVehicleResponse>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles");
        Roles("Manager");
        Validator<CreateVehicleRequestValidator>();
    }

    public override async Task HandleAsync(CreateVehicleRequest req, CancellationToken ct)
    {
        var cmd = new CreateVehicleCommand(req.Vin, req.Make, ...);
        var result = await sender.Send(cmd, ct);
        await result.Match(
            onSuccess: id => SendCreatedAtAsync($"/api/v1/vehicles/{id}",
                new CreateVehicleResponse(id), cancellation: ct),
            onFailure: async err => await SendResultAsync(
                ResultExtensions.ToIResult(err, HttpContext)));
    }
}
```

---

## 7. Real-time Event Flow

```
Manager A creates vehicle
   └─► POST /api/v1/vehicles
         └─► CreateVehicleHandler
               ├─► VehicleRepository.AddAsync
               ├─► UnitOfWork.SaveChangesAsync (commits)
               ├─► VehicleAddedToInventoryEvent dispatched
               └─► IVehicleHubNotifier.VehicleAddedAsync
                     └─► SignalR broadcasts to "inventory-dashboard" group
                           └─► All dashboards patch MobX store via applyVehicleAdded(v)

Background (every 15 min)
   └─► AgingStockMonitorService
         └─► For each vehicle crossing 90 days:
               └─► VehicleAgingThresholdReachedEvent
                     └─► Hub.VehicleAging
                           └─► "Aging Stock" tile lights up + AgingStockStore.applyVehicleAging(v)

Manager logs action on aging vehicle
   └─► POST /api/v1/vehicles/{id}/actions
         └─► LogVehicleActionHandler
               ├─► VehicleActionRepository.AddAsync
               ├─► SaveChanges
               └─► VehicleActionLoggedEvent
                     └─► Hub.VehicleActionLogged
                           └─► VehicleActionsStore.applyActionLogged(a)
```

---

## 8. Logging & Error Handling

- **Source-gen logs only** via `LoggerMessageAttribute`; no `LogInformation`/`LogDebug`.
- Placeholders PascalCase: `{VehicleId}`, `{Vin}`.
- `ILogger<T>.IsEnabled(level)` guards checked before structured log calls.
- Levels: `Debug` (dev), `Information` (ops), `Warning` (recoverable), `Error`/`Critical` (failures).
- `Result<T>` carries `ErrorKind`; endpoints call `ResultExtensions.ToStatusCode()` — **no** inline `switch` on error code in endpoints.
- Unhandled exceptions caught by `UseExceptionHandler` → 500 with sanitized payload.

---

## 9. Testing

| Layer | Tests |
|---|---|
| Domain | VO invariants, factory methods, event raising, `AgingStockIdentifier` |
| Application | Handler unit tests with Moq; validator negative cases; pipeline behaviors |
| Infrastructure | Testcontainers SQL Server + FluentAssertions against migrations |
| Integration | `WebApplicationFactory` for endpoints; `HubConnection` smoke tests for SignalR |
| Coverage target | ≥ 80% line on `Domain` and `Application` |

---

## 10. Setup

```bash
# 0. Prereqs — .NET 10 SDK pinned via global.json (already committed)
dotnet --version   # 10.0.x

# 1. Restore + build
dotnet restore
dotnet build -c Debug

# 2. Configure connection string (User Secrets, dev)
dotnet user-secrets set "ConnectionStrings:IID" \
  "Server=localhost;Database=IID;User Id=sa;Password=<pwd>;TrustServerCertificate=True;" \
  --project src/IID.Api

# 3. Apply EF Core 10 migrations
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef database update \
  --project src/IID.Infrastructure \
  --startup-project src/IID.Api

# 4. Run
dotnet run --project src/IID.Api
# Swagger:  http://localhost:5000/swagger
# SignalR:  ws://localhost:5000/hubs/inventory
```

### C# 14 / .NET 10 Notes

- All classes use **primary constructors** for dependency injection (no field-and-constructor boilerplate).
- Value objects and aggregate entities prefer the **C# 14 `field` contextual keyword** for encapsulated properties: `public Money Price { get; private set => field = Money.Of(value); }` — eliminates explicit backing fields and centralizes invariant checks.
- `[LoggerMessage]` partial classes are unaffected by .NET 10; same source-gen pipeline.
- Tests target xUnit **v3** (`xunit v3` ships with .NET 10 native AOT-friendly test runner).

---

**Related plans:** [`frontend.md`](./frontend.md) · [`database.md`](./database.md)