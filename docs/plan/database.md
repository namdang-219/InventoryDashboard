# Database Plan — Intelligent Inventory Dashboard

> **Engine:** Microsoft SQL Server 2019+ (Express ok for dev, Standard/Enterprise for prod)
> **Schema name:** `iid`
> **Migrations:** **EF Core 10** (managed in `database/migrations/` and applied via `dotnet ef`)
> **Scripting style:** Migration scripts **also** mirrored as `.sql` files in `database/migrations/` for ops/DBA review
> **Backend target:** .NET 10 LTS — see [`backend.md`](./backend.md)

---

## 1. Design Principles

| Principle | Choice |
|---|---|
| Engine | SQL Server 2019+ (TDE-ready, rowversion, indexed views, JSON) |
| Integrity | FK + `CHECK` constraints + unique indexes enforced at the DB |
| Soft delete | `DeletedAtUtc` columns + filtered indexes for fast active-set reads |
| Audit | `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedByUserId`, `UpdatedByUserId` on every table |
| Concurrency | `RowVersion` (`rowversion`) on aggregates mutated by users |
| Identity | ASP.NET Core Identity tables (`AspNet*`) |
| Indexing | Filtered indexes on `IsActive=1`, covering indexes for hot list queries |
| History | `VehicleInventoryHistory` append-only log of stock movements |
| Money | `decimal(18,4)`; currency code stored alongside |
| Time | All timestamps `datetimeoffset(7)` UTC; `datetime2(0)` where date-only |
| Unicode | `nvarchar` everywhere; collate `SQL_Latin1_General_CP1_CI_AS` |

---

## 2. ERD

```
+------------------+        +-----------------------------+
| AspNetUsers      |        | Vehicle                     |
+------------------+        +-----------------------------+
| Id (PK, uniqueid)|1     0..*| Id (PK, uniqueidentifier)   |
| ...Identity cols |◄──────┤ CreatedByUserId (FK NULL)   |
+------------------+ 1    * | UpdatedByUserId (FK NULL)   |
                          | DeletedAtUtc                |
                          | RowVersion (rowversion)     |
                          +-----------------------------+
                                   ▲ 1
                                   │
                                   │ *
                          +-----------------------------+
                          | VehicleAction                |
                          +-----------------------------+
                          | Id (PK, uniqueidentifier)   |
                          | VehicleId (FK)              |
                          | ActionType (int)            |
                          | Notes (nvarchar 2000 NULL)  |
                          | LoggedByUserId (FK)         |
                          | LoggedAtUtc                 |
                          | Created/Updated audit       |
                          +-----------------------------+

Vehicle 1 ── * VehicleInventoryHistory
```

---

## 3. Tables

### 3.1 `dbo.AspNetUsers` (ASP.NET Identity)
Standard ASP.NET Core Identity schema; PK `nvarchar(450)`. Roles seed `Manager`, `Viewer`.

### 3.2 `dbo.AspNetRoles`, `dbo.AspNetUserRoles`, `dbo.AspNetRoleClaims`, `dbo.AspNetUserClaims`, `dbo.AspNetUserLogins`, `dbo.AspNetUserTokens`
Standard Identity tables; PKs `nvarchar(450)`.

### 3.3 `dbo.Vehicle`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK, default `NEWSEQUENTIALID()` |
| `Vin` | `nvarchar(17)` | NO | ISO 3779, **unique active** |
| `Make` | `nvarchar(50)` | NO | |
| `Model` | `nvarchar(50)` | NO | |
| `Year` | `int` | NO | CHECK 1900..(year+1) |
| `Color` | `nvarchar(30)` | NO | |
| `Mileage` | `int` | NO | CHECK ≥ 0 |
| `PurchasePriceAmount` | `decimal(18,4)` | NO | CHECK ≥ 0 |
| `PurchasePriceCurrency` | `char(3)` | NO | default `USD` |
| `AskingPriceAmount` | `decimal(18,4)` | NO | CHECK ≥ 0 |
| `AskingPriceCurrency` | `char(3)` | NO | default `USD` |
| `Status` | `tinyint` | NO | enum VehicleStatus |
| `DateAddedToInventoryUtc` | `datetimeoffset(7)` | NO | CHECK ≤ SYSUTCDATETIME() |
| `CreatedAtUtc` | `datetimeoffset(7)` | NO | default SYSUTCDATETIME() |
| `UpdatedAtUtc` | `datetimeoffset(7)` | NO | |
| `CreatedByUserId` | `nvarchar(450)` | YES | FK → AspNetUsers |
| `UpdatedByUserId` | `nvarchar(450)` | YES | FK → AspNetUsers |
| `DeletedAtUtc` | `datetimeoffset(7)` | YES | soft delete |
| `RowVersion` | `rowversion` | NO | optimistic concurrency |

**Indexes**

| Name | Columns | Type |
|---|---|---|
| `UX_Vehicle_Vin_Active` | `Vin` | **Filtered unique** `WHERE DeletedAtUtc IS NULL` |
| `IX_Vehicle_Make_Model_Year` | `Make, Model, Year` | non-clustered |
| `IX_Vehicle_Status_DateAdded` | `Status, DateAddedToInventoryUtc` | non-clustered |
| `IX_Vehicle_Aging_Active` | `DateAddedToInventoryUtc` | **Filtered** `WHERE DeletedAtUtc IS NULL INCLUDE (Status, Make, Model)` |

### 3.4 `dbo.VehicleAction`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NO | PK, default `NEWSEQUENTIALID()` |
| `VehicleId` | `uniqueidentifier` | NO | FK → Vehicle |
| `ActionType` | `tinyint` | NO | enum VehicleActionType |
| `Notes` | `nvarchar(2000)` | YES | |
| `LoggedByUserId` | `nvarchar(450)` | NO | FK → AspNetUsers |
| `LoggedAtUtc` | `datetimeoffset(7)` | NO | default SYSUTCDATETIME() |
| `CreatedAtUtc` | `datetimeoffset(7)` | NO | |
| `UpdatedAtUtc` | `datetimeoffset(7)` | NO | |
| `CreatedByUserId` | `nvarchar(450)` | YES | |
| `UpdatedByUserId` | `nvarchar(450)` | YES | |
| `DeletedAtUtc` | `datetimeoffset(7)` | YES | soft delete |

**Indexes**

| Name | Columns |
|---|---|
| `IX_VehicleAction_VehicleId_LoggedAt` | `VehicleId, LoggedAtUtc DESC` (filtered `DeletedAtUtc IS NULL`) |
| `IX_VehicleAction_LoggedBy` | `LoggedByUserId, LoggedAtUtc DESC` |

### 3.5 `dbo.VehicleInventoryHistory` (append-only audit)

| Column | Type | Notes |
|---|---|---|
| `Id` | `bigint` PK, IDENTITY(1,1) | |
| `VehicleId` | `uniqueidentifier` FK | |
| `EventType` | `tinyint` | Added/Updated/Removed/StatusChanged/PriceChanged |
| `OldValuesJson` | `nvarchar(max)` NULL | snapshot before |
| `NewValuesJson` | `nvarchar(max)` NULL | snapshot after |
| `ChangedByUserId` | `nvarchar(450)` FK | |
| `ChangedAtUtc` | `datetimeoffset(7)` | default SYSUTCDATETIME() |

**Indexes**
- `IX_VehicleInventoryHistory_VehicleId_ChangedAt` `(VehicleId, ChangedAtUtc DESC)`

---

## 4. Aging View

```sql
CREATE OR ALTER VIEW dbo.vw_AgingStock
WITH SCHEMABINDING
AS
SELECT
    v.Id,
    v.Vin,
    v.Make,
    v.Model,
    v.Year,
    v.Color,
    v.Mileage,
    v.AskingPriceAmount,
    v.AskingPriceCurrency,
    v.Status,
    v.DateAddedToInventoryUtc,
    DATEDIFF(DAY, v.DateAddedToInventoryUtc, SYSUTCDATETIME()) AS DaysInInventory,
    CASE
        WHEN DATEDIFF(DAY, v.DateAddedToInventoryUtc, SYSUTCDATETIME()) > 90
        THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS IsAging
FROM dbo.Vehicle v
WHERE v.DeletedAtUtc IS NULL;
```

- Bound to base table → clustered index aligned.
- Backed by `IX_Vehicle_Aging_Active` covering index.

---

## 5. Stored Procedures & Functions

| Name | Purpose |
|---|---|
| `dbo.usp_GetAgingStock @Page, @Limit, @Sort, @Order` | Server-side paging + sorting for `/vehicles/aging-stock`. Falls back to OFFSET/FETCH on the view. |
| `dbo.usp_ListVehicles @FiltersXml, @Page, @Limit, @Sort, @Order` | Paged list with filters; returns rows + total count. |
| `dbo.usp_LogVehicleAction @VehicleId, @ActionType, @Notes, @LoggedByUserId` | Validates vehicle exists; inserts row + audit row in single tx; returns new Id. |
| `dbo.tvf_VehicleActionsByVehicle (@VehicleId)` | Inline TVF for action history list. |

> Handlers can call these via EF Core raw SQL when needed; otherwise EF Core LINQ is fine.

---

## 6. Seed Data

`database/seeds/0001_IdentitySeed.sql`

- Roles: `Manager`, `Viewer`
- Demo manager user (`manager@demo.local` / `P@ssw0rd!` — change in prod)
- Demo viewer user

`database/seeds/0002_VehiclesSeed.sql`

- 25 sample vehicles spanning makes/models/years
- 5 deliberately aged beyond 90 days for aging-stock demonstration
- 2 with logged `PriceReductionPlanned` actions

`database/seeds/0003_VehicleActionsSeed.sql`

- 4 sample actions linked to the aged vehicles

---

## 7. Migration Plan

| Step | EF Core command | Output |
|---|---|---|
| 1 | `dotnet ef migrations add Init_Schema -p src/IID.Infrastructure -s src/IID.Api` | `Migrations/<ts>_Init_Schema.cs` + Designer + ModelSnapshot |
| 2 | `dotnet ef migrations add Identity -p ... -s ...` | Adds `AspNet*` tables |
| 3 | `dotnet ef migrations add SeedDemoData -p ... -s ...` | Inserts seed data on `Up` |
| 4 | `dotnet ef database update -p ... -s ...` | Apply locally |
| 5 | `dotnet ef migrations script -p ... -s ... -i` | Idempotent script → `database/migrations/<ts>_Init_Schema.sql` |

> `dotnet-ef` tool must be version **10.0.0** to match EF Core 10: `dotnet tool install --global dotnet-ef --version 10.0.0`.

### Local Dev

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=YourStrong!Passw0rd \
  -p 1433:1433 --name iid-sql -d mcr.microsoft.com/mssql/server:2022-latest

dotnet ef database update \
  --project src/IID.Infrastructure \
  --startup-project src/IID.Api
```

### Production Apply Order

1. Backup existing `iid` DB (full + log).
2. Run idempotent `database/migrations/0000_PreDeploy_Health.sql` (wait stats update, blocking check).
3. Run `0001_Identity.sql`.
4. Run `0002_Vehicle.sql`.
5. Run `0003_VehicleAction.sql`.
6. Run `0004_IndexesAndViews.sql` (filtered indexes + aging view).
7. Run `0005_SprocsAndFunctions.sql`.
8. Run `0006_Seeds.sql` (only on empty DB; gated by row count check).
9. Post-deploy `0099_PostDeploy_RebuildIndexes.sql`.

---

## 8. Backup & Recovery

| Frequency | Type | Retention |
|---|---|---|
| Every 15 min | Transaction log backup | 24 h |
| Daily 02:00 | Differential backup | 7 d |
| Weekly Sun 01:00 | Full backup | 4 w |
| Monthly | Full backup + offsite copy | 12 mo |

- RPO ≤ 15 min, RTO ≤ 1 h (standard).
- All scripts in `database/backups/`.

---

## 9. Security & Compliance

- Login: dedicated SQL login `iid_app` with `db_owner` on `iid` DB.
- App uses Windows Auth in prod; SQL Auth only when Azure SQL requires it.
- TDE enabled for production (`CREATE DATABASE ENCRYPTION KEY`).
- Always Encrypted optional for `Notes` column in `VehicleAction`.
- Audit: SQL Server Audit → file target → Log Analytics.
- `GRANT SELECT, INSERT, UPDATE, DELETE` on app tables to `iid_app`; deny direct DDL.
- Row-Level Security (optional, per dealership): `Tenants.TenantId` predicate on `Vehicle`.

---

## 10. Performance Targets

| Query | Target p95 |
|---|---|
| List vehicles (page of 20, filtered) | ≤ 80 ms |
| Aging-stock list (page of 20) | ≤ 60 ms (covered by filtered index) |
| Get vehicle by id | ≤ 20 ms |
| Log vehicle action | ≤ 40 ms |

- Initial seed (50k vehicles, 250k actions) must keep aging query p95 ≤ 150 ms.

---

## 11. File Layout

```
database/
├── README.md                          # run/backup instructions
├── migrations/
│   ├── 0001_Identity.sql
│   ├── 0002_Vehicle.sql
│   ├── 0003_VehicleAction.sql
│   ├── 0004_VehicleInventoryHistory.sql
│   ├── 0005_IndexesAndViews.sql
│   ├── 0006_SprocsAndFunctions.sql
│   ├── 0007_Seeds.sql
│   ├── 0099_PostDeploy_RebuildIndexes.sql
│   └── EF_Migrations/                 # EF Core generated scripts mirror
├── seeds/
│   ├── 0001_IdentitySeed.sql
│   ├── 0002_VehiclesSeed.sql
│   └── 0003_VehicleActionsSeed.sql
├── backups/
│   ├── full_backup.sql.cmd
│   ├── log_backup.sql.cmd
│   └── restore_checklist.md
└── scripts/
    ├── create_database.sql
    ├── create_login.sql
    └── health_check.sql
```

---

**Related plans:** [`backend.md`](./backend.md) · [`frontend.md`](./frontend.md)