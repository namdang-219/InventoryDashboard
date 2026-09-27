# Intelligent Inventory Dashboard — Business Requirements

> **Audience:** Dealership managers and sales staff. Plain-English guide to what the system does, every feature, and how every Inventory Overview number is calculated.

## 1. What the System Does

A real-time control panel for vehicle stock. Managers see, at a glance, what is on the lot, which units are aging, and what to do next. Every screen updates live: when a salesperson marks a unit sold or a manager logs a price reduction, every connected dashboard reflects it within seconds.

**Who uses it** — **Manager:** full access (add/edit/sell/transfer/delete vehicles, log actions). **Sales / Viewer:** read-only (browse lot, see aging stock, read activity feed).

**Core ideas**

| Term | Meaning |
|---|---|
| **Vehicle** | A single car on the lot, identified by its 17-character VIN. |
| **Inventory** | Every vehicle the dealership owns. |
| **Status** | Available, Pending, Sold, or Wholesale. |
| **Aging stock** | A car on the lot too long (see §4). |
| **Action** | A note the manager leaves about a decision on a vehicle. |
| **Dashboard** | The opening screen — KPIs, charts, alerts, inventory list. |

## 2. Inventory Overview Dashboard — how every number is calculated

The dashboard is composed of KPI tiles, charts, action items, alerts, and an inventory list — all driven by the same analytics query (top 1,000 vehicles).

### 2.1 KPI tiles & cards — meaning and formula

| KPI | Meaning | Formula |
|---|---|---|
| **Total Inventory** | All cars tracked | `COUNT(all)` |
| **Available** | Cars we can sell today | `COUNT(Status = Available)` |
| **Pending** | Cars with a deal in progress | `COUNT(Status = Pending)` |
| **Sold** | Cars already sold | `COUNT(Status = Sold)` |
| **Wholesale** | Cars routed to wholesale | `COUNT(Status = Wholesale)` |
| **Aging Count** | Cars past 90 days | `COUNT(active AND DaysInInventory > 90)` |
| **Aging — Warning** | 31–60 days on lot | `COUNT(active AND 30 < Days ≤ 60)` |
| **Aging — High** | 61–90 days on lot | `COUNT(active AND 60 < Days ≤ 90)` |
| **Aging — Critical** | 91+ days on lot | `COUNT(active AND Days > 90)` |
| **Total Inventory Value** | Asking-price value of active lot | `SUM(AskingPrice)` over `Status ≠ Sold` |
| **Avg Asking Price** | Typical sticker | `Total Inventory Value ÷ Active count` |
| **Avg Days on Lot** | Average wait | `AVERAGE(DaysInInventory)` over active vehicles |
| **Top 5 Makes** | Brands we carry most | `GROUP BY Make, ORDER BY COUNT DESC, TAKE 5` |
| **Fuel Mix** | Petrol / Hybrid / EV split | `GROUP BY FuelType, COUNT` |
| **Demand Distribution** | Low / Med / High demand split | `GROUP BY DemandLevel, COUNT` (see §5) |

All money uses ISO 4217 currency. All timestamps are UTC.

### 2.2 Charts

| Chart | Grouping |
|---|---|
| Status Breakdown (donut) | `GROUP BY Status` |
| Fuel Breakdown | `GROUP BY FuelType` |
| Aging Histogram (0–30 / 31–60 / 61–90 / 91–180 / 180+ days) | Days-on-lot bucket |
| Monthly Sales (last 12 months) | `GROUP BY Year-Month of SoldAt`, revenue = `SUM(SoldPrice)` |

### 2.3 Action Center, List & Alerts

**Action Center** — Flags vehicles needing attention, sorted: critical aging → high aging → low-demand. Within each tier: lowest demand-score first, then longest days-on-lot. Up to 20 items.

**Inventory List** — Paginated table (20/page default, max 100): stock number, VIN, year/make/model, color, mileage, fuel type, status, asking price, days in inventory, aging severity, demand score.

**Alerts strip** — Top 10 aging warnings, newest first, with severity (Warning / High / Critical).

## 3. Vehicle Lifecycle

```
Available ──► Pending ──► Sold
   │              │
   │              └─► Available (deal fell through)
   └──► Wholesale (routed to wholesale channel)
```

| Status | Allowed |
|---|---|
| **Available** | Edit, transfer, sell, wholesale, delete. |
| **Pending** | Edit, transfer, sell. |
| **Sold** | **No further edits — status is permanent.** |
| **Wholesale** | Edit allowed; **cannot be sold via retail flow.** |

**Soft delete** — Hides a vehicle from the UI but keeps it in the database for audit.

## 4. Aging Stock

The single most important concept for managers. A vehicle starts aging the moment it rolls onto the lot.

| Severity | Days on lot | What it means |
|---|---|---|
| **None** | 0–30 | Fresh — normal sales window. |
| **Warning** | 31–60 | Getting stale. Watch closely. |
| **High** | 61–90 | Danger zone. Consider a price cut or extra marketing. |
| **Critical** | 91+ | Bleeding margin. Act now: price cut, trade-in, wholesale, or auction. |

**Days on lot** = `Today − Date the car arrived`, rounded to whole calendar days. For sold cars the clock stops on the **sold date** so historical numbers don't grow forever. Whenever a vehicle crosses the 30-, 60-, or 90-day line, a fresh alert is pushed to every connected dashboard within seconds — no manual refresh.

## 5. Demand Score

For every active vehicle the system computes a **demand score 0–100** estimating how fast the unit will sell at its current asking price. Three signals feed the heuristic:

- **Model year vs. current year** — newer = higher score.
- **Fuel type** — EV / Plug-in Hybrid / Hybrid score higher than Petrol; Diesel scores lower.
- **Days already on lot** — longer wait → score drops.

| Score | Level | Interpretation |
|---|---|---|
| 70–100 | **High** | Hot unit — should sell fast. |
| 40–69 | **Medium** | Normal pace expected. |
| 0–39 | **Low** | Slow mover — flagged in Action Center. |

Sold vehicles always score 0.

## 6. Manager Actions

When a manager decides what to do with a vehicle, they log an **action** — a permanent note in the vehicle's history.

| Action type | When to use |
|---|---|
| **Price Reduction Planned** | Decided to drop the price — not yet applied. |
| **Price Reduction Executed** | The price cut was actually applied. |
| **Transfer to Wholesale** | Sent to wholesale channel. |
| **Trade-In Customer** | Acquired from a customer trade-in. |
| **Marketing Campaign** | Marketing push started. |
| **Dealer Auction** | Listed on the dealer auction. |
| **Manager Review** | Flagging for a deeper review. |
| **Relist** | Re-listed after a previous attempt. |
| **Transfer Dealership** | Moved to a sister dealership. |
| **Other** | Anything that doesn't fit a defined type. |

Each action takes optional notes (up to 2,000 characters). All actions are stamped with who logged them and when — server-side.

## 7. Real-Time Updates (Realtime)

The dashboard never needs a manual refresh. Every change is pushed to connected users instantly.

| Event | What the user sees |
|---|---|
| **Vehicle added** | New row appears in the list. |
| **Vehicle updated** | Row updates in place — new price, status, mileage. |
| **Vehicle removed** | Row disappears. |
| **Vehicle aging** | Aging flag lights up; severity updates. |
| **Action logged** | Action appears in feed and vehicle history. |
| **Dashboard summary refresh** | KPI tiles and charts re-compute. |
| **Alerts refresh** | Alerts strip updates. |
| **Inventory changed** | Global ping triggers a refresh check. |

**Dealership scoping** — Inventory updates are scoped per dealership: transferring a car from A to B, both dealerships see it instantly — A sees it leave, B sees it arrive. Sold/removed events broadcast globally so stale dashboards get cleaned up.

## 8. Activity Feed & Notifications

Every meaningful change writes a row to a global activity feed.

- **Unread count** — bell-icon badge shows how many feed items are unread.
- **Mark read** — single click on an item, or "Mark all as read" for the whole feed.
- **Optimized counting** — exact count for small inboxes; fast approximate count for very large ones.

## 9. Authentication & Roles

| Action | Manager | Sales / Viewer |
|---|---|---|
| View inventory, dashboard, aging stock, actions | ✅ | ✅ |
| Add / edit / sell / transfer / delete vehicles | ✅ | ❌ |
| Log / edit / delete vehicle actions | ✅ | ❌ |
| Manage dealerships | ✅ | ❌ |

Every endpoint requires a JWT bearer token (short-lived; a refresh endpoint issues a new one without re-entering credentials). Display names in the feed are resolved with a small cache so users never see raw IDs.

## 10. Data Rules & Targets

**Invariants (enforced, cannot be bypassed):** VIN unique and ISO 3779 · Year 1980→next · Mileage ≥ 0 · Prices ≥ 0, ISO 4217 · Status ∈ {Available, Sold, Pending, Wholesale} · DateAddedToInventory ≤ today · Sold = immutable · Wholesale ≠ retail-sellable · every change audited (Created/Updated timestamps + user IDs) · optimistic concurrency (two editors → one wins, the other reloads).

| Success metric | Target |
|---|---|
| Time from "day 91 on lot" → "manager sees alert" | < 1 minute |
| Aging-flag → action logged (median) | < 24 hours |
| Vehicles aging past 90 days | < 8% of active inventory |
| Aging vehicles with an action within 7 days | ≥ 90% |

## 11. Glossary

| Term | Meaning |
|---|---|
| **Aging Stock** | Car on the lot more than 90 days. |
| **Dealership** | One physical store; owns its own vehicles. |
| **Demand Score** | 0–100 predicting how fast a vehicle will sell. |
| **Realtime** | Updates pushed to connected users within seconds. |
| **Wholesale** | Routed to wholesale/auction instead of retail. |

---

*For technical implementation details, see `/docs/plan`.*
