import { Routes } from '@angular/router';
import { authChildGuard, authGuard, clinicRoleGuard, dashboardGuard, guestGuard, roleGuard } from './core/auth.guard';
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
    canActivateChild: [authChildGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', canActivate: [dashboardGuard], loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent) },
      { path: 'patients', canActivate: [clinicRoleGuard(['Admin', 'Doctor', 'Reception'])], loadComponent: () => import('./features/patients/patients.component').then((m) => m.PatientsComponent) },
      { path: 'patients/:id/chart', canActivate: [clinicRoleGuard(['Admin', 'Doctor', 'Reception'])], loadComponent: () => import('./features/patient-chart/patient-chart.component').then((m) => m.PatientChartComponent) },
      { path: 'appointments', canActivate: [clinicRoleGuard(['Admin', 'Doctor', 'Reception'])], loadComponent: () => import('./features/appointments/appointments.component').then((m) => m.AppointmentsComponent) },
      { path: 'online-bookings', canActivate: [clinicRoleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/online-bookings/online-bookings.component').then((m) => m.OnlineBookingsComponent) },
      { path: 'visits', canActivate: [clinicRoleGuard(['Admin', 'Doctor'])], loadComponent: () => import('./features/visits/visits.component').then((m) => m.VisitsComponent) },
      { path: 'prescriptions', canActivate: [clinicRoleGuard(['Admin', 'Doctor'])], loadComponent: () => import('./features/prescriptions/prescriptions.component').then((m) => m.PrescriptionsComponent) },
      { path: 'billing', canActivate: [clinicRoleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/billing/billing.component').then((m) => m.BillingComponent) },
      { path: 'billing/invoices', canActivate: [clinicRoleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/invoices/invoices.component').then((m) => m.InvoicesComponent) },
      { path: 'billing/invoices/:id', canActivate: [clinicRoleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/invoices/invoices.component').then((m) => m.InvoicesComponent) },
      { path: 'reports', canActivate: [clinicRoleGuard(['Admin', 'Reception'])], loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent) },
      { path: 'operations', canActivate: [clinicRoleGuard(['Admin'])], loadComponent: () => import('./features/operations/operations.component').then((m) => m.OperationsComponent) },
      { path: 'users', canActivate: [clinicRoleGuard(['Admin'])], loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent) },
      { path: 'admin', redirectTo: 'platform/dashboard', pathMatch: 'full' },
      { path: 'admin/clinics/:id', redirectTo: 'platform/clinics/:id', pathMatch: 'full' },
      { path: 'platform/dashboard', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/clinics', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/clinics/:id', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/admin-clinic-detail/admin-clinic-detail.component').then((m) => m.AdminClinicDetailComponent) },
      { path: 'platform/subscriptions', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/plans', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/reports', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/audit-logs', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
      { path: 'platform/settings', canActivate: [roleGuard(['SuperAdmin'])], loadComponent: () => import('./features/platform/platform.component').then((m) => m.PlatformComponent) },
    ],
  },
  { path: '**', redirectTo: '' },
];
