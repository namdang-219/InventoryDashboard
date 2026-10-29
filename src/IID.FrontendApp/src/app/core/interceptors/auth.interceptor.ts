import { InternalAxiosRequestConfig } from 'axios';
import { HttpError, http } from '../services/api.service';
import { authService } from '../services/auth.service';

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _isRetry?: boolean;
}

let isInstalled = false;
let isRefreshing = false;
let refreshWaiters: { resolve: (token: string) => void; reject: (err: unknown) => void }[] = [];

function isAuthEndpoint(url: string | undefined): boolean {
  return !!url && (url.includes('/api/v1/auth/login') || url.includes('/api/v1/auth/refresh'));
}

/**
 * Registers the Bearer-token request interceptor and the 401 → refresh → retry
 * response interceptor (single-flight refresh; concurrent 401s wait for the new token).
 * Idempotent if called more than once.
 */
export function setupAuthInterceptor(): void {
  if (isInstalled) return;
  isInstalled = true;

  http.interceptors.request.use((config: RetryableRequestConfig) => {
    if (config._isRetry) {
      return config;
    }
    const token = authService.token();
    // Skip auth header for login or refresh
    if (token && !isAuthEndpoint(config.url)) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
  });

  http.interceptors.response.use(
    response => response,
    async (error: unknown) => {
      if (!(error instanceof HttpError) || error.status !== 401 || isAuthEndpoint(error.url) || !error.config) {
        return Promise.reject(error);
      }

      const original = error.config as RetryableRequestConfig;
      if (original._isRetry) {
        return Promise.reject(error);
      }

      const retryWith = (token: string) => {
        original._isRetry = true;
        original.headers.set('Authorization', `Bearer ${token}`);
        return http.request(original);
      };

      if (!isRefreshing) {
        isRefreshing = true;
        try {
          const res = await authService.refreshToken();
          isRefreshing = false;
          const waiters = refreshWaiters;
          refreshWaiters = [];
          waiters.forEach(w => w.resolve(res.token));
          return retryWith(res.token);
        } catch (refreshErr) {
          isRefreshing = false;
          const waiters = refreshWaiters;
          refreshWaiters = [];
          waiters.forEach(w => w.reject(refreshErr));
          authService.logout();
          return Promise.reject(refreshErr);
        }
      }

      // A refresh is already in flight: queue and wait for the new token
      const token = await new Promise<string>((resolve, reject) => {
        refreshWaiters.push({ resolve, reject });
      });
      return retryWith(token);
    }
  );
}
