import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, finalize, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  roles: string[];
}

export interface ApiEnvelope<T> {
  isSuccess: boolean;
  message?: string;
  data?: T;
}

export interface AuthSessionData {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly accessTokenKey = 'accessToken';
  private readonly refreshTokenKey = 'refreshToken';
  private readonly expiresAtKey = 'expiresAt';
  private readonly authUserKey = 'authUser';

  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private refreshInFlight: Observable<ApiEnvelope<AuthSessionData>> | null = null;

  get currentUserId(): string | null {
    return this.currentUserSubject.value?.id || null;
  }

  constructor(private http: HttpClient, private router: Router) {
    this.loadStoredUser();
  }

  login(model: { email: string; password: string }): Observable<ApiEnvelope<AuthSessionData>> {
    return this.http.post<ApiEnvelope<AuthSessionData>>(`${environment.apiIdentityEndpoint}auth/login`, model)
      .pipe(tap((response) => {
        if (response?.isSuccess && response.data?.token) this.setSession(response.data);
      }));
  }

  register(model: { fullName: string; email: string; password: string }): Observable<ApiEnvelope<AuthSessionData>> {
    return this.http.post<ApiEnvelope<AuthSessionData>>(`${environment.apiIdentityEndpoint}auth/register`, model)
      .pipe(tap((response) => {
        if (response?.isSuccess && response.data?.token) this.setSession(response.data);
      }));
  }

  loginWithGoogle(idToken: string): Observable<ApiEnvelope<AuthSessionData>> {
    return this.http.post<ApiEnvelope<AuthSessionData>>(`${environment.apiIdentityEndpoint}auth/google`, { idToken })
      .pipe(tap((response) => {
        if (response?.isSuccess && response.data?.token) this.setSession(response.data);
      }));
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${environment.apiIdentityEndpoint}auth/forgot-password`, { email });
  }

  resetPassword(model: { email: string; token: string; newPassword: string }): Observable<any> {
    return this.http.post(`${environment.apiIdentityEndpoint}auth/reset-password`, model);
  }

  setSession(authResult: AuthSessionData) {
    if (!authResult.token) return;
    sessionStorage.setItem(this.accessTokenKey, authResult.token);
    sessionStorage.setItem(this.refreshTokenKey, authResult.refreshToken || '');
    sessionStorage.setItem(this.expiresAtKey, authResult.expiresAt);
    sessionStorage.setItem(this.authUserKey, JSON.stringify(authResult.user));
    this.currentUserSubject.next(authResult.user);
  }

  logout(expired = false) {
    const refreshToken = sessionStorage.getItem(this.refreshTokenKey);
    const token = sessionStorage.getItem(this.accessTokenKey);
    sessionStorage.removeItem(this.accessTokenKey);
    sessionStorage.removeItem(this.refreshTokenKey);
    sessionStorage.removeItem(this.expiresAtKey);
    sessionStorage.removeItem(this.authUserKey);
    this.currentUserSubject.next(null);
    if (!token && !refreshToken) return;
    if (refreshToken) {
      // Best-effort server-side revocation; local state is cleared regardless.
      this.http.post(`${environment.apiIdentityEndpoint}auth/logout`, { refreshToken })
        .subscribe({ error: () => { /* ignore: session is cleared locally */ } });
    }
    if (expired) {
      this.router.navigate(['/login'], {
        queryParams: { sessionExpired: '1', returnUrl: this.router.url }
      });
    } else {
      this.router.navigateByUrl('/');
    }
  }

  isLoggedIn(): boolean {
    const token = sessionStorage.getItem(this.accessTokenKey);
    const expiresAt = sessionStorage.getItem(this.expiresAtKey);
    if (!token || !expiresAt) return false;
    return new Date(expiresAt) > new Date();
  }

  getToken(): string | null {
    return sessionStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem(this.refreshTokenKey);
  }

  refreshAccessToken(): Observable<ApiEnvelope<AuthSessionData>> {
    const token = this.getToken();
    const refreshToken = this.getRefreshToken();
    if (!token || !refreshToken) {
      return throwError(() => new Error('No session to refresh.'));
    }
    if (!this.refreshInFlight) {
      this.refreshInFlight = this.http.post<ApiEnvelope<AuthSessionData>>(
        `${environment.apiIdentityEndpoint}auth/refresh-token`,
        { token, refreshToken }
      ).pipe(
        tap((response) => {
          if (response?.isSuccess && response.data?.token) this.setSession(response.data);
        }),
        shareReplay(1),
        finalize(() => { this.refreshInFlight = null; })
      );
    }
    return this.refreshInFlight;
  }

  private loadStoredUser() {
    if (!this.isLoggedIn()) return;

    const storedUser = sessionStorage.getItem(this.authUserKey);
    if (storedUser) {
      try {
        const user = JSON.parse(storedUser) as UserDto;
        if (user && user.id) {
          this.currentUserSubject.next(user);
          return;
        }
      } catch { /* fall through to JWT decode */ }
    }

    const token = sessionStorage.getItem(this.accessTokenKey);
    if (!token) return;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.currentUserSubject.next({
        id: payload.sub,
        fullName: payload.fullName || payload.name || '',
        email: payload.email || '',
        phoneNumber: payload.phoneNumber || '',
        roles: this.extractRoles(payload)
      });
    } catch { /* invalid token payload; session will be dropped when expired */ }
  }

  private extractRoles(payload: any): string[] {
    const role = payload.role;
    const roles = payload.roles;
    if (Array.isArray(roles)) return roles;
    if (Array.isArray(role)) return role;
    if (typeof role === 'string' && role) return [role];
    return [];
  }
}