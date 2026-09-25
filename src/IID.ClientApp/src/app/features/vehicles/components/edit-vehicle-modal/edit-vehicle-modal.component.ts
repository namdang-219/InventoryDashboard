import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FuelType, UpdateVehicleRequest, Vehicle, VehicleStatus } from '../../../../core/models/vehicle.model';
import { InventoryService } from '../../../../core/services/inventory.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';

interface EditVehicleForm {
  make: FormControl<string>;
  model: FormControl<string>;
  year: FormControl<number>;
  color: FormControl<string>;
  mileage: FormControl<number>;
  fuelType: FormControl<FuelType>;
  purchasePrice: FormControl<number>;
  askingPrice: FormControl<number>;
  status: FormControl<VehicleStatus>;
}

@Component({
  selector: 'iid-edit-vehicle-modal',
  standalone: true,
  imports: [ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './edit-vehicle-modal.component.html',
  styleUrl: './edit-vehicle-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditVehicleModalComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly toast = inject(ToastService);

  readonly vehicle = input.required<Vehicle>();
  readonly close = output<void>();
  readonly updated = output<void>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<EditVehicleForm>({
    make: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
    model: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
    year: new FormControl(new Date().getFullYear(), {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1980), Validators.max(new Date().getFullYear() + 1)]
    }),
    color: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(30)] }),
    mileage: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    fuelType: new FormControl<FuelType>('Petrol', { nonNullable: true, validators: [Validators.required] }),
    purchasePrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    askingPrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    status: new FormControl<VehicleStatus>('Available', { nonNullable: true, validators: [Validators.required] })
  });

  ngOnInit(): void {
    const v = this.vehicle();
    this.form.patchValue({
      make: v.make,
      model: v.model,
      year: v.year,
      color: v.color,
      mileage: v.mileage,
      fuelType: v.fuelType,
      purchasePrice: v.purchasePriceAmount,
      askingPrice: v.askingPriceAmount,
      status: v.status === 'Sold' ? 'Available' : v.status
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
    const req: UpdateVehicleRequest = {
      make: val.make.trim(),
      model: val.model.trim(),
      year: val.year,
      color: val.color.trim(),
      mileage: val.mileage,
      fuelType: val.fuelType,
      purchasePrice: val.purchasePrice,
      askingPrice: val.askingPrice,
      status: val.status,
      rowVersion: this.vehicle().rowVersion
    };

    this.inventoryService.updateVehicle(this.vehicle().id, req).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.toast.success(
          'Vehicle Updated',
          `${req.year} ${req.make} ${req.model} details updated successfully.`
        );
        this.updated.emit();
        this.close.emit();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = extractErrorMessage(err, 'Failed to update vehicle.');
        this.errorMessage.set(msg);
        this.toast.error('Failed to Update Vehicle', msg);
      }
    });
  }
}
