import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const isAuthRequest = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');
  const token = auth.token();
  const withToken = (accessToken: string | null) =>
    accessToken && !isAuthRequest ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }) : req;

  if (token && !isAuthRequest && auth.shouldRefreshToken()) {
    return from(auth.refreshSession()).pipe(
      switchMap((session) => next(withToken(session.accessToken))),
      catchError((error) => {
        router.navigateByUrl('/auth');
        return throwError(() => error);
      }),
    );
  }

  return next(withToken(token)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token && !isAuthRequest) {
        return from(auth.refreshSession()).pipe(
          switchMap((session) => next(withToken(session.accessToken))),
          catchError((refreshError) => {
            auth.clear();
            router.navigateByUrl('/auth');
            return throwError(() => refreshError);
          }),
        );
      }

      if (error.status === 401 && !isAuthRequest) {
        auth.clear();
        router.navigateByUrl('/auth');
      }

      return throwError(() => error);
    }),
  );
};
