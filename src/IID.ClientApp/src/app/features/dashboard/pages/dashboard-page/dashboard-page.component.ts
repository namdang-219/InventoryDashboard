import { ChangeDetectionStrategy, Component, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { ActionCenterItem, DashboardBundle, LowInventoryAlert } from '../../../../core/models/dashboard.model';
import { DashboardService } from '../../../../core/services/dashboard.service';
import { DealershipService } from '../../../../core/services/dealership.service';
import { RealtimeService } from '../../../../core/services/realtime.service';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge.component';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';
import { ActionCenterSectionComponent } from '../../components/action-center-section/action-center-section.component';
import { InventoryChartsComponent } from '../../components/inventory-charts/inventory-charts.component';
import { LowInventoryAlertsComponent } from '../../components/low-inventory-alerts/low-inventory-alerts.component';
import { QuickStatsSectionComponent } from '../../components/quick-stats-section/quick-stats-section.component';

@Component({
  selector: 'iid-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
    QuickStatsSectionComponent,
    InventoryChartsComponent,
    ActionCenterSectionComponent,
    LowInventoryAlertsComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
    DemandBadgeComponent,
    AgingSeverityBadgeComponent,
    FormatCurrencyPipe
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly dealershipService = inject(DealershipService);
  private readonly realtime = inject(RealtimeService);
  private readonly router = inject(Router);

  readonly bundle = signal<DashboardBundle | null>(null);
  readonly alerts = signal<readonly LowInventoryAlert[]>([]);
  readonly loading = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      // Re-trigger load when active dealership changes
      const dealerId = this.dealershipService.selectedDealershipId();
      if (dealerId !== undefined) {
        this.loadDashboard(true);
        this.loadAlerts();
      }
    });

    this.realtime.inventoryChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.loadDashboard(false);
        this.loadAlerts();
      });

    this.realtime.summaryUpdated$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.loadDashboard(false);
        this.loadAlerts();
      });

    this.realtime.alertsUpdated$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        // Aging-alerts feed (top of dashboard). Re-fetch the full dashboard
        // bundle so summary, charts and inventory stay in sync with the
        // server-side computed alert set.
        this.loadDashboard(false);
        this.loadAlerts();
      });
  }

  ngOnInit(): void {
    // Loaded via effect on init
  }

  loadDashboard(showLoading = true): void {
    if (showLoading) {
      this.loading.set(true);
    }
    this.error.set(null);

    const dealershipId = this.dealershipService.selectedDealershipId();
    this.dashboardService.getDashboardBundle(1, 10, dealershipId).subscribe({
      next: res => {
        this.bundle.set(res.data);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Unable to connect to IID API server.');
      }
    });
  }

  loadAlerts(): void {
    this.dashboardService.getLowInventoryAlerts().subscribe({
      next: res => {
        this.alerts.set(res.data);
      }
    });
  }

  handleActionItem(item: ActionCenterItem): void {
    if (item.vehicleId) {
      this.router.navigate(['/vehicles'], { queryParams: { vin: item.vehicleId } });
    } else {
      this.router.navigate(['/vehicles']);
    }
  }
}
