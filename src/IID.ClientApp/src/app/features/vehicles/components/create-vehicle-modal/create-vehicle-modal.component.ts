import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateVehicleRequest, FuelType, VehicleStatus } from '../../../../core/models/vehicle.model';
import { DealershipService } from '../../../../core/services/dealership.service';
import { InventoryService } from '../../../../core/services/inventory.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';

interface CreateVehicleForm {
  dealershipId: FormControl<string>;
  vin: FormControl<string>;
  stockNumber: FormControl<string>;
  make: FormControl<string>;
  model: FormControl<string>;
  year: FormControl<number>;
  color: FormControl<string>;
  mileage: FormControl<number>;
  fuelType: FormControl<FuelType>;
  purchasePrice: FormControl<number>;
  askingPrice: FormControl<number>;
  status: FormControl<VehicleStatus>;
  dateAddedToInventory: FormControl<string>;
}

@Component({
  selector: 'iid-create-vehicle-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './create-vehicle-modal.component.html',
  styleUrl: './create-vehicle-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateVehicleModalComponent {
  private readonly inventoryService = inject(InventoryService);
  readonly dealershipService = inject(DealershipService);
  private readonly toast = inject(ToastService);

  readonly close = output<void>();
  readonly created = output<string>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<CreateVehicleForm>({
    dealershipId: new FormControl(this.dealershipService.selectedDealershipId() ?? '', {
      nonNullable: true
    }),
    vin: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(11), Validators.maxLength(17)]
    }),
    stockNumber: new FormControl('', { nonNullable: true }),
    make: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    model: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    year: new FormControl(new Date().getFullYear(), {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1980), Validators.max(new Date().getFullYear() + 1)]
    }),
    color: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    mileage: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    fuelType: new FormControl<FuelType>('Petrol', { nonNullable: true, validators: [Validators.required] }),
    purchasePrice: new FormControl(25000, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    askingPrice: new FormControl(31000, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    status: new FormControl<VehicleStatus>('Available', { nonNullable: true, validators: [Validators.required] }),
    dateAddedToInventory: new FormControl(new Date().toISOString().substring(0, 10), {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  fillSample(preset: 'tesla' | 'bmw' | 'porsche'): void {
    if (preset === 'tesla') {
      this.form.patchValue({
        vin: '5YJ3E1EB' + Math.floor(100000000 + Math.random() * 900000000),
        make: 'Tesla',
        model: 'Model 3 Performance',
        year: 2024,
        color: 'Deep Blue Metallic',
        mileage: 4500,
        fuelType: 'Electric',
        purchasePrice: 42000,
        askingPrice: 49500,
        status: 'Available'
      });
    } else if (preset === 'bmw') {
      this.form.patchValue({
        vin: 'WBA53AY0' + Math.floor(100000000 + Math.random() * 900000000),
        make: 'BMW',
        model: 'X5 xDrive45e',
        year: 2023,
        color: 'Mineral White',
        mileage: 18200,
        fuelType: 'PluginHybrid',
        purchasePrice: 51000,
        askingPrice: 58900,
        status: 'Available'
      });
    } else {
      this.form.patchValue({
        vin: 'WP0AB2A9' + Math.floor(100000000 + Math.random() * 900000000),
        make: 'Porsche',
        model: 'Taycan 4S',
        year: 2023,
        color: 'Volcano Grey',
        mileage: 8900,
        fuelType: 'Electric',
        purchasePrice: 85000,
        askingPrice: 97500,
        status: 'Available'
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();
    const dealerId = this.dealershipService.selectedDealershipId() || val.dealershipId || undefined;
    const req: CreateVehicleRequest = {
      dealershipId: dealerId,
      vin: val.vin.trim().toUpperCase(),
      stockNumber: val.stockNumber.trim() || undefined,
      make: val.make.trim(),
      model: val.model.trim(),
      year: val.year,
      color: val.color.trim(),
      mileage: val.mileage,
      fuelType: val.fuelType,
      purchasePrice: val.purchasePrice,
      askingPrice: val.askingPrice,
      status: val.status,
      dateAddedToInventory: new Date(val.dateAddedToInventory).toISOString()
    };

    this.inventoryService.createVehicle(req).subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.toast.success('Vehicle Created', `${req.year} ${req.make} ${req.model} added to inventory.`);
        this.created.emit(res.id);
        this.close.emit();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = extractErrorMessage(err, 'Failed to create vehicle.');
        this.errorMessage.set(msg);
        this.toast.error('Failed to Create Vehicle', msg);
      }
    });
  }
}
