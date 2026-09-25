import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dealership } from '../../../../core/models/dealership.model';
import { TransferVehicleRequest, Vehicle } from '../../../../core/models/vehicle.model';
import { DealershipService } from '../../../../core/services/dealership.service';
import { InventoryService } from '../../../../core/services/inventory.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

interface TransferDealershipForm {
  targetDealershipId: FormControl<string>;
  notes: FormControl<string>;
}

@Component({
  selector: 'iid-transfer-dealership-modal',
  standalone: true,
  imports: [ReactiveFormsModule, FormatCurrencyPipe],
  templateUrl: './transfer-dealership-modal.component.html',
  styleUrl: './transfer-dealership-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransferDealershipModalComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  readonly dealershipService = inject(DealershipService);
  private readonly toast = inject(ToastService);

  readonly vehicle = input.required<Vehicle>();
  readonly close = output<void>();
  readonly transferred = output<{ vehicleId: string; targetDealershipId: string }>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<TransferDealershipForm>({
    targetDealershipId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    notes: new FormControl('', { nonNullable: true })
  });

  // Filter out the dealership where the vehicle currently resides
  readonly availableDealerships = computed(() => {
    const currentId = this.vehicle().dealershipId;
    return this.dealershipService.dealerships().filter(d => d.id !== currentId);
  });

  readonly currentDealershipName = computed(() => {
    if (this.vehicle().dealershipName) {
      return this.vehicle().dealershipName!;
    }
    const currentId = this.vehicle().dealershipId;
    const match = this.dealershipService.dealerships().find(d => d.id === currentId);
    return match ? `${match.name} (${match.city}, ${match.state})` : 'Current Showroom';
  });

  readonly selectedTargetDealership = computed<Dealership | null>(() => {
    const targetId = this.form.controls.targetDealershipId.value;
    if (!targetId) return null;
    return this.dealershipService.dealerships().find(d => d.id === targetId) ?? null;
  });

  ngOnInit(): void {
    const available = this.availableDealerships();
    if (available.length > 0) {
      this.form.patchValue({
        targetDealershipId: available[0].id
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const targetId = this.form.controls.targetDealershipId.value;
    if (!targetId || targetId === this.vehicle().dealershipId) {
      this.errorMessage.set('Please select a valid destination dealership different from current showroom.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const req: TransferVehicleRequest = {
      targetDealershipId: targetId,
      notes: this.form.controls.notes.value.trim() || undefined
    };

    const targetDealerName = this.selectedTargetDealership()?.name ?? 'target dealership';

    this.inventoryService.transferDealership(this.vehicle().id, req).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.toast.success(
          'Vehicle Transferred!',
          `${this.vehicle().year} ${this.vehicle().make} ${this.vehicle().model} transferred to ${targetDealerName}.`
        );
        this.transferred.emit({
          vehicleId: this.vehicle().id,
          targetDealershipId: targetId
        });
        this.close.emit();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = extractErrorMessage(err, 'Failed to transfer vehicle dealership.');
        this.errorMessage.set(msg);
        this.toast.error('Transfer Failed', msg);
      }
    });
  }
}
