import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const isAuthRoute = (url: string): boolean => url.includes('/auth/');

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const token = authService.getToken();
  if (token && !isAuthRoute(req.url)) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(req).pipe(
    catchError((error) => {
      if (error instanceof HttpErrorResponse && error.status === 401 &&
          !isAuthRoute(req.url) && !req.headers.has('X-Auth-Retry')) {
        return authService.refreshAccessToken().pipe(
          catchError(() => {
            authService.logout();
            return throwError(() => error);
          }),
          switchMap((response) => {
            if (!(response?.isSuccess && response.data?.token)) {
              authService.logout();
              return throwError(() => error);
            }
            const retried = req.clone({
              setHeaders: {
                'X-Auth-Retry': 'true',
                Authorization: `Bearer ${response.data.token}`
              }
            });
            return next(retried);
          })
        );
      }
      return throwError(() => error);
    })
  );
};