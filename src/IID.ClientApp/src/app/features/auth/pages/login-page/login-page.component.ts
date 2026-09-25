import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

import { DealershipService } from '../../../../core/services/dealership.service';

interface LoginForm {
  email: FormControl<string>;
  password: FormControl<string>;
}

@Component({
  selector: 'iid-login-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly dealershipService = inject(DealershipService);

  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup<LoginForm>({
    email: new FormControl('admin@iid.local', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('P@ssw0rd!Admin', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(4)]
    })
  });

  fillDemo(role: 'admin' | 'saler' | 'viewer'): void {
    if (role === 'admin') {
      this.form.patchValue({
        email: 'admin@iid.local',
        password: 'P@ssw0rd!Admin'
      });
      this.toast.info('Quick Fill', 'Loaded Manager account credentials.');
    } else {
      this.form.patchValue({
        email: 'saler@iid.local',
        password: 'P@ssw0rd!Saler'
      });
      this.toast.info('Quick Fill', 'Loaded Saler account credentials.');
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();

    this.auth.login({ email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.dealershipService.loadDealerships();
        this.router.navigate(['/dashboard']);
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err?.error?.message || 'Invalid credentials or API server unreachable. Please verify API is running.'
        );
      }
    });
  }
}
