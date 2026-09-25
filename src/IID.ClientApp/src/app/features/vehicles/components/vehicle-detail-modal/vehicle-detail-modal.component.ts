import { ChangeDetectionStrategy, Component, OnInit, effect, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Vehicle, VehicleAction } from '../../../../core/models/vehicle.model';
import { InventoryService } from '../../../../core/services/inventory.service';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge.component';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

@Component({
  selector: 'iid-vehicle-detail-modal',
  standalone: true,
  imports: [
    DatePipe,
    StatusBadgeComponent,
    AgingSeverityBadgeComponent,
    DemandBadgeComponent,
    FormatCurrencyPipe
  ],
  templateUrl: './vehicle-detail-modal.component.html',
  styleUrl: './vehicle-detail-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VehicleDetailModalComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);

  readonly vehicle = input.required<Vehicle>();
  readonly isManager = input<boolean>(false);
  readonly highlightActionId = input<string | null>(null);

  readonly close = output<void>();
  readonly editVehicle = output<Vehicle>();
  readonly markSold = output<Vehicle>();
  readonly logAction = output<Vehicle>();
  readonly transferDealership = output<Vehicle>();

  readonly actions = signal<readonly VehicleAction[]>([]);
  readonly isLoadingActions = signal<boolean>(false);

  constructor() {
    effect(() => {
      const targetId = this.highlightActionId();
      const list = this.actions();
      if (targetId && list.length > 0) {
        this.scrollToHighlightedAction();
      }
    });
  }

  ngOnInit(): void {
    this.isLoadingActions.set(true);
    this.inventoryService.getVehicleActions(this.vehicle().id).subscribe({
      next: res => {
        this.actions.set(res.data);
        this.isLoadingActions.set(false);
      },
      error: () => this.isLoadingActions.set(false)
    });
  }

  isActionHighlighted(actionId: string): boolean {
    const target = this.highlightActionId();
    if (!target) return false;
    return actionId.toLowerCase() === target.toLowerCase();
  }

  private scrollToHighlightedAction(): void {
    const targetId = this.highlightActionId();
    if (!targetId) return;

    setTimeout(() => {
      const el = document.getElementById(`action-card-${targetId.toLowerCase()}`);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.focus({ preventScroll: true });
      }
    }, 150);
  }
}
