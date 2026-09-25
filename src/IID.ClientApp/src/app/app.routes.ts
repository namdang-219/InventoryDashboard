import { Routes } from '@angular/router';
import { authGuard, managerGuard } from './core/guards/auth.guard';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/pages/login-page/login-page.component').then(m => m.LoginPageComponent)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard-page/dashboard-page.component').then(
            m => m.DashboardPageComponent
          )
      },
      {
        path: 'vehicles',
        loadComponent: () =>
          import('./features/vehicles/pages/vehicle-list-page/vehicle-list-page.component').then(
            m => m.VehicleListPageComponent
          )
      },
      {
        path: 'vehicles/aging',
        loadComponent: () =>
          import('./features/vehicles/pages/vehicle-list-page/vehicle-list-page.component').then(
            m => m.VehicleListPageComponent
          )
      },
      {
        path: 'dealerships',
        canActivate: [managerGuard],
        loadComponent: () =>
          import('./features/dealerships/pages/dealership-list-page/dealership-list-page.component').then(
            m => m.DealershipListPageComponent
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
