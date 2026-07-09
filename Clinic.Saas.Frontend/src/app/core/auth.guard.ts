import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Role } from './models';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() || router.createUrlTree(['/auth']);
};

export const authChildGuard: CanActivateChildFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() || router.createUrlTree(['/auth']);
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return !auth.isAuthenticated() || router.createUrlTree([defaultRouteForRole(auth.user()?.role)]);
};

export const roleGuard = (roles: Role[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/auth']);
  return auth.hasRole(roles) || router.createUrlTree([defaultRouteForRole(auth.user()?.role)]);
};

export const dashboardGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/auth']);
  return auth.user()?.role === 'SuperAdmin' ? router.createUrlTree(['/platform/dashboard']) : true;
};

export const clinicRoleGuard = (roles?: Role[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/auth']);
  if (auth.user()?.role === 'SuperAdmin') return router.createUrlTree(['/platform/dashboard']);
  return !roles?.length || auth.hasRole(roles) || router.createUrlTree(['/dashboard']);
};

function defaultRouteForRole(role?: Role) {
  return role === 'SuperAdmin' ? '/platform/dashboard' : '/dashboard';
}
