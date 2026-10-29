import {
  AxiosAdapter,
  AxiosError,
  AxiosHeaders,
  AxiosResponse,
  InternalAxiosRequestConfig
} from 'axios';
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginResponse } from '../models/auth.model';
import { http } from '../services/api.service';
import {
  REFRESH_TOKEN_KEY,
  TOKEN_KEY,
  USER_KEY,
  authCleared,
  authSucceeded
} from '../store/auth.slice';
import { store } from '../store/store';
import { toastsCleared } from '../store/toast.slice';
import { setNavigator } from '../utils/navigation';
import { setupAuthInterceptor } from './auth.interceptor';

function okResponse<T>(config: InternalAxiosRequestConfig, data: T): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: 'OK',
    headers: {},
    config
  };
}

function rejectStatus(
  config: InternalAxiosRequestConfig,
  status: number,
  statusText: string,
  data: unknown = null
): Promise<never> {
  const response: AxiosResponse = {
    data,
    status,
    statusText,
    headers: {},
    config
  };
  return Promise.reject(
    new AxiosError(
      `Request failed with status code ${status}`,
      AxiosError.ERR_BAD_REQUEST,
      config,
      null,
      response
    )
  );
}

function getAuthHeader(config: InternalAxiosRequestConfig): string | undefined {
  const val = AxiosHeaders.from(config.headers).get('Authorization');
  return typeof val === 'string' ? val : undefined;
}

describe('setupAuthInterceptor', () => {
  const originalAdapter = http.defaults.adapter;
  const navigateSpy = vi.fn();

  const mockUser = {
    id: 'u-1',
    email: 'admin@iid.local',
    roles: ['Manager']
  };

  beforeAll(() => {
    setupAuthInterceptor();
  });

  beforeEach(() => {
    localStorage.clear();
    store.dispatch(authCleared());
    store.dispatch(toastsCleared());
    navigateSpy.mockReset();
    setNavigator(navigateSpy);
  });

  afterEach(() => {
    http.defaults.adapter = originalAdapter;
    localStorage.clear();
    store.dispatch(authCleared());
    store.dispatch(toastsCleared());
  });

  it('attaches Authorization: Bearer <token> header on protected API calls', async () => {
    store.dispatch(authSucceeded({ token: 'my-jwt-token', user: mockUser }));

    let capturedAuth: string | undefined;
    const adapter: AxiosAdapter = async config => {
      capturedAuth = getAuthHeader(config);
      return okResponse(config, { ok: true });
    };
    http.defaults.adapter = adapter;

    const res = await http.get('http://localhost:8080/api/v1/vehicles');
    expect(res.data).toEqual({ ok: true });
    expect(capturedAuth).toBe('Bearer my-jwt-token');
  });

  it('skips Authorization header on /api/v1/auth/login and /api/v1/auth/refresh', async () => {
    store.dispatch(authSucceeded({ token: 'existing-jwt-token', user: mockUser }));

    const capturedAuthHeaders: (string | undefined)[] = [];
    const adapter: AxiosAdapter = async config => {
      capturedAuthHeaders.push(getAuthHeader(config));
      return okResponse(config, { ok: true });
    };
    http.defaults.adapter = adapter;

    await http.post('http://localhost:8080/api/v1/auth/login', {
      email: 'admin@iid.local',
      password: 'secret'
    });
    await http.post('http://localhost:8080/api/v1/auth/refresh', {
      refreshToken: 'rt-1'
    });

    expect(capturedAuthHeaders).toEqual([undefined, undefined]);
  });

  it('on 401 calls refresh once and retries original request with new token', async () => {
    localStorage.setItem(TOKEN_KEY, 'expired-token');
    localStorage.setItem(REFRESH_TOKEN_KEY, 'valid-refresh-token');
    store.dispatch(authSucceeded({ token: 'expired-token', user: mockUser }));

    let refreshCallCount = 0;
    const seenAuthOnVehicles: (string | undefined)[] = [];

    const refreshedPayload: LoginResponse = {
      token: 'renewed-token-999',
      refreshToken: 'renewed-refresh-999',
      refreshTokenExpiry: '2026-12-31T00:00:00Z',
      user: mockUser
    };

    const adapter: AxiosAdapter = async config => {
      if (config.url?.includes('/api/v1/auth/refresh')) {
        refreshCallCount += 1;
        expect(getAuthHeader(config)).toBeUndefined();
        return okResponse(config, refreshedPayload);
      }

      const authHeader = getAuthHeader(config);
      seenAuthOnVehicles.push(authHeader);
      if (authHeader !== 'Bearer renewed-token-999') {
        return rejectStatus(config, 401, 'Unauthorized', { message: 'Token expired' });
      }
      return okResponse(config, { items: ['veh-1', 'veh-2'] });
    };
    http.defaults.adapter = adapter;

    const res = await http.get('http://localhost:8080/api/v1/vehicles');

    expect(refreshCallCount).toBe(1);
    expect(seenAuthOnVehicles).toEqual(['Bearer expired-token', 'Bearer renewed-token-999']);
    expect(res.data).toEqual({ items: ['veh-1', 'veh-2'] });
    expect(store.getState().auth.token).toBe('renewed-token-999');
    expect(localStorage.getItem(TOKEN_KEY)).toBe('renewed-token-999');
  });

  it('concurrent 401 requests share ONE single-flight refresh call and all resolve', async () => {
    localStorage.setItem(TOKEN_KEY, 'expired-token');
    localStorage.setItem(REFRESH_TOKEN_KEY, 'valid-refresh-token');
    store.dispatch(authSucceeded({ token: 'expired-token', user: mockUser }));

    let refreshCallCount = 0;
    const retriedEndpoints: string[] = [];

    const refreshedPayload: LoginResponse = {
      token: 'shared-fresh-token',
      refreshToken: 'shared-fresh-refresh',
      refreshTokenExpiry: '2026-12-31T00:00:00Z',
      user: mockUser
    };

    const adapter: AxiosAdapter = async config => {
      if (config.url?.includes('/api/v1/auth/refresh')) {
        refreshCallCount += 1;
        await new Promise(resolve => setTimeout(resolve, 15));
        return okResponse(config, refreshedPayload);
      }

      const authHeader = getAuthHeader(config);
      if (authHeader !== 'Bearer shared-fresh-token') {
        return rejectStatus(config, 401, 'Unauthorized');
      }

      retriedEndpoints.push(config.url ?? '');
      return okResponse(config, { endpoint: config.url });
    };
    http.defaults.adapter = adapter;

    const [r1, r2, r3] = await Promise.all([
      http.get('http://localhost:8080/api/v1/vehicles'),
      http.get('http://localhost:8080/api/v1/dashboard'),
      http.get('http://localhost:8080/api/v1/dealerships')
    ]);

    expect(refreshCallCount).toBe(1);
    expect(retriedEndpoints).toHaveLength(3);
    expect(r1.data).toEqual({ endpoint: 'http://localhost:8080/api/v1/vehicles' });
    expect(r2.data).toEqual({ endpoint: 'http://localhost:8080/api/v1/dashboard' });
    expect(r3.data).toEqual({ endpoint: 'http://localhost:8080/api/v1/dealerships' });
  });

  it('refresh failure clears auth state and logs out to /login', async () => {
    localStorage.setItem(TOKEN_KEY, 'expired-token');
    localStorage.setItem(REFRESH_TOKEN_KEY, 'expired-refresh-token');
    localStorage.setItem(USER_KEY, JSON.stringify(mockUser));
    store.dispatch(authSucceeded({ token: 'expired-token', user: mockUser }));

    const adapter: AxiosAdapter = async config => {
      if (config.url?.includes('/api/v1/auth/refresh')) {
        return rejectStatus(config, 401, 'Unauthorized', { message: 'Refresh token invalid' });
      }
      return rejectStatus(config, 401, 'Unauthorized', { message: 'Access token expired' });
    };
    http.defaults.adapter = adapter;

    await expect(http.get('http://localhost:8080/api/v1/vehicles')).rejects.toBeDefined();

    expect(store.getState().auth.token).toBeNull();
    expect(store.getState().auth.currentUser).toBeNull();
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
    expect(localStorage.getItem(USER_KEY)).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/login');
  });
});
