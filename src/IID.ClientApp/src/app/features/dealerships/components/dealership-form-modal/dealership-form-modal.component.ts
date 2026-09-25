import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateDealershipRequest, Dealership, UpdateDealershipRequest } from '../../../../core/models/dealership.model';
import { DealershipService } from '../../../../core/services/dealership.service';
import { ToastService } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';

interface DealershipForm {
  name: FormControl<string>;
  code: FormControl<string>;
  city: FormControl<string>;
  state: FormControl<string>;
  phone: FormControl<string>;
}

@Component({
  selector: 'iid-dealership-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './dealership-form-modal.component.html',
  styleUrl: './dealership-form-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DealershipFormModalComponent implements OnInit {
  private readonly dealershipService = inject(DealershipService);
  private readonly toast = inject(ToastService);

  readonly dealership = input<Dealership | null>(null);
  readonly close = output<void>();
  readonly saved = output<void>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<DealershipForm>({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)]
    }),
    code: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(20)]
    }),
    city: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)]
    }),
    state: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)]
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(50)]
    })
  });

  get isEditMode(): boolean {
    return !!this.dealership();
  }

  ngOnInit(): void {
    const d = this.dealership();
    if (d) {
      this.form.patchValue({
        name: d.name,
        code: d.code,
        city: d.city,
        state: d.state,
        phone: d.phone ?? ''
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
    const current = this.dealership();

    if (current) {
      // Edit mode
      const req: UpdateDealershipRequest = {
        name: val.name.trim(),
        code: val.code.trim().toUpperCase(),
        city: val.city.trim(),
        state: val.state.trim().toUpperCase(),
        phone: val.phone.trim()
      };

      this.dealershipService.updateDealership(current.id, req).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.toast.success(
            'Dealership Updated',
            `Showroom "${req.name}" details have been updated successfully.`
          );
          this.saved.emit();
          this.close.emit();
        },
        error: err => {
          this.isSubmitting.set(false);
          const msg = extractErrorMessage(err, 'Failed to update dealership.');
          this.errorMessage.set(msg);
          this.toast.error('Update Failed', msg);
        }
      });
    } else {
      // Create mode
      const req: CreateDealershipRequest = {
        name: val.name.trim(),
        code: val.code.trim().toUpperCase(),
        city: val.city.trim(),
        state: val.state.trim().toUpperCase(),
        phone: val.phone.trim()
      };

      this.dealershipService.createDealership(req).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.toast.success(
            'Dealership Registered',
            `Showroom "${req.name}" (${req.code}) has been created.`
          );
          this.saved.emit();
          this.close.emit();
        },
        error: err => {
          this.isSubmitting.set(false);
          const msg = extractErrorMessage(err, 'Failed to create dealership.');
          this.errorMessage.set(msg);
          this.toast.error('Registration Failed', msg);
        }
      });
    }
  }
}
