import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  // Skip auth header for login or refresh
  const isAuthEndpoint = req.url.includes('/api/v1/auth/login') || req.url.includes('/api/v1/auth/refresh');

  let cloned = req;
  if (token && !isAuthEndpoint) {
    cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(cloned).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
        if (!isRefreshing) {
          isRefreshing = true;
          return auth.refreshToken().pipe(
            switchMap(res => {
              isRefreshing = false;
              const retryReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${res.token}`
                }
              });
              return next(retryReq);
            }),
            catchError(refreshErr => {
              isRefreshing = false;
              auth.logout();
              return throwError(() => refreshErr);
            })
          );
        }
      }
      return throwError(() => error);
    })
  );
};
