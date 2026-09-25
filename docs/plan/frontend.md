# Frontend Plan — Intelligent Inventory Dashboard

> **Stack:** React 18 + TypeScript (strict), Vite, React Router, **MobX 6** (global / UI state), TanStack Query 5 (server cache), Axios, **SignalR JS client** (`@microsoft/signalr` 8.x — matches ASP.NET Core 10 server), MUI 5, SCSS Modules, Vitest + Testing Library, ESLint + Prettier.
> **Backend target:** .NET 10 LTS — see [`backend.md`](./backend.md)
> **Style note:** The workspace rule recommends Zustand for client state and TanStack Query for server state. This plan explicitly diverges per user request and uses **MobX** for client state. All other workspace standards (Container/Presentational, one-folder-per-component, CSS Modules, no inline styles, no `any`, hooks-only) are preserved.

---

## 1. Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Server state | TanStack Query | Caching, retries, background refetch for REST endpoints |
| Real-time state | MobX `InventoryStore` | Push events from SignalR mutate observables; queries re-fetch invalidation where needed |
| Client/UI state | MobX `UIStore`, `AuthStore` | Sidebar open, theme, modal state, current user roles |
| Forms | React Hook Form + Zod | Typed schemas, simple validation |
| Real-time transport | `@microsoft/signalr` | Matches backend hub contract |
| Styling | SCSS Modules + MUI tokens | No inline `style`; MUI customization via CSS Modules |
| Routing | React Router 6 | `/`, `/vehicles`, `/vehicles/:id`, `/aging-stock`, `/login` |
| Testing | Vitest + React Testing Library | Unit + integration; one test per component folder |
| Build | Vite | Fast HMR, ESM |

### Container / Presentational Split

- **Containers** (e.g. `VehiclesPage`, `AgingStockPanel`): subscribe to MobX stores, invoke TanStack Query hooks, pass data + callbacks down.
- **Presentationals** (e.g. `VehicleTable`, `AgingBadge`): typed props only; no API/store mutations; pure UI.

### Feature Layout

```
src/
├── app/                       # Router, providers (QueryClientProvider, MobX root store)
├── stores/                    # MobX root store + individual stores
│   ├── RootStore.ts
│   ├── InventoryStore.ts      # Real-time deltas from SignalR
│   ├── VehicleActionsStore.ts # Aging vehicle actions cache
│   ├── AuthStore.ts           # Current user + JWT
│   └── UIStore.ts             # Sidebar, theme, modals
├── services/
│   ├── http.ts                # Axios instance w/ JWT interceptor
│   ├── queryClient.ts
│   └── signalr/
│       └── inventoryHub.ts    # HubConnection lifecycle
├── features/
│   ├── auth/
│   ├── vehicles/
│   ├── aging-stock/
│   └── vehicle-actions/
├── components/                # Cross-cutting UI (AppShell, DataTable, etc.)
├── hooks/                     # Cross-cutting hooks
├── theme/                     # MUI theme + tokens
├── types/                     # Shared DTOs/types
└── shared/                    # utils
```

### One Folder per Component

```
VehicleTable/
├── VehicleTable.tsx
├── VehicleTable.scss
├── VehicleTable.test.tsx
└── index.ts
```

---

## 2. Routes & Permissions

| Route | View | Roles |
|---|---|---|
| `/login` | `LoginPage` | public |
| `/` → redirect to `/vehicles` | n/a | both |
| `/vehicles` | `VehiclesPage` (full list + filters) | both |
| `/vehicles/:id` | `VehicleDetailPage` | both |
| `/aging-stock` | `AgingStockPage` (highlighted list) | both |
| `/vehicle-actions` | `VehicleActionsPage` (all logged actions) | Manager only |
| `*` | `NotFoundPage` | — |

- `PrivateRoute` wraps protected routes; reads roles from `AuthStore`.
- Viewers see the read-only UI; Manager-only controls are hidden (not disabled).

---

## 3. API Contracts (typed)

```ts
// types/vehicle.ts
export interface Vehicle {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  color: string;
  mileage: number;
  purchasePrice: number;
  askingPrice: number;
  status: 'Available' | 'Sold' | 'Pending' | 'Wholesale';
  dateAddedToInventory: string; // ISO 8601
  daysInInventory: number;
  isAging: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface VehicleAction {
  id: string;
  vehicleId: string;
  actionType:
    | 'PriceReductionPlanned'
    | 'TradeInEvaluation'
    | 'WholesaleListed'
    | 'ManagerReview'
    | 'Relist'
    | 'Other';
  notes?: string;
  loggedByUserId: string;
  loggedAt: string;
}

// types/paged.ts
export interface PagedResponse<T> {
  data: T[];
  meta: { page: number; limit: number; total: number };
}

export interface ApiError {
  error: { code: number; message: string; details: unknown[] };
}
```

---

## 4. TanStack Query (Server Cache)

```ts
// features/vehicles/api/queries.ts
export const useVehiclesQuery = (filters: VehicleFilters) =>
  useQuery({
    queryKey: ['vehicles', filters],
    queryFn: () => api.get<PagedResponse<Vehicle>>('/api/v1/vehicles', { params: filters }),
    staleTime: 30_000,
  });

export const useAgingStockQuery = (page: number) =>
  useQuery({
    queryKey: ['aging-stock', page],
    queryFn: () => api.get<PagedResponse<Vehicle>>('/api/v1/vehicles/aging-stock', { params: { page } }),
    staleTime: 30_000,
  });

export const useVehicleQuery = (id: string) =>
  useQuery({ queryKey: ['vehicle', id], queryFn: () => api.get<{ data: Vehicle }>(`/api/v1/vehicles/${id}`) });

export const useCreateVehicleMutation = () =>
  useMutation({ mutationFn: (cmd: CreateVehicleCommand) => api.post('/api/v1/vehicles', cmd) });

export const useLogVehicleActionMutation = (vehicleId: string) =>
  useMutation({
    mutationFn: (cmd: LogVehicleActionCommand) =>
      api.post(`/api/v1/vehicles/${vehicleId}/actions`, cmd),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['vehicle-actions', vehicleId] }),
  });
```

- `QueryClient` global defaults: `retry: 1`, `refetchOnWindowFocus: true` (dashboard expects liveness).
- Mutations invalidate relevant query keys; SignalR events also call `qc.invalidateQueries` where appropriate.

---

## 5. MobX Stores

### 5.1 `RootStore`

```ts
export class RootStore {
  auth = new AuthStore(this);
  inventory = new InventoryStore(this);
  vehicleActions = new VehicleActionsStore(this);
  ui = new UIStore(this);
}
```

Provided once at app root via a React context (`StoresProvider`).

### 5.2 `InventoryStore` (real-time deltas)

```ts
export class InventoryStore {
  recentAdds = observable.array<Vehicle>([], { deep: false });
  recentUpdates = observable.map<string, Vehicle>();
  agingVehicleIds = observable.set<string>();
  liveBadgeCount = 0;          // updated aging count, for header chip
  private root: RootStore;

  constructor(root: RootStore) { this.root = root; makeAutoObservable(this); }

  applyVehicleAdded = (v: Vehicle) => { this.recentAdds.unshift(v); if (this.recentAdds.length > 50) this.recentAdds.pop(); };
  applyVehicleUpdated = (v: Vehicle) => { this.recentUpdates.set(v.id, v); };
  applyVehicleRemoved = (id: string) => { this.recentUpdates.delete(id); this.agingVehicleIds.delete(id); };
  applyVehicleAging = (v: Vehicle) => { this.agingVehicleIds.add(v.id); this.liveBadgeCount = this.agingVehicleIds.size; };
  applyActionLogged = (a: VehicleAction) => { this.root.vehicleActions.applyActionLogged(a); };

  get hasLiveBadge(): boolean { return this.liveBadgeCount > 0; }
}
```

- Consumed via small `observer` selectors (`useInventoryBadgeCount()`).
- Real-time deltas **augment** server state, do not replace it; queries still drive the canonical lists.

### 5.3 `VehicleActionsStore`

```ts
export class VehicleActionsStore {
  byVehicleId = observable.map<string, VehicleAction[]>(); // newest first, cap 100

  applyActionLogged = (a: VehicleAction) => {
    const list = this.byVehicleId.get(a.vehicleId) ?? [];
    this.byVehicleId.set(a.vehicleId, [a, ...list].slice(0, 100));
  };
}
```

### 5.4 `AuthStore`

```ts
export class AuthStore {
  token: string | null = null;
  user: { id: string; email: string; roles: string[] } | null = null;

  constructor(private root: RootStore) { makeAutoObservable(this); this.hydrate(); }

  hydrate = () => { /* read token/user from localStorage */ };
  login = async (email: string, password: string) => { /* POST /auth/login */ };
  logout = () => { this.token = null; this.user = null; };

  get isManager() { return this.user?.roles.includes('Manager') ?? false; }
}
```

### 5.5 `UIStore`

```ts
export class UIStore {
  sidebarOpen = true;
  logActionModalOpen = false;
  logActionModalVehicleId: string | null = null;

  constructor(private root: RootStore) { makeAutoObservable(this); }

  openLogActionModal = (vehicleId: string) => { this.logActionModalVehicleId = vehicleId; this.logActionModalOpen = true; };
  closeLogActionModal = () => { this.logActionModalOpen = false; this.logActionModalVehicleId = null; };

  toggleSidebar = () => { this.sidebarOpen = !this.sidebarOpen; };
}
```

---

## 6. SignalR Integration

```ts
// services/signalr/inventoryHub.ts
export class InventoryHubClient {
  private connection: HubConnection | null = null;
  constructor(private root: RootStore) {}

  start = async (token: string) => {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_API_BASE}/hubs/inventory`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.connection.on('VehicleAdded', v => this.root.inventory.applyVehicleAdded(v));
    this.connection.on('VehicleUpdated', v => this.root.inventory.applyVehicleUpdated(v));
    this.connection.on('VehicleRemoved', id => this.root.inventory.applyVehicleRemoved(id));
    this.connection.on('VehicleAging', v => this.root.inventory.applyVehicleAging(v));
    this.connection.on('VehicleActionLogged', a => this.root.inventory.applyActionLogged(a));

    await this.connection.start();
  };

  stop = () => this.connection?.stop();
}
```

- Started once after `AuthStore.login` succeeds; restarted on token refresh.
- On reconnect, `qc.invalidateQueries({ queryKey: ['vehicles'] })` and `['aging-stock']` are fired.

---

## 7. Filters & Aging Calculation

- Filter UI: `make`, `model`, `minAgeDays`, `maxAgeDays`, `status`, `page`, `limit`, `sort`, `order`.
- Backend already returns `daysInInventory` + `isAging` per vehicle — no client recalculation required for accuracy.
- `AgingStockPage` is a thin wrapper over `useAgingStockQuery`; tiles highlight `isAging === true`.

---

## 8. Component Inventory (per feature folder)

### `features/vehicles`
- `VehiclesPage` (container)
- `VehiclesFiltersBar` (presentational)
- `VehicleTable` (presentational, MUI Table inside SCSS module)
- `VehicleRow` (presentational)
- `VehicleStatusChip` (presentational, MUI Chip)
- `VehicleCreateModal` (container — uses React Hook Form + Zod + `useCreateVehicleMutation`)
- `VehicleDetailPage` (container)
- `VehicleDetailHeader` (presentational)
- `hooks/useVehicleFilters.ts` (encapsulates URL ↔ state sync)

### `features/aging-stock`
- `AgingStockPage` (container)
- `AgingStockPanel` (presentational)
- `AgingBadge` (presentational — uses `InventoryStore.agingVehicleIds`)

### `features/vehicle-actions`
- `LogVehicleActionModal` (container — opened by `UIStore`)
- `VehicleActionsList` (presentational — reads `VehicleActionsStore.byVehicleId`)
- `VehicleActionItem` (presentational)

### `features/auth`
- `LoginPage`, `LoginForm`, `PrivateRoute`

### Cross-cutting
- `AppShell`, `Sidebar`, `Header` (with `AgingBadge`), `DataTable`, `Pagination`, `ConfirmDialog`, `ErrorBoundary`, `ToastProvider`

---

## 9. State Boundary Rules

- Presentational components **never** call MobX or TanStack Query directly.
- Containers own the wiring; all callbacks passed as typed props.
- No `any` anywhere; event handlers explicitly typed (`React.MouseEvent<HTMLButtonElement>`, `React.ChangeEvent<HTMLInputElement>`).
- No inline `style={{ ... }}` — only SCSS modules + MUI theme tokens.
- Components target ≤ 150 lines; subcomponents extracted when complexity grows.

---

## 10. Error & Empty States

- `QueryClient` global error handler → toast via `ToastProvider`.
- Each list view distinguishes `loading | error | empty | data` with skeleton loaders and a friendly empty state (`No vehicles match your filters`).
- SignalR disconnect → banner: `Live updates paused — reconnecting…`.

---

## 11. Testing

| Layer | Tests |
|---|---|
| Stores | `InventoryStore.apply*` reducer tests with vitest |
| Hooks | Custom hook tests with `@testing-library/react` |
| Containers | Mock stores + queries; assert rendered structure |
| Components | Snapshot + interaction tests for presentationals |
| SignalR | `MockHubConnection` from `@microsoft/signalr` + simulated events |
| Coverage target | ≥ 75% statements on `features/` |

---

## 12. Setup

```bash
cd frontend
npm install
cp .env.example .env
# .env
# VITE_API_BASE=https://localhost:5001
# VITE_HUB_URL=https://localhost:5001/hubs/inventory

npm run dev       # http://localhost:5173
npm run build
npm run preview
npm run test
npm run lint
```

---

**Related plans:** [`backend.md`](./backend.md) · [`database.md`](./database.md)