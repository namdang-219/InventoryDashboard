import { ChangeDetectionStrategy, Component, EventEmitter, OnInit, Output, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { InventoryService } from '../../core/services/inventory.service';
import { ToastService } from '../../core/services/toast.service';
import { ThemeService } from '../../core/services/theme.service';
import { DealershipService } from '../../core/services/dealership.service';
import { LiveActivityFeedItem } from '../../core/models/realtime.model';
import { Vehicle } from '../../core/models/vehicle.model';
import { VehicleDetailModalComponent } from '../../features/vehicles/components/vehicle-detail-modal/vehicle-detail-modal.component';
import { LogActionModalComponent } from '../../features/vehicles/components/log-action-modal/log-action-modal.component';
import { EditVehicleModalComponent } from '../../features/vehicles/components/edit-vehicle-modal/edit-vehicle-modal.component';
import { MarkSoldModalComponent } from '../../features/vehicles/components/mark-sold-modal/mark-sold-modal.component';

@Component({
  selector: 'iid-header',
  standalone: true,
  imports: [
    VehicleDetailModalComponent,
    LogActionModalComponent,
    EditVehicleModalComponent,
    MarkSoldModalComponent
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly realtime = inject(RealtimeService);
  readonly theme = inject(ThemeService);
  readonly dealershipService = inject(DealershipService);
  private readonly inventoryService = inject(InventoryService);
  private readonly toast = inject(ToastService);

  ngOnInit(): void {
    if (this.auth.token()) {
      this.realtime.loadInitialActivities(10);
    }
  }

  onDealershipSelect(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select && select.value) {
      this.dealershipService.selectDealership(select.value);
    }
  }

  readonly showNotifications = signal<boolean>(false);
  readonly selectedVehicleForDetail = signal<Vehicle | null>(null);
  readonly highlightedActionId = signal<string | null>(null);
  readonly selectedVehicleForAction = signal<Vehicle | null>(null);
  readonly selectedVehicleForEdit = signal<Vehicle | null>(null);
  readonly selectedVehicleForSold = signal<Vehicle | null>(null);
  readonly isLoadingVehicle = signal<boolean>(false);

  readonly user = this.auth.currentUser;
  readonly isManager = this.auth.isManager;
  readonly connectionStatus = this.realtime.connectionStatus;
  readonly activities = this.realtime.recentActivities;
  readonly hasMoreActivities = this.realtime.hasMoreActivities;
  readonly isLoadingMoreActivities = this.realtime.isLoadingMoreActivities;
  readonly totalActivities = this.realtime.totalActivities;

  readonly unreadCount = computed(() => {
    return Math.max(this.realtime.unreadCount(), this.activities().filter(a => !a.isRead).length);
  });

  readonly unreadBadgeText = computed(() => {
    const count = this.unreadCount();
    return count >= 10 ? '10+' : count.toString();
  });

  toggleNotifications(): void {
    const opening = !this.showNotifications();
    this.showNotifications.set(opening);
    if (opening && this.activities().length === 0) {
      this.realtime.loadInitialActivities(10);
    }
  }

  onDropdownScroll(event: Event): void {
    const el = event.target as HTMLElement;
    if (!el) return;
    const threshold = 40;
    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight <= threshold;
    if (atBottom && this.hasMoreActivities() && !this.isLoadingMoreActivities()) {
      this.realtime.loadMoreActivities(10);
    }
  }

  markAllAsRead(): void {
    this.realtime.markAllAsRead();
  }

  onActivityClick(act: LiveActivityFeedItem): void {
    // Mark as read immediately on click
    if (!act.isRead) {
      this.realtime.markAsRead(act.id);
    }

    if (act.vehicleId && act.type !== 'removed') {
      this.showNotifications.set(false);
      this.isLoadingVehicle.set(true);
      this.inventoryService.getVehicleById(act.vehicleId).subscribe({
        next: res => {
          this.isLoadingVehicle.set(false);
          if (act.type === 'action') {
            this.highlightedActionId.set(act.id);
          } else {
            this.highlightedActionId.set(null);
          }
          this.selectedVehicleForDetail.set(res.data);
        },
        error: () => {
          this.isLoadingVehicle.set(false);
          this.toast.error('Vehicle Not Found', 'Could not load vehicle details.');
        }
      });
    }
  }

  closeDetailModal(): void {
    this.selectedVehicleForDetail.set(null);
    this.highlightedActionId.set(null);
  }

  onEditFromDetail(v: Vehicle): void {
    this.closeDetailModal();
    this.selectedVehicleForEdit.set(v);
  }

  onLogActionFromDetail(v: Vehicle): void {
    this.closeDetailModal();
    this.selectedVehicleForAction.set(v);
  }

  onMarkSoldFromDetail(v: Vehicle): void {
    this.closeDetailModal();
    this.selectedVehicleForSold.set(v);
  }

  closeActionModal(): void {
    this.selectedVehicleForAction.set(null);
  }

  closeEditModal(): void {
    this.selectedVehicleForEdit.set(null);
  }

  closeSoldModal(): void {
    this.selectedVehicleForSold.set(null);
  }

  logout(): void {
    this.realtime.stopConnection();
    this.auth.logout();
  }
}
