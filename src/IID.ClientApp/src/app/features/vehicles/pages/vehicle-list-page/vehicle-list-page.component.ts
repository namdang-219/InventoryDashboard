import { ChangeDetectionStrategy, Component, OnInit, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Vehicle, VehicleFilterParams } from '../../../../core/models/vehicle.model';
import { AuthService } from '../../../../core/services/auth.service';
import { DealershipService } from '../../../../core/services/dealership.service';
import { InventoryService } from '../../../../core/services/inventory.service';
import { RealtimeService } from '../../../../core/services/realtime.service';
import { ToastService } from '../../../../core/services/toast.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { CreateVehicleModalComponent } from '../../components/create-vehicle-modal/create-vehicle-modal.component';
import { EditVehicleModalComponent } from '../../components/edit-vehicle-modal/edit-vehicle-modal.component';
import { LogActionModalComponent } from '../../components/log-action-modal/log-action-modal.component';
import { MarkSoldModalComponent } from '../../components/mark-sold-modal/mark-sold-modal.component';
import { TransferDealershipModalComponent } from '../../components/transfer-dealership-modal/transfer-dealership-modal.component';
import { VehicleCardComponent } from '../../components/vehicle-card/vehicle-card.component';
import { VehicleDetailModalComponent } from '../../components/vehicle-detail-modal/vehicle-detail-modal.component';
import { VehicleFilterBarComponent } from '../../components/vehicle-filter-bar/vehicle-filter-bar.component';
import { VehicleTableComponent } from '../../components/vehicle-table/vehicle-table.component';

@Component({
  selector: 'iid-vehicle-list-page',
  standalone: true,
  imports: [
    VehicleFilterBarComponent,
    VehicleCardComponent,
    VehicleTableComponent,
    CreateVehicleModalComponent,
    EditVehicleModalComponent,
    MarkSoldModalComponent,
    LogActionModalComponent,
    TransferDealershipModalComponent,
    VehicleDetailModalComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './vehicle-list-page.component.html',
  styleUrl: './vehicle-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VehicleListPageComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly dealershipService = inject(DealershipService);
  private readonly realtime = inject(RealtimeService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  /**
   * Suppresses redundant realtime-triggered reloads immediately after a
   * local action (create/edit/sold/action). Without this cooldown, an edit
   * triggers 4 GETs: explicit local refresh, realtime `VehicleUpdated`,
   * realtime `InventoryChanged`, plus an optional detail fetch.
   */
  private static readonly REALTIME_SUPPRESS_MS = 1500;
  private lastLocalActionAtUtc = 0;

  private shouldIgnoreRealtimeReload(): boolean {
    return Date.now() - this.lastLocalActionAtUtc < VehicleListPageComponent.REALTIME_SUPPRESS_MS;
  }

  readonly isManager = this.auth.isManager;
  readonly canMarkSold = this.auth.canMarkSold;

  // View state
  readonly isAgingOnly = signal<boolean>(false);
  readonly viewMode = signal<'grid' | 'table'>('table');
  readonly loading = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  // Data
  readonly vehicles = signal<readonly Vehicle[]>([]);
  readonly page = signal<number>(1);
  readonly limit = signal<number>(20);
  readonly total = signal<number>(0);

  // Filters
  private currentFilters: VehicleFilterParams = {};

  // Modals
  readonly showCreateModal = signal<boolean>(false);
  readonly vehicleForEdit = signal<Vehicle | null>(null);
  readonly vehicleForSold = signal<Vehicle | null>(null);
  readonly vehicleForAction = signal<Vehicle | null>(null);
  readonly vehicleForTransfer = signal<Vehicle | null>(null);
  readonly vehicleForDetail = signal<Vehicle | null>(null);
  readonly detailModalLoading = signal<boolean>(false);

  constructor() {
    effect(() => {
      const dealerId = this.dealershipService.selectedDealershipId();
      if (dealerId !== undefined) {
        untracked(() => {
          this.page.set(1);
          this.loadVehicles(true);
        });
      }
    });

    // Realtime events handling
    this.realtime.vehicleAdded$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.shouldIgnoreRealtimeReload()) return;
        this.loadVehicles(false);
      });

    this.realtime.vehicleUpdated$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.shouldIgnoreRealtimeReload()) return;
        this.loadVehicles(false);
      });

    this.realtime.vehicleRemoved$
      .pipe(takeUntilDestroyed())
      .subscribe(id => {
        this.vehicles.update(list => list.filter(v => v.id !== id));
        this.total.update(t => Math.max(0, t - 1));
      });

    this.realtime.inventoryChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.shouldIgnoreRealtimeReload()) return;
        this.loadVehicles(false);
      });

    // Deep-link from dashboard "Take Action": /vehicles?vin={vehicleId}
    this.route.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => {
        const vin = params.get('vin');
        if (vin) {
          this.openDetailFromDeepLink(vin);
        }
      });
  }

  ngOnInit(): void {
    // Check if on aging stock tab route
    this.route.url.subscribe(segments => {
      const isAging = segments.some(s => s.path === 'aging');
      this.isAgingOnly.set(isAging);
      this.loadVehicles(true);
    });
  }

  loadVehicles(showLoading = true): void {
    if (showLoading) this.loading.set(true);
    this.error.set(null);

    if (this.isAgingOnly()) {
      // Calls GET /api/v1/vehicles/aging-stock
      const dealerId = this.dealershipService.selectedDealershipId() ?? undefined;
      this.inventoryService.getAgingStock(this.page(), this.limit(), dealerId).subscribe({
        next: res => {
          this.vehicles.set(res.data);
          this.total.set(res.meta.total);
          this.loading.set(false);
        },
        error: err => {
          this.loading.set(false);
          this.error.set(err?.error?.message || 'Failed to load aging stock.');
        }
      });
    } else {
      // Calls GET /api/v1/vehicles
      const params: VehicleFilterParams = {
        ...this.currentFilters,
        dealershipId: this.dealershipService.selectedDealershipId() ?? undefined,
        page: this.page(),
        limit: this.limit()
      };

      this.inventoryService.listVehicles(params).subscribe({
        next: res => {
          this.vehicles.set(res.data);
          this.total.set(res.meta.total);
          this.loading.set(false);
        },
        error: err => {
          this.loading.set(false);
          this.error.set(err?.error?.message || 'Failed to load vehicles from API.');
        }
      });
    }
  }

  onFilterChange(params: VehicleFilterParams): void {
    this.currentFilters = params;
    this.page.set(1);
    this.loadVehicles(true);
  }

  changePage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1) return;
    const maxPage = Math.ceil(this.total() / this.limit());
    if (maxPage > 0 && next > maxPage) return;
    this.page.set(next);
    this.loadVehicles(true);
  }

  openCreateModal(): void {
    if (!this.isManager()) {
      this.toast.warning('Permission Denied', 'Only Managers can register new vehicles.');
      return;
    }
    this.showCreateModal.set(true);
  }

  onVehicleCreated(): void {
    this.lastLocalActionAtUtc = Date.now();
    this.loadVehicles(true);
  }

  openMarkSold(v: Vehicle): void {
    if (!this.canMarkSold()) {
      this.toast.warning('Permission Denied', 'Only Salers can mark vehicles as sold.');
      return;
    }
    this.vehicleForSold.set(v);
  }

  openLogAction(v: Vehicle): void {
    if (!this.isManager()) {
      this.toast.warning('Permission Denied', 'Only Managers can log vehicle actions.');
      return;
    }
    this.vehicleForAction.set(v);
  }

  openTransferDealership(v: Vehicle): void {
    if (!this.isManager()) {
      this.toast.warning('Permission Denied', 'Only Managers can transfer vehicle showrooms.');
      return;
    }
    this.vehicleForTransfer.set(v);
  }

  openDetail(v: Vehicle): void {
    this.vehicleForDetail.set(v);
  }

  /**
   * Opens the detail modal for a vehicle identified by `vin` query param
   * (deep-link target from dashboard Action Center "Take Action" button).
   * Fetches the full vehicle record when it is not in the current page list.
   */
  openDetailFromDeepLink(vehicleId: string): void {
    // Try to use cached data first to avoid a redundant network call
    const existing = this.vehicles().find(v => v.id === vehicleId);
    if (existing) {
      this.vehicleForDetail.set(existing);
      this.clearDeepLinkParam();
      return;
    }

    this.detailModalLoading.set(true);
    this.inventoryService.getVehicleById(vehicleId).subscribe({
      next: res => {
        this.vehicleForDetail.set(res.data);
        this.detailModalLoading.set(false);
        this.clearDeepLinkParam();
      },
      error: err => {
        this.detailModalLoading.set(false);
        this.clearDeepLinkParam();
        this.toast.error(
          'Vehicle Not Found',
          err?.error?.message || `Could not locate vehicle ${vehicleId}.`
        );
      }
    });
  }

  private clearDeepLinkParam(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { vin: null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  openEditVehicle(v: Vehicle): void {
    if (!this.isManager()) {
      this.toast.warning('Permission Denied', 'Only Managers can edit vehicles.');
      return;
    }
    this.vehicleForEdit.set(v);
  }

  onVehicleUpdated(): void {
    this.lastLocalActionAtUtc = Date.now();
    this.loadVehicles(false);
    const detail = this.vehicleForDetail();
    if (detail) {
      this.inventoryService.getVehicleById(detail.id).subscribe({
        next: res => this.vehicleForDetail.set(res.data),
        error: () => this.vehicleForDetail.set(null)
      });
    }
  }

  onSoldSuccess(): void {
    this.lastLocalActionAtUtc = Date.now();
    this.loadVehicles(false);
  }

  onTransferSuccess(result: { vehicleId: string; targetDealershipId: string }): void {
    this.lastLocalActionAtUtc = Date.now();
    this.loadVehicles(false);
    const detail = this.vehicleForDetail();
    if (detail && detail.id === result.vehicleId) {
      this.inventoryService.getVehicleById(result.vehicleId).subscribe({
        next: res => this.vehicleForDetail.set(res.data),
        error: () => this.vehicleForDetail.set(null)
      });
    }
  }

  onActionSuccess(): void {
    this.lastLocalActionAtUtc = Date.now();
    this.loadVehicles(false);
  }
}
