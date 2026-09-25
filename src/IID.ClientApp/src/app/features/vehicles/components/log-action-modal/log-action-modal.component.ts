import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LogVehicleActionRequest, Vehicle, VehicleAction, VehicleActionType } from '../../../../core/models/vehicle.model';
import { InventoryService } from '../../../../core/services/inventory.service';
import { RealtimeService } from '../../../../core/services/realtime.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';

interface LogActionForm {
  actionType: FormControl<VehicleActionType>;
  notes: FormControl<string>;
}

@Component({
  selector: 'iid-log-action-modal',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './log-action-modal.component.html',
  styleUrl: './log-action-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogActionModalComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly realtime = inject(RealtimeService);
  private readonly toast = inject(ToastService);

  readonly vehicle = input.required<Vehicle>();
  readonly close = output<void>();
  readonly actionLogged = output<string>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly actions = signal<readonly VehicleAction[]>([]);
  readonly isLoadingActions = signal<boolean>(false);

  readonly form = new FormGroup<LogActionForm>({
    actionType: new FormControl<VehicleActionType>('PriceReductionPlanned', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    notes: new FormControl('', { nonNullable: true })
  });

  readonly actionTypes: { type: VehicleActionType; label: string; desc: string }[] = [
    { type: 'PriceReductionPlanned', label: 'Price Reduction Planned', desc: 'Flag price cut for upcoming adjustment' },
    { type: 'PriceReductionExecuted', label: 'Price Reduction Executed', desc: 'Applied discount to asking price' },
    { type: 'TransferToWholesale', label: 'Transfer to Wholesale', desc: 'Route aging unit to wholesale broker' },
    { type: 'MarketingCampaign', label: 'Marketing Push Campaign', desc: 'Boost vehicle on digital ad channels' },
    { type: 'DealerAuction', label: 'Dealer Auction Placement', desc: 'List unit on dealer wholesale network' },
    { type: 'ManagerReview', label: 'Manager Review Required', desc: 'Inspection or appraisal assessment' },
    { type: 'Relist', label: 'Relist Vehicle Online', desc: 'Refresh listing photos and description' },
    { type: 'TransferDealership', label: 'Transfer Dealership Showroom', desc: 'Relocate vehicle to another dealership showroom' },
    { type: 'TradeInCustomer', label: 'Trade-in Appraisal', desc: 'Customer trade-in appraisal log' },
    { type: 'Other', label: 'Other Operational Action', desc: 'General operational activity log' }
  ];

  constructor() {
    this.realtime.vehicleActionLogged$
      .pipe(takeUntilDestroyed())
      .subscribe(action => {
        if (action.vehicleId === this.vehicle().id) {
          this.loadActions();
        }
      });
  }

  ngOnInit(): void {
    this.loadActions();
  }

  loadActions(): void {
    this.isLoadingActions.set(true);
    this.inventoryService.getVehicleActions(this.vehicle().id).subscribe({
      next: res => {
        this.actions.set(res.data);
        this.isLoadingActions.set(false);
      },
      error: () => {
        this.isLoadingActions.set(false);
      }
    });
  }

  setNotePreset(text: string): void {
    this.form.patchValue({ notes: text });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();
    const req: LogVehicleActionRequest = {
      actionType: val.actionType,
      notes: val.notes.trim() || undefined
    };

    this.inventoryService.logVehicleAction(this.vehicle().id, req).subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.toast.success(
          'Action Recorded',
          `Logged "${val.actionType}" on ${this.vehicle().year} ${this.vehicle().make} ${this.vehicle().model}.`
        );
        this.form.patchValue({ notes: '' });
        this.actionLogged.emit(res.data.id);
        this.loadActions();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = extractErrorMessage(err, 'Failed to record action.');
        this.errorMessage.set(msg);
        this.toast.error('Action Failed', msg);
      }
    });
  }
}
