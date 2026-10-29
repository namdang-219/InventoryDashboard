import { Suspense, lazy } from 'react';
import { Navigate, createBrowserRouter } from 'react-router-dom';
import { RequireAuth, RequireManager } from './core/guards/auth.guard';
import { setNavigator } from './core/utils/navigation';
import { ShellComponent } from './layout/shell/shell';

const LoginPageComponent = lazy(() =>
  import('./features/auth/pages/login-page/login-page').then(m => ({ default: m.LoginPageComponent }))
);
const DashboardPageComponent = lazy(() =>
  import('./features/dashboard/pages/dashboard-page/dashboard-page').then(m => ({
    default: m.DashboardPageComponent
  }))
);
const VehicleListPageComponent = lazy(() =>
  import('./features/vehicles/pages/vehicle-list-page/vehicle-list-page').then(m => ({
    default: m.VehicleListPageComponent
  }))
);
const DealershipListPageComponent = lazy(() =>
  import('./features/dealerships/pages/dealership-list-page/dealership-list-page').then(m => ({
    default: m.DealershipListPageComponent
  }))
);

const lazyPage = (el: React.ReactNode) => <Suspense fallback={null}>{el}</Suspense>;

export const router = createBrowserRouter([
  {
    path: 'login',
    element: lazyPage(<LoginPageComponent />)
  },
  {
    path: '',
    element: (
      <RequireAuth>
        <ShellComponent />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <Navigate to="dashboard" replace /> },
      { path: 'dashboard', element: lazyPage(<DashboardPageComponent />) },
      { path: 'vehicles', element: lazyPage(<VehicleListPageComponent />) },
      { path: 'vehicles/aging', element: lazyPage(<VehicleListPageComponent />) },
      {
        path: 'dealerships',
        element: <RequireManager />,
        children: [{ index: true, element: lazyPage(<DealershipListPageComponent />) }]
      }
    ]
  },
  { path: '*', element: <Navigate to="/dashboard" replace /> }
]);

setNavigator((to, options) => router.navigate(to, options));
