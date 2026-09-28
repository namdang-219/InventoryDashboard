# Database Plan — Intelligent Inventory Dashboard

> **Database Engine:** Microsoft SQL Server 2022 (Dockerized via `mcr.microsoft.com/mssql/server:2022-latest`, Developer edition, port 1434)
> **ORM & Schema Management:** **EF Core 10** Code-First Migrations (`src/IID.Infrastructure/Migrations`)
> **Initialization:** Automated startup migration & idempotent seeder pipeline (`IidDbInitializer`)

---

## 1. Design Principles

| Principle | Implementation Details |
|---|---|
| **Engine** | SQL Server 2022 Developer edition in Docker Compose (`localhost:1434`), connection string with SSL encryption options. |
| **Soft Delete** | `DeletedAt` timestamp columns on `Vehicle` and `VehicleAction`, enforced via EF Core global query filters (`[DeletedAt] IS NULL`). |
| **Optimistic Concurrency** | `RowVersion` (`rowversion`) column on `Vehicle` aggregate to prevent concurrent write collisions. |
| **Integrity & Relations** | Foreign keys with `Restrict` behavior (`Vehicle.DealershipId` $\to$ `Dealerships.Id`, `VehicleAction.VehicleId` $\to$ `Vehicle.Id`). |
| **Filtered Indexes** | Unique active indexes on `Vin` and `StockNumber` scoped to `WHERE [DeletedAt] IS NULL`. |
| **Precision & Money** | Monies stored as `decimal(18,4)` for amount alongside ISO `nvarchar(3)` currency columns. |
| **Audit Trails** | `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`, and immutable `VehicleAction` logs. |

---

## 2. Entity Relationship Diagram (ERD)

```
┌─────────────────────────┐
│     dbo.AspNetUsers     │
├─────────────────────────┤
│ Id (PK, nvarchar 450)   │
│ Email, UserName, ...    │
└────────────┬────────────┘
             │ 1
             │
             │ 0..*
┌────────────▼────────────┐        1..* ┌─────────────────────────┐
│     dbo.Dealerships     │ ◄────────── │       dbo.Vehicle       │
├─────────────────────────┤             ├─────────────────────────┤
│ Id (PK, uniqueidentifier│             │ Id (PK, uniqueidentifier│
│ Name (nvarchar 150)     │             │ DealershipId (FK)       │
│ Code (UX, nvarchar 32)  │             │ Vin (UX filtered)       │
│ City, State, Phone      │             │ StockNumber (UX filter) │
│ CreatedAt, UpdatedAt    │             │ Make, Model, Year, Color│
└─────────────────────────┘             │ Mileage, FuelType       │
                                        │ Purchase / Asking Price │
                                        │ SoldPrice, SoldAt       │
                                        │ Status, DateAdded       │
                                        │ CreatedAt, UpdatedAt    │
                                        │ DeletedAt (soft delete) │
                                        │ RowVersion (rowversion) │
                                        └────────────┬────────────┘
                                                     │ 1
                                                     │
                                                     │ 0..*
                                        ┌────────────▼────────────┐
                                        │    dbo.VehicleAction    │
                                        ├─────────────────────────┤
                                        │ Id (PK, uniqueidentifier│
                                        │ VehicleId (FK)          │
                                        │ ActionType (int 0..9)   │
                                        │ Notes (nvarchar 2000)   │
                                        │ LoggedByUserId (FK)     │
                                        │ LoggedAt (datetimeoffset│
                                        │ CreatedAt, UpdatedAt    │
                                        │ DeletedAt (soft delete) │
                                        └─────────────────────────┘
```

---

## 3. Physical Schema & Table Specifications

### 3.1 `dbo.Dealerships`

Stores dealership branches across metropolitan regions.

| Column | Type | Nullable | Notes & Constraints |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | Primary Key |
| `Name` | `nvarchar(150)` | NO | Dealership trade name |
| `Code` | `nvarchar(32)` | NO | Branch identifier (e.g. `DLR-LA-01`), **Unique Index (`UX_Dealership_Code`)** |
| `City` | `nvarchar(100)` | NO | City location |
| `State` | `nvarchar(32)` | NO | State code (e.g. `CA`, `WA`) |
| `Phone` | `nvarchar(50)` | NO | Contact telephone number |
| `CreatedAt` | `datetimeoffset(7)` | NO | UTC creation timestamp |
| `UpdatedAt` | `datetimeoffset(7)` | NO | UTC last update timestamp |

### 3.2 `dbo.Vehicle`

Core vehicle inventory aggregate root.

| Column | Type | Nullable | Notes & Constraints |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | Primary Key |
| `DealershipId` | `uniqueidentifier` | NO | FK $\to$ `Dealerships.Id` (`ON DELETE RESTRICT`), Indexed |
| `Vin` | `nvarchar(17)` | NO | ISO 3779 standard, **Filtered Unique Index (`UX_Vehicle_Vin_Active`)** |
| `StockNumber` | `nvarchar(32)` | NO | Lot identifier, **Filtered Unique Index (`UX_Vehicle_StockNumber_Active`)** |
| `Make` | `nvarchar(50)` | NO | Manufacturer |
| `Model` | `nvarchar(50)` | NO | Vehicle model name |
| `Year` | `int` | NO | Model year ($1980 \le \text{Year} \le \text{Now} + 1$) |
| `Color` | `nvarchar(30)` | NO | Exterior color |
| `Mileage` | `int` | NO | Vehicle odometer reading |
| `FuelType` | `nvarchar(32)` | NO | `Petrol`, `Diesel`, `Hybrid`, `PluginHybrid`, `Electric`, Indexed |
| `PurchasePriceAmount` | `decimal(18,4)` | NO | Cost basis |
| `PurchasePriceCurrency` | `nvarchar(3)` | NO | ISO currency (default `USD`) |
| `AskingPriceAmount` | `decimal(18,4)` | NO | Listed retail price |
| `AskingPriceCurrency` | `nvarchar(3)` | NO | ISO currency (default `USD`) |
| `SoldPriceAmount` | `decimal(18,4)` | YES | Final sale price if sold |
| `SoldPriceCurrency` | `nvarchar(3)` | YES | ISO currency if sold |
| `SoldAt` | `datetimeoffset(7)` | YES | Sale completion timestamp |
| `Status` | `int` | NO | 0=Available, 1=Pending, 2=Sold, 3=Wholesale |
| `DateAddedToInventory` | `datetimeoffset(7)` | NO | Tenure clock reference date |
| `CreatedAt`, `UpdatedAt` | `datetimeoffset(7)` | NO | Audit timestamps |
| `CreatedByUserId`, `UpdatedByUserId` | `nvarchar(450)` | YES | User attribution |
| `DeletedAt` | `datetimeoffset(7)` | YES | Soft-delete timestamp (NULL = active) |
| `RowVersion` | `rowversion` | NO | Optimistic concurrency token |

**Indexes on `Vehicle`:**
- `UX_Vehicle_Vin_Active` (`Vin`) WHERE `[DeletedAt] IS NULL` (Unique)
- `UX_Vehicle_StockNumber_Active` (`StockNumber`) WHERE `[DeletedAt] IS NULL` (Unique)
- `IX_Vehicle_Status_DateAdded` (`Status`, `DateAddedToInventory`)
- `IX_Vehicle_FuelType` (`FuelType`)
- `IX_Vehicle_DealershipId` (`DealershipId`)

### 3.3 `dbo.VehicleAction`

Closed-loop remediation and status decision log for vehicles.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | Primary Key |
| `VehicleId` | `uniqueidentifier` | NO | FK $\to$ `Vehicle.Id` (`ON DELETE RESTRICT`) |
| `ActionType` | `int` | NO | 0=PriceReductionPlanned, 1=PriceReductionExecuted, 2=TransferToWholesale, 3=TradeInCustomer, 4=MarketingCampaign, 5=DealerAuction, 6=ManagerReview, 7=Relist, 8=Other, 9=TransferDealership |
| `Notes` | `nvarchar(2000)` | YES | Free-text operational note |
| `LoggedByUserId` | `nvarchar(450)` | NO | Identity of authenticated manager |
| `LoggedAt` | `datetimeoffset(7)` | NO | Timestamp when action was logged |
| `CreatedAt`, `UpdatedAt` | `datetimeoffset(7)` | NO | Audit timestamps |
| `DeletedAt` | `datetimeoffset(7)` | YES | Soft-delete timestamp |

**Indexes on `VehicleAction`:**
- `IX_VehicleAction_VehicleId_LoggedAt` (`VehicleId`, `LoggedAt`) WHERE `[DeletedAt] IS NULL`

### 3.4 `dbo.UserActivityReadStatuses`

Tracks read/unread notification states per user account.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | Primary Key |
| `UserId` | `nvarchar(450)` | NO | AspNetUsers reference |
| `ActivityId` | `nvarchar(100)` | NO | Activity or notification identifier |
| `IsRead` | `bit` | NO | Read state flag (1 = Read) |
| `ReadAtUtc` | `datetimeoffset(7)` | NO | UTC read timestamp |

---

## 4. EF Core 10 Migrations

Migrations live in `src/IID.Infrastructure/Migrations/`:

1. `20260924083419_InitialCreate`: Core schema, ASP.NET Identity, `Vehicle`, and `VehicleAction`.
2. `20260924221500_AddUserActivityReadStatus`: Adds user notification read state tracking table.
3. `20260925024224_AddDealershipEntity`: Adds `Dealerships` table and foreign key relation on `Vehicle`.
4. `20260925094817_AddSoldByUserIdToVehicle`: Adds sales tracking attribution columns.

### Applying Migrations

Migrations execute automatically on API launch via `IidDbInitializer`. To apply manually via EF CLI:

```bash
dotnet ef database update \
  --project src/IID.Infrastructure \
  --startup-project src/IID.Api
```

---

## 5. Seed Data Pipeline (`IidSeeder`)

Executed automatically on a fresh database:

1. **Identity (`IdentitySeeder`):**
   - Roles: `Manager`, `Saler`, `Sales`.
   - Default Users: `admin@iid.local` (Manager) & `saler@iid.local` (Saler/Sales).
2. **Dealerships (`DealershipsSeeder`):**
   - 10 deterministic demo dealerships (Apex Motors LA, Metro Auto Seattle, Summit Luxury Denver, Pinnacle Ford Dallas, Grand Horizon Phoenix, Velocity Miami, Coastal Bay SF, Heritage Chicago, Frontier Austin, Silverstone Atlanta).
3. **Vehicles (`VehiclesSeeder`):**
   - 733 vehicles distributed across all 10 branches (56–92 units each).
   - Real-world distribution: fresh arrivals, high/critical aging stock (>90 days), pending deals, sold units, and diverse fuel mixes.
4. **Actions (`VehicleActionsSeeder`):**
   - Seeds realistic operational notes and action logs across aged units.

---

**Related plans:** [`business.md`](./business.md) · [`backend.md`](./backend.md) · [`frontend.md`](./frontend.md)