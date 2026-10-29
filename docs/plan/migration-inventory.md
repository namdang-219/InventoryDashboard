# Angular 19 → React Migration Inventory (`IID.ClientApp` → `IID.FrontendApp`)

This document inventories every component, service, guard, interceptor, and pipe in `src/IID.ClientApp` along with its exact React mapping in `src/IID.FrontendApp`.

## 1. Core Layer

| Angular Source (`IID.ClientApp`) | React Target (`IID.FrontendApp`) | Role & State Mapping |
|---|---|---|
| `core/models/*.model.ts` (6 files) | `core/models/*.model.ts` | Verbatim copy (pure TypeScript types/interfaces) |
| `core/utils/error-extractor.ts` | `core/utils/error-extractor.ts` | Verbatim copy; works with normalized `HttpError` |
| `core/services/api.service.ts` | `core/services/api.service.ts` | `apiConfig` + Axios `http` instance + `HttpError` normalizer |
| `core/interceptors/auth.interceptor.ts` | `core/interceptors/auth.interceptor.ts` | `setupAuthInterceptor()` — Bearer header + single-flight 401 token refresh queue |
| `core/guards/auth.guard.ts` | `core/guards/auth.guard.tsx` | `<RequireAuth>` and `<RequireManager>` route wrappers |
| `core/services/auth.service.ts` | `core/services/auth.service.ts` + `core/store/auth.slice.ts` | Redux slice + `createAsyncThunk` (`loginThunk`, `refreshTokenThunk`) + `useAuth()` |
| `core/services/toast.service.ts` | `core/services/toast.service.ts` + `core/store/toast.slice.ts` | Redux slice + `toastService` + `useToast()` |
| `core/services/theme.service.ts` | `core/services/theme.service.ts` + `core/store/theme.slice.ts` | Redux slice + `themeService` + `useTheme()` (`data-theme` attribute sync) |
| `core/services/dealership.service.ts` | `core/services/dealership.service.ts` + `core/store/dealership.slice.ts` | Redux slice + SignalR listeners + `useDealerships()` |
| `core/services/realtime.service.ts` | `core/services/realtime.service.ts` + `core/store/realtime.slice.ts` | SignalR singleton + Redux slice + `realtimeEvents` callback registry + `useRealtime()` / `useRealtimeEvent()` |
| `core/services/dashboard.service.ts` | `core/services/dashboard.service.ts` | Stateless Promise API wrapper (`dashboardService`) |
| `core/services/inventory.service.ts` | `core/services/inventory.service.ts` | Stateless Promise API wrapper (`inventoryService`) |

## 2. Shared Pipes & Components

| Angular Component / Pipe | Selector | React File | Props / Signature |
|---|---|---|---|
| `DaysAgoPipe` | `daysAgo` | `shared/pipes/days-ago.pipe.ts` | `daysAgo(value: string \| Date \| null \| undefined): string` |
| `FormatCurrencyPipe` | `formatCurrency` | `shared/pipes/format-currency.pipe.ts` | `formatCurrency(value: number \| null \| undefined, currency?: string): string` |
| `AgingSeverityBadgeComponent` | `iid-aging-severity-badge` | `shared/components/aging-severity-badge/aging-severity-badge.tsx` | `{ severity?: AgingSeverity \| string; days?: number \| null }` |
| `DemandBadgeComponent` | `iid-demand-badge` | `shared/components/demand-badge/demand-badge.tsx` | `{ level?: DemandLevel \| string; score?: number \| null }` |
| `EmptyStateComponent` | `iid-empty-state` | `shared/components/empty-state/empty-state.tsx` | `{ icon?: string; title?: string; description?: string; actionLabel?: string \| null; onActionClick?: () => void }` |
| `LoadingSpinnerComponent` | `iid-loading-spinner` | `shared/components/loading-spinner/loading-spinner.tsx` | `{ message?: string; size?: 'sm' \| 'md' \| 'lg' }` |
| `StatCardComponent` | `iid-stat-card` | `shared/components/stat-card/stat-card.tsx` | `{ title: string; value: string \| number; icon?: string; accent?: 'primary' \| 'success' \| 'warning' \| 'danger' \| 'purple'; trend?: 'up' \| 'down' \| 'flat' \| null; trendLabel?: string \| null; subtitle?: string \| null }` |
| `StatusBadgeComponent` | `iid-status-badge` | `shared/components/status-badge/status-badge.tsx` | `{ status?: VehicleStatus \| string }` |
| `ToastContainerComponent` | `iid-toast-container` | `shared/components/toast-container/toast-container.tsx` | `{}` (reads `useToast()`) |

## 3. Layout Components

| Angular Component | Selector | React File | Props & Services |
|---|---|---|---|
| `ShellComponent` | `iid-shell` | `layout/shell/shell.tsx` | `{}` — starts `realtimeService.startConnection()`, renders `SidebarComponent`, `HeaderComponent`, `<Outlet />` |
| `SidebarComponent` | `iid-sidebar` | `layout/sidebar/sidebar.tsx` | `{ collapsed?: boolean; onToggleCollapse?: () => void }` — uses `useAuth()`, `NavLink` |
| `HeaderComponent` | `iid-header` | `layout/header/header.tsx` | `{}` — uses `useAuth`, `useRealtime`, `useTheme`, `useDealerships`, `inventoryService`, `toastService`, vehicle modals |

## 4. Feature Components & Pages

| Feature | Component | Selector | React File | Props (Inputs + `onXxx` Outputs) |
|---|---|---|---|---|
| Auth | `LoginPageComponent` | `iid-login-page` | `features/auth/pages/login-page/login-page.tsx` | `{}` — controlled form, demo quick-fill, `useAuth`, `useDealerships`, `useNavigate` |
| Dashboard | `QuickStatsSectionComponent` | `iid-quick-stats-section` | `features/dashboard/components/quick-stats-section/quick-stats-section.tsx` | `{ summary?: DashboardSummary \| null; quickStats?: QuickStats \| null; isManager?: boolean }` |
| Dashboard | `ActionCenterSectionComponent` | `iid-action-center-section` | `features/dashboard/components/action-center-section/action-center-section.tsx` | `{ items?: readonly ActionCenterItem[]; onActionClick?: (item: ActionCenterItem) => void }` |
| Dashboard | `InventoryChartsComponent` | `iid-inventory-charts` | `features/dashboard/components/inventory-charts/inventory-charts.tsx` | `{ charts?: InventoryCharts \| null }` |
| Dashboard | `LowInventoryAlertsComponent` | `iid-low-inventory-alerts` | `features/dashboard/components/low-inventory-alerts/low-inventory-alerts.tsx` | `{ alerts?: readonly LowInventoryAlert[] }` |
| Dashboard | `SellingVehiclesTrendComponent` | `iid-selling-vehicles-trend` | `features/dashboard/components/selling-vehicles-trend/selling-vehicles-trend.tsx` | `{ monthlySales?: readonly MonthlySales[] }` |
| Dashboard | `DashboardPageComponent` | `iid-dashboard-page` | `features/dashboard/pages/dashboard-page/dashboard-page.tsx` | `{}` — fetches bundle & alerts, subscribes to SignalR events |
| Vehicles | `CreateVehicleModalComponent` | `iid-create-vehicle-modal` | `features/vehicles/components/create-vehicle-modal/create-vehicle-modal.tsx` | `{ onClose: () => void; onCreated?: (id: string) => void }` |
| Vehicles | `EditVehicleModalComponent` | `iid-edit-vehicle-modal` | `features/vehicles/components/edit-vehicle-modal/edit-vehicle-modal.tsx` | `{ vehicle: Vehicle; onClose: () => void; onUpdated?: () => void }` |
| Vehicles | `LogActionModalComponent` | `iid-log-action-modal` | `features/vehicles/components/log-action-modal/log-action-modal.tsx` | `{ vehicle: Vehicle; onClose: () => void; onActionLogged?: (id: string) => void }` |
| Vehicles | `MarkSoldModalComponent` | `iid-mark-sold-modal` | `features/vehicles/components/mark-sold-modal/mark-sold-modal.tsx` | `{ vehicle: Vehicle; isManager?: boolean; onClose: () => void; onSoldSuccess?: (id: string) => void }` |
| Vehicles | `TransferDealershipModalComponent` | `iid-transfer-dealership-modal` | `features/vehicles/components/transfer-dealership-modal/transfer-dealership-modal.tsx` | `{ vehicle: Vehicle; onClose: () => void; onTransferred?: (e: { vehicleId: string; targetDealershipId: string }) => void }` |
| Vehicles | `VehicleCardComponent` | `iid-vehicle-card` | `features/vehicles/components/vehicle-card/vehicle-card.tsx` | `{ vehicle: Vehicle; isManager?: boolean; canMarkSold?: boolean; onViewDetails?: (v: Vehicle) => void; onEditVehicle?: (v: Vehicle) => void; onMarkSold?: (v: Vehicle) => void; onLogAction?: (v: Vehicle) => void; onTransferDealership?: (v: Vehicle) => void }` |
| Vehicles | `VehicleDetailModalComponent` | `iid-vehicle-detail-modal` | `features/vehicles/components/vehicle-detail-modal/vehicle-detail-modal.tsx` | `{ vehicle: Vehicle; isManager?: boolean; canMarkSold?: boolean; highlightActionId?: string \| null; onClose: () => void; onEditVehicle?: (v: Vehicle) => void; onMarkSold?: (v: Vehicle) => void; onLogAction?: (v: Vehicle) => void; onTransferDealership?: (v: Vehicle) => void }` |
| Vehicles | `VehicleFilterBarComponent` | `iid-vehicle-filter-bar` | `features/vehicles/components/vehicle-filter-bar/vehicle-filter-bar.tsx` | `{ onFilterChange: (params: VehicleFilterParams) => void }` |
| Vehicles | `VehicleTableComponent` | `iid-vehicle-table` | `features/vehicles/components/vehicle-table/vehicle-table.tsx` | `{ vehicles: readonly Vehicle[]; isManager?: boolean; canMarkSold?: boolean; onViewDetails?: (v: Vehicle) => void; onEditVehicle?: (v: Vehicle) => void; onMarkSold?: (v: Vehicle) => void; onLogAction?: (v: Vehicle) => void; onTransferDealership?: (v: Vehicle) => void }` |
| Vehicles | `VehicleListPageComponent` | `iid-vehicle-list-page` | `features/vehicles/pages/vehicle-list-page/vehicle-list-page.tsx` | `{}` — serves `/vehicles` and `/vehicles/aging` |
| Dealerships | `DealershipFormModalComponent` | `iid-dealership-form-modal` | `features/dealerships/components/dealership-form-modal/dealership-form-modal.tsx` | `{ dealership?: Dealership \| null; onClose: () => void; onSaved?: () => void }` |
| Dealerships | `DealershipListPageComponent` | `iid-dealership-list-page` | `features/dealerships/pages/dealership-list-page/dealership-list-page.tsx` | `{}` — manager-guarded dealership directory |

