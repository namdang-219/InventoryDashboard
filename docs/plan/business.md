# Business Plan — Intelligent Inventory Dashboard

> **Vision:** Give dealership managers a real-time, decision-grade view of vehicle stock so aging units get acted on before they bleed margin.
> **Scope:** This document is the **business source of truth**. It defines *what* the system does, *who* uses it, and *why*. Technical *how* lives in [`backend.md`](./backend.md), [`frontend.md`](./frontend.md), and [`database.md`](./database.md). When this file and a technical plan disagree, **this file wins** until amended.

---

## 1. Executive Summary

Dealership managers today rely on weekly snapshots, spreadsheets, and gut feel. Units sit on the lot past 90 days, depreciation compounds, and the manager finds out only after the next inventory turn is already late. The Intelligent Inventory Dashboard surfaces every vehicle, surfaces the ones that are aging in real time, and makes it a one-click action to log a proposal (price reduction, trade-in evaluation, wholesale listing, etc.) — so the manager's next move is always one screen away.

---

## 2. Bounded Context & Stakeholders

### 2.1 Bounded Context

| | |
|---|---|
| **Context name** | Dealership Inventory |
| **Aggregate roots** | `Vehicle`, `VehicleAction` |
| **Ubiquitous language** | vehicle, inventory, aging stock, status, action, manager |
| **Upstream context** | Acquisitions (vehicle intake) — out of scope; `Vehicle` is fed via API/seed in v1 |
| **Downstream context** | Reporting & Finance — out of scope; consumes future event stream |

### 2.2 Actors & Roles

| Actor | Role in system | Key actions |
|---|---|---|
| **Dealership Manager** | Owner of inventory decisions | CRUD vehicles, log/propose actions on aging vehicles, see dashboard |
| **Viewer (Sales / Floor)** | Read-only consumer | View list, view aging stock, view action history |
| **System** | Background processes | Detect aging-stock crossings, broadcast real-time updates, enforce invariants |

### 2.3 Out of Scope (v1)

- Acquisition / intake workflow
- Customer-facing inventory pages
- Multi-tenant dealership isolation (single dealership per deployment in v1)
- Reporting / finance integrations
- Mobile native apps

---

## 3. Value Proposition & KPIs

| Value lever | KPI | Target (v1) |
|---|---|---|
| Faster detection of aging stock | Time from "day 91 on lot" to "manager views alert" | < 1 minute (real-time SignalR push) |
| Faster action on aging stock | Median time from aging-flag → logged action | < 24 h |
| Reduce aged-inventory carrying cost | % of vehicles aging past 90 days | < 8% of active inventory |
| Operational transparency | Actions logged per aging vehicle | ≥ 90% of aging vehicles have at least one action within 7 days of aging |

---

## 4. Core Business Rules (Ubiquitous Language as Code)

These rules are encoded as **invariants** in the Domain layer and **CHECK constraints** in the database. Tests in `IID.Domain.Tests` must cover each one.

### 4.1 Vehicle Aggregate

| ID | Rule | Invariant |
|---|---|---|
| V-001 | A vehicle must have a unique, valid VIN | `Vin` VO: 17 chars, ISO 3779 charset `[A-HJ-NPR-Z0-9]`; unique among active vehicles |
| V-002 | Model year must be plausible | `1900 ≤ Year ≤ currentYear + 1` |
| V-003 | Mileage must be non-negative | `Mileage >= 0` |
| V-004 | Prices must be non-negative and ISO-currency | `Money.Amount >= 0`; `Money.Currency` is ISO 4217 |
| V-005 | Status must be one of `Available`, `Sold`, `Pending`, `Wholesale` | enum constraint |
| V-006 | `DateAddedToInventory` cannot be in the future | `≤ DateTimeOffset.UtcNow` |
| V-007 | A vehicle is **aging stock** when `DaysInInventory > 90` | computed by `AgingStockIdentifier` |
| V-008 | Soft-deletion preserves history | `DeletedAtUtc` set; rows remain in `VehicleInventoryHistory` |
| V-009 | Optimistic concurrency on every mutation | `RowVersion` enforced at DB |
| V-010 | A vehicle's audit fields are immutable by API callers | `CreatedAtUtc`, `CreatedByUserId` set on create only |

### 4.2 VehicleAction Aggregate

| ID | Rule | Invariant |
|---|---|---|
| A-001 | An action must reference an existing, **non-deleted** vehicle | FK to `Vehicle.Id`; 404 if soft-deleted |
| A-002 | Action type must be one of the six defined options | enum constraint |
| A-003 | Notes are optional but capped | `≤ 2000` chars |
| A-004 | `LoggedByUserId` is the authenticated principal | populated from `ICurrentUser.Id`; never trusted from request body |
| A-005 | `LoggedAtUtc` is server-set | defaults to `SYSUTCDATETIME()` on insert |
| A-006 | One action per vehicle per manager per action-type within a sliding window | — *(v2 rule; not enforced in v1)* |

### 4.3 Aging Stock

| ID | Rule | Invariant |
|---|---|---|
| AG-001 | Threshold is **90 calendar days** from `DateAddedToInventory` | `InventoryPolicy.AgingStockThresholdDays = 90` |
| AG-002 | Crossing the threshold raises `VehicleAgingThresholdReachedEvent` exactly once | `AgingStockMonitorService` records last-flagged timestamp |
| AG-003 | The "aging" flag is computed at read time using `SYSUTCDATETIME()` | never persisted (no `IsAging` column on `Vehicle`) |
| AG-004 | Aging stock is queryable independently from the full list | `GET /api/v1/vehicles/aging-stock`; backed by `vw_AgingStock` |

### 4.4 Authorization

| ID | Rule | Invariant |
|---|---|---|
| AU-001 | Vehicle mutation requires the `Manager` role | endpoint-level `Roles("Manager")` |
| AU-002 | Vehicle read is allowed for both `Manager` and `Viewer` | endpoint-level allow |
| AU-003 | VehicleAction mutation requires `Manager` | endpoint-level `Roles("Manager")` |
| AU-004 | VehicleAction read is allowed for both roles | endpoint-level allow |
| AU-005 | Anonymous access is rejected on all routes except `/auth/*` | JWT bearer middleware |

---

## 5. Domain Events (Business Meaning)

| Event | Business meaning | Consumer reaction |
|---|---|---|
| `VehicleAddedToInventoryEvent` | A new unit is on the lot | Push to dashboards, increment "Recent arrivals" tile |
| `VehicleUpdatedEvent` | A unit's facts changed (price, status, mileage) | Push to dashboards so all managers see consistent state |
| `VehicleRemovedEvent` | A unit was deleted (correction, never sold in v1) | Push to dashboards, remove from lists |
| `VehicleAgingThresholdReachedEvent` | A unit just crossed 90 days on the lot | Light up the "Aging Stock" tile, raise alert, prompt action |
| `VehicleActionLoggedEvent` | A manager has decided what to do with an aging unit | Append to that vehicle's action history, push to dashboards |

All events are **after-commit** and **idempotent** at the consumer.

---

## 6. Use Cases (Business Workflows)

### UC-1: View real-time inventory
1. Manager opens `/vehicles`.
2. System lists all non-deleted vehicles paged 20/page, with `daysInInventory` and `isAging` flags.
3. Manager applies filters (make, model, age range, status).
4. SignalR pushes any subsequent create/update/remove to the list without refresh.

### UC-2: Identify aging stock
1. Manager opens `/aging-stock`.
2. System lists vehicles where `DaysInInventory > 90`, ordered by oldest first.
3. Header chip shows live count from `InventoryStore.agingVehicleIds`.
4. When a vehicle crosses 90 days, the chip increments in real time.

### UC-3: Log a proposed action on an aging vehicle
1. From an aging row, Manager clicks **Log Action**.
2. Modal collects `ActionType` (required) and `Notes` (optional).
3. System validates the vehicle is active, persists the action, raises `VehicleActionLoggedEvent`.
4. Action appears in the vehicle's history within the next SignalR tick.

### UC-4: Background aging scan
1. Every 15 min, `AgingStockMonitorService` scans active vehicles added between 80–91 days ago.
2. For each crossing the threshold for the first time, it raises `VehicleAgingThresholdReachedEvent`.
3. Dashboard clients update within seconds.

---

## 7. Success Metrics & Reporting

| Metric | Source | Frequency |
|---|---|---|
| Aging-stock percentage | `vw_AgingStock` | Real-time tile; weekly report |
| Median time aging → action logged | `VehicleAction.LoggedAtUtc` − `DateAddedToInventory + 90d` | Daily |
| Action types distribution | `GROUP BY ActionType` | Weekly |
| Aging vehicles without actions after 7 days | Anti-join `vw_AgingStock ⨝ VehicleAction WHERE NOT EXISTS` | Daily alert |
| Most-aged vehicle | `ORDER BY DateAddedToInventoryUtc ASC LIMIT 1` | Real-time tile |

---

## 8. Compliance, Privacy & Operational Constraints

| Topic | Constraint |
|---|---|
| Personal data | None collected in v1 (no customer PII). Future customer-facing flows require a privacy review. |
| Audit | Every mutation writes a `VehicleInventoryHistory` row with before/after JSON snapshots. |
| Soft delete | Default; hard delete only via DBAs in audit cases. |
| Localization | `en-US` only in v1. Currency stored as ISO 4217; UI displays in the configured currency. |
| Time | All timestamps UTC; UI formats per dealership locale. |
| Data retention | `VehicleInventoryHistory` retained ≥ 7 years for finance/audit. |
| SLA | p95 latency targets documented in [`database.md`](./database.md) §10. |

---

## 9. Glossary (Ubiquitous Language)

| Term | Definition |
|---|---|
| **Vehicle** | A single physical unit in the dealership's inventory, identified by VIN. |
| **Inventory** | The set of all non-deleted vehicles. |
| **Aging stock** | A vehicle whose `DateAddedToInventory` is more than 90 days ago at read time. |
| **Status** | Where the vehicle sits in the sales lifecycle: Available, Sold, Pending, Wholesale. |
| **Action** | A manager's logged proposal or decision on an aging vehicle. |
| **Action type** | One of six predefined categories (Price Reduction Planned, Trade-In Evaluation, Wholesale Listed, Manager Review, Relist, Other). |
| **Dashboard** | The manager-facing SPA at the root of this product. |

---

## 10. Change Control

- Any change to a **business rule** (section 4) or a **KPI** (section 3) requires an amendment to this file in the same PR.
- The Domain layer's invariants must change in lockstep — Domain tests must be updated.
- The DB `CHECK` constraints must change in lockstep — a migration is required.
- Technical docs (`backend.md`, `frontend.md`, `database.md`) are updated to reflect the new rule but do not own the truth.

**Related plans:** [`backend.md`](./backend.md) · [`frontend.md`](./frontend.md) · [`database.md`](./database.md)