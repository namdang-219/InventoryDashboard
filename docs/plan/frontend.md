# Frontend Plan — Intelligent Inventory Dashboard

> **Stack:** **React 19** · Vite · TypeScript · Redux Toolkit · React Router · Axios · SCSS · `@microsoft/signalr` 10.x · Vitest + React Testing Library
> **Architecture:** Feature-based modular SPA (`src/IID.FrontendApp`), Redux Toolkit slices + typed event callbacks, custom responsive SCSS design system, role-guarded lazy routes.

---

## 1. Architecture Decisions

| Area | Implementation Choice | Rationale |
|---|---|---|
| **Framework & Bundler** | React 19 + Vite | Fast HMR dev server (port 4200), native ESM bundling, `React.lazy` code-splitting, strict TypeScript typing. |
| **State Management** | Redux Toolkit + React Hooks | Predictable cross-cutting slices (`auth`, `toast`, `theme`, `dealership`, `realtime`) paired with `useState`/`useMemo` for local UI state. |
| **Event Dispatch** | Typed Callback Registry (`realtimeEvents`) | Lightweight zero-RxJS subscription mechanism for asynchronous SignalR WebSocket push events alongside Redux dispatches. |
| **HTTP Client** | Axios + Single-Flight Refresh Interceptor | Centralized HTTP client with `setupAuthInterceptor` (automatic JWT Bearer injection and single-flight 401 refresh queue). |
| **Realtime Transport** | `@microsoft/signalr` 10.x | Direct WebSocket connection to `/hubs/inventory` supporting automatic reconnection and dealership room subscriptions. |
| **Styling & UI** | Custom SCSS Design System | Component-scoped `.scss` files nested under unique `.iid-*` root classes, plus global design tokens and dark/light mode via `themeService`. |
| **Routing** | React Router (`createBrowserRouter`) | Lazy route loading (`React.lazy` + `Suspense`) and declarative route guards (`RequireAuth`, `RequireManager`). |
| **Testing** | Vitest + React Testing Library + jsdom | Fast headless unit and component testing across components, slices, guards, pipes, and HTTP interceptors. |

---

## 2. Project & Component Layout

```
src/IID.FrontendApp/src/app/
├── core/
│   ├── guards/          # RequireAuth, RequireManager (auth.guard.tsx)
│   ├── interceptors/    # setupAuthInterceptor (JWT bearer + 401 refresh queue)
│   ├── models/          # vehicle.model, dashboard.model, dealership.model, realtime.model, auth.model
│   ├── services/        # realtime, inventory, dashboard, dealership, auth, toast, theme, api
│   ├── store/           # store.ts, hooks.ts, auth/toast/theme/dealership/realtime slices
│   └── utils/           # error-extractor.ts, navigation.ts
├── layout/
│   ├── shell/           # ShellComponent (App shell layout with Outlet, Sidebar & Header)
│   ├── header/          # HeaderComponent (Branch selector, live WebSocket bell, unread badge, profile)
│   └── sidebar/         # SidebarComponent (Nav links: Dashboard, Inventory, Aging Stock, Dealerships)
├── features/
│   ├── auth/            # LoginPageComponent
│   ├── dashboard/       # DashboardPageComponent, QuickStats, SellingTrend, ActionCenter, InventoryCharts, LowInventoryAlerts
│   ├── vehicles/        # VehicleListPageComponent, VehicleTable, VehicleCard, FilterBar, Modals
│   └── dealerships/     # DealershipListPageComponent, DealershipFormModalComponent
└── shared/
    ├── components/      # StatCard, StatusBadge, DemandBadge, AgingSeverityBadge, LoadingSpinner, EmptyState, ToastContainer
    └── pipes/           # formatCurrency, daysAgo pure utility functions
```

---

## 3. Routes & Authorization

| Route | Component | Guard / Access | Description |
|---|---|---|---|
| `/login` | `LoginPageComponent` | Public | Credentials authentication with demo presets |
| `/` | `ShellComponent` | `RequireAuth` | Redirects to `/dashboard` |
| `/dashboard` | `DashboardPageComponent` | `RequireAuth` (`Manager`, `Sales`) | Executive cockpit: KPIs, velocity trend, alerts, action center |
| `/vehicles` | `VehicleListPageComponent` | `RequireAuth` (`Manager`, `Sales`) | Full inventory roster with search, filter bar, cards/table |
| `/vehicles/aging` | `VehicleListPageComponent` | `RequireAuth` (`Manager`, `Sales`) | Pre-filtered view for units $>90$ days on lot |
| `/dealerships` | `DealershipListPageComponent` | `RequireManager` (`Manager` only) | Multi-branch management, branch statistics, and creation |
| `*` | Redirect to `/dashboard` | — | Catch-all fallback |

---

## 4. Key Services & State Management

### 4.1 `realtimeService` & `realtime.slice` (SignalR WebSocket Client)

Maintains the persistent connection to `/hubs/inventory`:
- **Redux State:** `connectionStatus` (`connected`, `reconnecting`, `connecting`, `disconnected`), `activeDealershipId`, `unreadCount`, `recentActivities`, `nextCursor`, `hasMoreActivities`, `latestActivityTick`.
- **Realtime Event Subscriptions (`realtimeEvents` / `useRealtimeEvent`):**
  - `vehicleAdded`, `vehicleUpdated`, `vehicleRemoved`, `vehicleAging`
  - `vehicleActionLogged`
  - `summaryUpdated`, `alertsUpdated`, `inventoryChanged`
  - `dealershipAdded`, `dealershipUpdated`
- **Branch Room Management:** Invokes `JoinDealership(id)` and `LeaveDealership(id)` on the hub when the user changes active branches in the header.

### 4.2 `dashboardService` & `inventoryService`

- **Dashboard:** Calls `GET /api/v1/dashboard?dealershipId={id}&page={page}&pageSize={size}` to fetch the aggregate single-roundtrip bundle.
- **Inventory:** Handles vehicle queries with multi-parameter filtering (`make`, `model`, `vin`, `stockNumber`, `minAgeDays`, `maxAgeDays`, `status`, `dealershipId`, `sort`, `order`).
- **Operational Mutations:**
  - `createVehicle(req)` $\to$ `POST /api/v1/vehicles`
  - `updateVehicle(id, req)` $\to$ `PUT /api/v1/vehicles/{id}`
  - `markVehicleSold(id, req)` $\to$ `POST /api/v1/vehicles/{id}/mark-sold`
  - `transferDealership(id, req)` $\to$ `POST /api/v1/vehicles/{id}/transfer`
  - `logVehicleAction(id, req)` $\to$ `POST /api/v1/vehicles/{id}/actions`

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
cd src/IID.FrontendApp

# Install npm dependencies
npm install

# Start Vite development server (binds to http://localhost:4200)
npm start

# Run automated Vitest unit tests
npm test

# Production build (outputs to dist/iid.frontend-app)
npm run build
```

---

**Related plans:** [`business.md`](./business.md) · [`backend.md`](./backend.md) · [`database.md`](./database.md) · [`migration-inventory.md`](./migration-inventory.md)
