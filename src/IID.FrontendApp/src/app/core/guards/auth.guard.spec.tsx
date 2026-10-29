import { cleanup, render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { authCleared, authSucceeded } from '../store/auth.slice';
import { store } from '../store/store';
import { toastsCleared } from '../store/toast.slice';
import { RequireAuth, RequireManager } from './auth.guard';

describe('Auth Route Guards', () => {
  beforeEach(() => {
    localStorage.clear();
    store.dispatch(authCleared());
    store.dispatch(toastsCleared());
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
    store.dispatch(authCleared());
    store.dispatch(toastsCleared());
  });

  describe('RequireAuth', () => {
    it('renders child content when user is authenticated', () => {
      store.dispatch(
        authSucceeded({
          token: 'valid-jwt',
          user: { id: 'u1', email: 'saler@iid.local', roles: ['Saler'] }
        })
      );

      render(
        <Provider store={store}>
          <MemoryRouter initialEntries={['/protected']}>
            <Routes>
              <Route path="/login" element={<div>Login Screen</div>} />
              <Route
                path="/protected"
                element={
                  <RequireAuth>
                    <div>Protected Content</div>
                  </RequireAuth>
                }
              />
            </Routes>
          </MemoryRouter>
        </Provider>
      );

      expect(screen.getByText('Protected Content')).toBeInTheDocument();
      expect(screen.queryByText('Login Screen')).not.toBeInTheDocument();
    });

    it('redirects to /login when user is unauthenticated', () => {
      render(
        <Provider store={store}>
          <MemoryRouter initialEntries={['/protected']}>
            <Routes>
              <Route path="/login" element={<div>Login Screen</div>} />
              <Route
                path="/protected"
                element={
                  <RequireAuth>
                    <div>Protected Content</div>
                  </RequireAuth>
                }
              />
            </Routes>
          </MemoryRouter>
        </Provider>
      );

      expect(screen.getByText('Login Screen')).toBeInTheDocument();
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });
  });

  describe('RequireManager', () => {
    it('renders child content for an authenticated manager without warning toast', () => {
      store.dispatch(
        authSucceeded({
          token: 'manager-jwt',
          user: { id: 'u-mgr', email: 'admin@iid.local', roles: ['Manager'] }
        })
      );

      render(
        <Provider store={store}>
          <MemoryRouter initialEntries={['/dealerships']}>
            <Routes>
              <Route path="/dashboard" element={<div>Dashboard Screen</div>} />
              <Route path="/dealerships" element={<RequireManager />}>
                <Route index element={<div>Manager Dealerships Content</div>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </Provider>
      );

      expect(screen.getByText('Manager Dealerships Content')).toBeInTheDocument();
      expect(store.getState().toast.toasts).toHaveLength(0);
    });

    it('shows warning toast and redirects to /dashboard for a non-manager user', () => {
      store.dispatch(
        authSucceeded({
          token: 'saler-jwt',
          user: { id: 'u-saler', email: 'saler@iid.local', roles: ['Saler'] }
        })
      );

      render(
        <Provider store={store}>
          <MemoryRouter initialEntries={['/dealerships']}>
            <Routes>
              <Route path="/dashboard" element={<div>Dashboard Screen</div>} />
              <Route path="/dealerships" element={<RequireManager />}>
                <Route index element={<div>Manager Dealerships Content</div>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </Provider>
      );

      expect(screen.getByText('Dashboard Screen')).toBeInTheDocument();
      expect(screen.queryByText('Manager Dealerships Content')).not.toBeInTheDocument();

      const toasts = store.getState().toast.toasts;
      expect(toasts).toHaveLength(1);
      expect(toasts[0].type).toBe('warning');
      expect(toasts[0].title).toBe('Access Restricted');
      expect(toasts[0].message).toBe('This action requires Manager permissions.');
    });
  });
});
