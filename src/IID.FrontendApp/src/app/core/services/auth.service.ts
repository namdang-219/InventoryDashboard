import { createAsyncThunk } from '@reduxjs/toolkit';
import { useMemo } from 'react';
import { LoginRequest, LoginResponse, RefreshTokenRequest, UserDto } from '../models/auth.model';
import { useAppSelector } from '../store/hooks';
import { REFRESH_TOKEN_KEY, TOKEN_KEY, USER_KEY, authCleared, authSucceeded } from '../store/auth.slice';
import { AppDispatch, store } from '../store/store';
import { navigateTo } from '../utils/navigation';
import { apiConfig, post } from './api.service';
import { toastService } from './toast.service';

function handleAuthSuccess(res: LoginResponse): void {
  localStorage.setItem(TOKEN_KEY, res.token);
  localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
  localStorage.setItem(USER_KEY, JSON.stringify(res.user));
  store.dispatch(authSucceeded({ token: res.token, user: res.user }));
}

/** POST /api/v1/auth/login */
export const loginThunk = createAsyncThunk<LoginResponse, LoginRequest, { rejectValue: unknown }>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      const res = await post<LoginResponse>(apiConfig.getApiUrl('/api/v1/auth/login'), credentials);
      handleAuthSuccess(res);
      toastService.success('Welcome back', `Logged in as ${res.user.email}`);
      return res;
    } catch (err) {
      toastService.error('Authentication Failed', 'Invalid email or password.');
      return rejectWithValue(err);
    }
  }
);

/** POST /api/v1/auth/refresh */
export const refreshTokenThunk = createAsyncThunk<LoginResponse, void, { rejectValue: unknown }>(
  'auth/refresh',
  async (_, { rejectWithValue }) => {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY) ?? '';
    const req: RefreshTokenRequest = { refreshToken };
    try {
      const res = await post<LoginResponse>(apiConfig.getApiUrl('/api/v1/auth/refresh'), req);
      handleAuthSuccess(res);
      return res;
    } catch (err) {
      authService.logout();
      return rejectWithValue(err);
    }
  }
);

export const authService = {
  /** Resolves with the login response, rejects with the original HTTP error. */
  async login(credentials: LoginRequest, dispatch: AppDispatch = store.dispatch): Promise<LoginResponse> {
    return dispatch(loginThunk(credentials)).unwrap();
  },

  async refreshToken(dispatch: AppDispatch = store.dispatch): Promise<LoginResponse> {
    return dispatch(refreshTokenThunk()).unwrap();
  },

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    store.dispatch(authCleared());
    navigateTo('/login');
  },

  token(): string | null {
    return store.getState().auth.token;
  },

  isAuthenticated(): boolean {
    return !!store.getState().auth.token;
  },

  isManager(): boolean {
    return computeIsManager(store.getState().auth.currentUser);
  }
};

function computeIsManager(user: UserDto | null): boolean {
  return user ? user.roles.some(r => r.toLowerCase() === 'manager') : false;
}

function computeIsSaler(user: UserDto | null): boolean {
  return user
    ? user.roles.some(r => {
        const role = r.toLowerCase();
        return role === 'saler' || role === 'sales' || role === 'viewer';
      })
    : false;
}

export function useAuth() {
  const currentUser = useAppSelector(s => s.auth.currentUser);
  const token = useAppSelector(s => s.auth.token);

  return useMemo(() => {
    const isManager = computeIsManager(currentUser);
    const isSaler = computeIsSaler(currentUser);
    return {
      currentUser,
      token,
      isAuthenticated: !!token,
      isManager,
      isSaler,
      canMarkSold: isSaler && !isManager,
      login: authService.login,
      refreshToken: authService.refreshToken,
      logout: authService.logout
    };
  }, [currentUser, token]);
}
