import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth.guard';
import { ShellComponent } from './shared/shell/shell.component';

export const routes: Routes = [
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/auth.component').then((m) => m.AuthComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      { path: 'patients', loadComponent: () => import('./features/patients/patients.component').then((m) => m.PatientsComponent) },
      { path: 'appointments', loadComponent: () => import('./features/appointments/appointments.component').then((m) => m.AppointmentsComponent) },
      { path: 'visits', canActivate: [roleGuard(['Admin', 'Doctor'])], loadComponent: () => import('./features/visits/visits.component').then((m) => m.VisitsComponent) },
      { path: 'prescriptions', canActivate: [roleGuard(['Admin', 'Doctor'])], loadComponent: () => import('./features/prescriptions/prescriptions.component').then((m) => m.PrescriptionsComponent) },
      { path: 'billing', canActivate: [roleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/billing/billing.component').then((m) => m.BillingComponent) },
      { path: 'operations', loadComponent: () => import('./features/operations/operations.component').then((m) => m.OperationsComponent) },
      { path: 'users', canActivate: [roleGuard(['Admin'])], loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent) },
      { path: 'admin', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/admin/admin.component').then((m) => m.AdminComponent) },
    ],
  },
  { path: '**', redirectTo: '' },
];
