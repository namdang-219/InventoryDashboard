import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse, RefreshTokenRequest, UserDto } from '../models/auth.model';
import { ApiConfigService } from './api.service';
import { ToastService } from './toast.service';

const TOKEN_KEY = 'iid_access_token';
const REFRESH_TOKEN_KEY = 'iid_refresh_token';
const USER_KEY = 'iid_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiConfig = inject(ApiConfigService);
  private readonly toast = inject(ToastService);

  private readonly _currentUser = signal<UserDto | null>(this.getStoredUser());
  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  readonly currentUser = this._currentUser.asReadonly();
  readonly token = this._token.asReadonly();
  readonly isAuthenticated = computed(() => !!this._token());
  readonly isManager = computed(() => {
    const user = this._currentUser();
    return user ? user.roles.some(r => r.toLowerCase() === 'manager') : false;
  });
  readonly isSaler = computed(() => {
    const user = this._currentUser();
    return user ? user.roles.some(r => r.toLowerCase() === 'saler' || r.toLowerCase() === 'sales' || r.toLowerCase() === 'viewer') : false;
  });
  readonly canMarkSold = computed(() => {
    return this.isSaler() && !this.isManager();
  });

  private getStoredUser(): UserDto | null {
    try {
      const saved = localStorage.getItem(USER_KEY);
      return saved ? (JSON.parse(saved) as UserDto) : null;
    } catch {
      return null;
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    const url = this.apiConfig.getApiUrl('/api/v1/auth/login');
    return this.http.post<LoginResponse>(url, credentials).pipe(
      tap({
        next: res => {
          this.handleAuthSuccess(res);
          this.toast.success('Welcome back', `Logged in as ${res.user.email}`);
        },
        error: () => {
          this.toast.error('Authentication Failed', 'Invalid email or password.');
        }
      })
    );
  }

  refreshToken(): Observable<LoginResponse> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY) ?? '';
    const url = this.apiConfig.getApiUrl('/api/v1/auth/refresh');
    const req: RefreshTokenRequest = { refreshToken };

    return this.http.post<LoginResponse>(url, req).pipe(
      tap({
        next: res => this.handleAuthSuccess(res),
        error: () => this.logout()
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private handleAuthSuccess(res: LoginResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this._token.set(res.token);
    this._currentUser.set(res.user);
  }
}
