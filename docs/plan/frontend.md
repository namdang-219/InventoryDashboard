# Frontend Plan — Intelligent Inventory Dashboard

> **Stack:** **Angular 19 (Standalone Components)** · TypeScript 5.7 · Angular Signals & RxJS · SCSS · `@microsoft/signalr` 10.x · Angular Router
> **Architecture:** Feature-based modular SPA, reactive Signals + RxJS event bus, custom responsive SCSS design system, role-guarded lazy routes.

---

## 1. Architecture Decisions

| Area | Implementation Choice | Rationale |
|---|---|---|
| **Framework** | Angular 19 (Standalone) | Modern standalone component architecture (`imports: [...]`), no `NgModule` overhead, strict TypeScript typing. |
| **State & Reactivity** | Angular Signals (`signal<T>`, `computed()`) | Fine-grained reactivity for connection status, active branch, unread notifications, and live UI toggles. |
| **Event Bus & Streams** | RxJS (`Subject<T>`, `Observable<T>`) | Stream management for asynchronous SignalR WebSocket push events and debounce search filters. |
| **HTTP Client** | Angular `HttpClient` + Functional Interceptors | Centralized HTTP with `authInterceptor` (automatic JWT Bearer injection and 401 handling). |
| **Realtime Transport** | `@microsoft/signalr` 10.x | Direct WebSocket connection to `/hubs/inventory` supporting automatic reconnection and dealership room subscriptions. |
| **Styling & UI** | Custom SCSS Design System | Scoped component styles, flexible CSS grid/flexbox, custom stat cards, badges, and dark/light mode via `ThemeService`. |
| **Routing** | Angular Router 19 | Standalone lazy loading (`loadComponent: () => import(...)`), functional route guards (`authGuard`, `managerGuard`). |

---

## 2. Project & Component Layout

```
src/IID.ClientApp/src/app/
├── core/
│   ├── guards/          # authGuard, managerGuard
│   ├── interceptors/    # authInterceptor (JWT bearer)
│   ├── models/          # vehicle.model, dashboard.model, dealership.model, realtime.model, auth.model
│   └── services/        # RealtimeService, InventoryService, DashboardService, DealershipService, AuthService, ToastService
├── layout/
│   ├── shell/           # ShellComponent (App shell layout with router-outlet & toast container)
│   ├── header/          # HeaderComponent (Branch selector, live WebSocket bell, unread badge, profile)
│   └── sidebar/         # SidebarComponent (Nav links: Dashboard, Inventory, Aging Stock, Dealerships)
├── features/
│   ├── auth/            # LoginPageComponent
│   ├── dashboard/       # DashboardPageComponent, QuickStats, SellingTrend, ActionCenter, LowInventoryAlerts
│   ├── vehicles/        # VehicleListPageComponent, VehicleTable, VehicleCard, FilterBar, Modals
│   └── dealerships/     # DealershipListPageComponent, DealershipFormModal
└── shared/
    ├── components/      # StatCard, StatusBadge, DemandBadge, AgingSeverityBadge, LoadingSpinner, EmptyState, Toast
    └── pipes/           # FormatCurrencyPipe, DaysAgoPipe
```

---

## 3. Routes & Authorization

| Route | Component | Guard / Access | Description |
|---|---|---|---|
| `/login` | `LoginPageComponent` | Public | Credentials authentication with demo presets |
| `/` | `ShellComponent` | `authGuard` | Redirects to `/dashboard` |
| `/dashboard` | `DashboardPageComponent` | `authGuard` (`Manager`, `Sales`, `Saler`) | Executive cockpit: KPIs, velocity trend, alerts, action center |
| `/vehicles` | `VehicleListPageComponent` | `authGuard` (`Manager`, `Sales`, `Saler`) | Full inventory roster with search, filter bar, cards/table |
| `/vehicles/aging` | `VehicleListPageComponent` | `authGuard` (`Manager`, `Sales`, `Saler`) | Pre-filtered view for units $>90$ days on lot |
| `/dealerships` | `DealershipListPageComponent` | `managerGuard` (`Manager` only) | Multi-branch management, branch statistics, and creation |
| `**` | Redirect to `/dashboard` | — | Catch-all fallback |

---

## 4. Key Services & State Management

### 4.1 `RealtimeService` (SignalR WebSocket Client)

Maintains the persistent connection to `/hubs/inventory`:
- **State Signals:** `connectionStatus` (`connected`, `reconnecting`, `disconnected`), `activeDealershipId`, `unreadCount`, `recentActivities`.
- **Event Observables:**
  - `vehicleAdded$`, `vehicleUpdated$`, `vehicleRemoved$`, `vehicleAging$`
  - `vehicleActionLogged$`
  - `summaryUpdated$`, `alertsUpdated$`, `inventoryChanged$`
  - `dealershipAdded$`, `dealershipUpdated$`
- **Branch Room Management:** Calls `joinDealership(id)` and `leaveDealership(id)` on the hub when the user changes active branches in the header.

### 4.2 `DashboardService` & `InventoryService`

- **Dashboard:** Calls `GET /api/v1/dashboard?dealershipId={id}&page={page}&pageSize={size}` to fetch the aggregate single-roundtrip bundle.
- **Inventory:** Handles vehicle queries with multi-parameter filtering (`make`, `model`, `vin`, `stockNumber`, `minAgeDays`, `maxAgeDays`, `status`, `dealershipId`, `sort`, `order`).
- **Operational Mutations:**
  - `createVehicle(req)` $\to$ `POST /api/v1/vehicles`
  - `updateVehicle(id, req)` $\to$ `PUT /api/v1/vehicles/{id}`
  - `markSold(id, req)` $\to$ `POST /api/v1/vehicles/{id}/sold`
  - `transferDealership(id, req)` $\to$ `POST /api/v1/vehicles/{id}/transfer`
  - `logAction(id, req)` $\to$ `POST /api/v1/vehicles/{id}/actions`

---

## 5. UI Features & Modals

### 5.1 Interactive Modals

All operational actions open dedicated modal dialogs without leaving the current view:

1. **`CreateVehicleModalComponent`:** Form validating VIN, stock number, make, model, year, color, mileage, fuel type, purchase price, asking price, and target branch.
2. **`EditVehicleModalComponent`:** Modify specifications, pricing, and status on unsold vehicles.
3. **`VehicleDetailModalComponent`:** In-depth unit dossier displaying specs, calculated demand score, lot tenure, pricing margins, and full chronological action history.
4. **`TransferDealershipModalComponent`:** Selects target dealership branch for inter-lot transfer.
5. **`MarkSoldModalComponent`:** Prompts for actual selling price and sale date, permanently locking the vehicle upon submission.
6. **`LogActionModalComponent`:** Logs one of 10 standardized business remediation actions with preset quick notes (*"Reduce 5%"*, *"Wholesale transfer"*, *"Ad Campaign"*).
7. **`DealershipFormModalComponent`:** Add or update branch locations (Code, Name, City, State, Phone).

### 5.2 Badges & Visual Indicators

- **`AgingSeverityBadgeComponent`:** Color-coded chip:
  - `Critical` ($\ge 90$ days) — Red pulse
  - `High` (60–89 days) — Amber
  - `Warning` (30–59 days) — Yellow
  - `None` ($< 30$ days) — Neutral/Green
- **`DemandBadgeComponent`:** High / Medium / Low demand score pill based on vehicle age, fuel type, and lot tenure.
- **`StatusBadgeComponent`:** Available (Green), Pending (Blue), Sold (Purple), Wholesale (Orange).

---

## 6. Build & Development

```bash
cd src/IID.ClientApp

# Install npm dependencies
npm install

# Start development dev server (binds to http://localhost:4200)
npm start

# Run automated Karma/Jasmine unit tests
npm test

# Production build
npm run build
```

---

**Related plans:** [`business.md`](./business.md) · [`backend.md`](./backend.md) · [`database.md`](./database.md)