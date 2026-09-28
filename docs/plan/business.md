# Business Plan — Intelligent Inventory Dashboard

> **Vision:** Provide dealership managers and sales staff with a real-time, decision-grade inventory control platform to proactively identify aging stock (>90 days), preserve working capital, and streamline remediation workflows.
> **Scope:** This document is the **business source of truth**. It defines *what* the system does, *who* uses it, and *why*. Technical implementation details live in [`backend.md`](./backend.md), [`frontend.md`](./frontend.md), and [`database.md`](./database.md).

---

## 1. Executive Summary

Automotive retail inventories represent a dealership's largest working capital investment. Vehicles sitting on lots past 90 days suffer rapid depreciation and incur daily floor-plan carrying costs ($25–$40/day). The **Intelligent Inventory Dashboard (IID)** provides live visibility into total working capital, lot turnaround velocity, demand heuristics, and multi-branch inventory, turning passive inventory into an active operational workflow.

---

## 2. Bounded Context & Stakeholders

### 2.1 Bounded Context

| Component | Definition |
|---|---|
| **Context Name** | Automotive Dealership Inventory & Supply |
| **Aggregate Roots** | `Vehicle`, `VehicleAction`, `Dealership` |
| **Supporting Entities** | `InventoryActivity`, `Notification`, `UserActivityReadStatus` |
| **Ubiquitous Language** | Vehicle, Inventory, Aging Stock, Demand Score, Dealership Branch, Vehicle Action, Lot Tenure |

### 2.2 Actors & Roles

| Actor | Role / System Name | Key Responsibilities & Permissions |
|---|---|---|
| **Dealership Manager** | `Manager` | Full control: CRUD vehicles, record sales (`MarkSold`), execute inter-branch transfers (`TransferDealership`), log actions, manage dealership profiles. |
| **Sales Staff** | `Sales` | Read-only operations: browse inventory, inspect aging stock, view activities, monitor alert banners. |
| **System** | Background / Realtime | Dispatches post-commit domain events, broadcasts SignalR WebSocket updates, recalculates dashboard KPIs. |

### 2.3 System Scope

- **Multi-Branch Dealership Support:** 10 metropolitan branches seeded out of the box with branch-specific filtering, metrics, and inter-dealership transfers.
- **Single-Roundtrip Dashboard Bundle:** Consolidates executive KPIs, sales velocity charts, powertrain breakdown, aging tiers, and action center into one fast response.
- **Real-Time Synchronization:** All connected clients receive live push events for vehicle additions, edits, sales, transfers, and action logs.

---

## 3. Value Proposition & Key Metrics (KPIs)

| Metric | Calculation / Meaning | Target |
|---|---|---|
| **Total Inventory Value** | $\sum \text{AskingPrice}$ for all active units (`Status` $\ne$ `Sold`) | Real-time working capital visibility |
| **Average Unit Valuation** | $\text{Total Value} \div \text{Active Units}$ | Track portfolio mix |
| **Average Days on Lot** | $\text{Average}(\text{DaysInInventory})$ across active stock | Target turnaround $< 65$ days |
| **Aging Stock Count** | Count of active vehicles with $\text{DaysInInventory} > 90$ | $< 10\%$ of lot volume |
| **Aging Severity Tiers** | Warning (30–59d), High (60–89d), Critical ($\ge$ 90d) | Prioritized triage in Action Center |
| **Demand Score** | Proprietary heuristic (0–100) combining vehicle age, fuel type (EV/Hybrid bonus), and lot tenure | Guide repricing and discount actions |

---

## 4. Core Business Invariants

### 4.1 Vehicle Aggregate (`Vehicle`)

| Rule ID | Rule Description | Enforcement / Invariant |
|---|---|---|
| **V-001** | Unique VIN | 17 characters, ISO 3779 charset `[A-HJ-NPR-Z0-9]`; unique among non-deleted units. |
| **V-002** | Stock Number | Assigned automatically (`STK-{VIN6}`) or custom; unique among non-deleted units. |
| **V-003** | Plausible Model Year | Must satisfy: $1980 \le \text{Year} \le \text{CurrentYear} + 1$. |
| **V-004** | Mileage & Pricing | Mileage $\ge 0$; Purchase and Asking prices must have $\text{Amount} \ge 0$ with ISO 4217 currency. |
| **V-005** | Fuel Type | Enumeration: `Petrol`, `Diesel`, `Hybrid`, `PluginHybrid`, `Electric`. |
| **V-006** | Dealership Association | Every vehicle must belong to a valid `DealershipId`. |
| **V-007** | Inventory Lifecycle Status | Status: `Available` $\to$ `Pending` $\to$ `Sold`, or `Available` $\to$ `Wholesale`. |
| **V-008** | Sold Invariant | Once marked `Sold`, a vehicle's specifications and pricing cannot be edited (`EnsureNotSold`). |
| **V-009** | Wholesale Invariant | `Wholesale` vehicles cannot be sold via the retail flow (`EnsureNotWholesale`). |
| **V-010** | Date Invariant | `DateAddedToInventory` $\le \text{DateTimeOffset.UtcNow}$. |
| **V-011** | Soft Deletion & Audit | Soft-delete sets `DeletedAt`; concurrency guarded by SQL `RowVersion`. |

### 4.2 Vehicle Action Aggregate (`VehicleAction`)

| Rule ID | Rule Description | Enforcement / Invariant |
|---|---|---|
| **A-001** | Vehicle Reference | Must reference an existing, non-deleted vehicle (`VehicleId`). |
| **A-002** | Action Type | Enumeration of 10 types: `PriceReductionPlanned` (0), `PriceReductionExecuted` (1), `TransferToWholesale` (2), `TradeInCustomer` (3), `MarketingCampaign` (4), `DealerAuction` (5), `ManagerReview` (6), `Relist` (7), `Other` (8), `TransferDealership` (9). |
| **A-003** | Notes | Optional string capped at $\le 2000$ characters. |
| **A-004** | Principal Attribution | `LoggedByUserId` set strictly from authenticated JWT identity. |
| **A-005** | Event Enrichment | Action domain events enrich payload with vehicle metadata before dispatching. |

### 4.3 Dealership Aggregate (`Dealership`)

| Rule ID | Rule Description | Enforcement / Invariant |
|---|---|---|
| **D-001** | Branch Uniqueness | `Code` (e.g. `DLR-LA-01`) is unique across all active dealerships. |
| **D-002** | Contact Information | Name, code, city, state, and phone are validated and non-empty. |
| **D-003** | Relational Constraint | Restricts deletion if active vehicles remain assigned to the dealership. |

### 4.4 Authorization & Access Control

| Rule ID | Rule Description | Enforcement |
|---|---|---|
| **AU-001** | Manager Operations | Vehicle creation, updates, deletes, transfers, mark-sold, action logging, and dealership CRUD require `Roles("Manager")`. |
| **AU-002** | Read Operations | Dashboard bundle, vehicle list, aging stock, activity stream, and alerts allow `Roles("Manager", "Sales")`. |
| **AU-003** | Public Endpoints | Authentication routes (`/api/v1/auth/login`, `/api/v1/auth/refresh-token`) are publicly accessible. |

---

## 5. Domain Events

| Event | Business Meaning | Downstream Action |
|---|---|---|
| `VehicleAdded` | New unit added to lot | Broadcast to dealership & dashboard SignalR groups; log activity |
| `VehicleUpdated` | Vehicle specs/price modified | Broadcast update to clients; recalculate dashboard KPIs |
| `VehicleStatusChanged` | Status transition (e.g. Available $\to$ Pending) | Update real-time counts, pipeline charts, and activity feed |
| `VehicleSold` | Unit marked as sold with final sale price | Finalize margin metrics; broadcast sale event; refresh summaries |
| `VehicleTransferred` | Unit transferred between dealership branches | Notify origin and destination branch channels; update lot rosters |
| `VehicleRemoved` | Vehicle soft-deleted | Remove unit from active listings across all clients |
| `VehicleActionLogged` | Manager logged remediation proposal | Append to action history; broadcast update to connected dashboards |
| `DealershipAdded` / `Updated` | Dealership branch created or edited | Synchronize branch selector list across active users |

---

## 6. Business Use Cases

- **UC-1: Executive Cockpit Overview:** Manager selects a dealership (or all branches) to view working capital, turnaround velocity, 12-month sales trend, powertrain breakdown, aging alerts, and the prioritized Action Center.
- **UC-2: Multi-Branch Inventory Browsing:** Users filter inventory by dealership, make, model, VIN, stock number, age range, fuel type, and status with pagination and column sorting.
- **UC-3: Inter-Branch Vehicle Transfer:** Manager selects a new target dealership; system validates status is not sold, updates branch assignment, and emits `VehicleTransferred`.
- **UC-4: Aging Stock Remediation:** Manager inspects units in Warning (30–59d), High (60–89d), or Critical ($\ge$ 90d) status and logs quick-actions (*Price Reduction*, *Wholesale Transfer*, *Marketing Push*).
- **UC-5: Closing a Sale:** Manager marks vehicle sold with actual selling price and timestamp; vehicle is permanently locked against further edits.
- **UC-6: Real-time Notifications & Feed:** Sales and management view incoming WebSocket activity events (ring bell indicator, toast notifications, unread count tracking).

---

**Related plans:** [`backend.md`](./backend.md) · [`frontend.md`](./frontend.md) · [`database.md`](./database.md)