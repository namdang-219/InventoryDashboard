import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MarkVehicleSoldRequest, Vehicle } from '../../../../core/models/vehicle.model';
import { InventoryService } from '../../../../core/services/inventory.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

interface MarkSoldForm {
  soldPrice: FormControl<number>;
  soldPriceCurrency: FormControl<string>;
  soldAtDate: FormControl<string>;
}

@Component({
  selector: 'iid-mark-sold-modal',
  standalone: true,
  imports: [ReactiveFormsModule, FormatCurrencyPipe],
  templateUrl: './mark-sold-modal.component.html',
  styleUrl: './mark-sold-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MarkSoldModalComponent {
  private readonly inventoryService = inject(InventoryService);
  private readonly toast = inject(ToastService);

  readonly vehicle = input.required<Vehicle>();
  readonly close = output<void>();
  readonly soldSuccess = output<string>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<MarkSoldForm>({
    soldPrice: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    soldPriceCurrency: new FormControl('USD', { nonNullable: true }),
    soldAtDate: new FormControl(new Date().toISOString().substring(0, 10), {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  readonly estimatedProfit = computed(() => {
    const sold = this.form.controls.soldPrice.value;
    const purchase = this.vehicle().purchasePriceAmount;
    return (sold || 0) - purchase;
  });

  readonly estimatedMargin = computed(() => {
    const sold = this.form.controls.soldPrice.value;
    const purchase = this.vehicle().purchasePriceAmount;
    if (!purchase || !sold) return 0;
    return Math.round(((sold - purchase) / purchase) * 100);
  });

  ngOnInit(): void {
    // Default sold price to asking price
    this.form.patchValue({
      soldPrice: this.vehicle().askingPriceAmount
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();
    const req: MarkVehicleSoldRequest = {
      soldPrice: val.soldPrice,
      soldPriceCurrency: val.soldPriceCurrency,
      soldAtUtc: new Date(val.soldAtDate).toISOString(),
      rowVersion: this.vehicle().rowVersion
    };

    this.inventoryService.markVehicleSold(this.vehicle().id, req).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.toast.success(
          'Vehicle Sold!',
          `${this.vehicle().year} ${this.vehicle().make} ${this.vehicle().model} marked as Sold.`
        );
        this.soldSuccess.emit(this.vehicle().id);
        this.close.emit();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = err?.status === 409
          ? 'Concurrency Conflict: This vehicle was modified by another transaction. Please reload the vehicle to obtain the latest revision.'
          : extractErrorMessage(err, 'Failed to mark vehicle as sold.');
        this.errorMessage.set(msg);
        this.toast.error('Sale Failed', msg);
      }
    });
  }
}
