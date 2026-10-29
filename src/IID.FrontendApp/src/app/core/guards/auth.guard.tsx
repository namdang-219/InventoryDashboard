import { useEffect } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../services/auth.service';
import { toastService } from '../services/toast.service';

/** Route guard: requires an authenticated user, otherwise redirects to /login. */
export function RequireAuth({ children }: { children?: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) {
    return <>{children ?? <Outlet />}</>;
  }
  return <Navigate to="/login" replace />;
}

/** Route guard: requires a manager, otherwise warns and redirects to /dashboard. */
export function RequireManager({ children }: { children?: React.ReactNode }) {
  const { isAuthenticated, isManager } = useAuth();
  const allowed = isAuthenticated && isManager;

  useEffect(() => {
    if (!allowed) {
      toastService.warning('Access Restricted', 'This action requires Manager permissions.');
    }
  }, [allowed]);

  if (allowed) {
    return <>{children ?? <Outlet />}</>;
  }
  return <Navigate to="/dashboard" replace />;
}
