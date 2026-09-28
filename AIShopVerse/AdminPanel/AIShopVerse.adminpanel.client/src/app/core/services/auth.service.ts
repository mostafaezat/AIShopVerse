import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto, AuthResponseDto } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private userSubject = new BehaviorSubject<UserDto | null>(null);
  user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    this.loadStoredUser();
  }

  login(email: string, password: string): Observable<AuthResponseDto> {
    return this.http.post<any>(`${environment.apiEndpoint}auth/login`, { email, password })
      .pipe(
        map(r => r.data),
        tap(response => {
          if (response?.token) {
            this.setSession(response);
          }
        })
      );
  }

  setSession(response: AuthResponseDto): void {
    if (response.token) localStorage.setItem('token', response.token);
    if (response.refreshToken) localStorage.setItem('refreshToken', response.refreshToken);
    if (response.user) {
      localStorage.setItem('user', JSON.stringify(response.user));
      this.userSubject.next(response.user);
    }
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  isSuperAdmin(): boolean {
    const user = this.userSubject.value;
    return user?.roles?.includes('SuperAdmin') ?? false;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  loadStoredUser(): void {
    const userJson = localStorage.getItem('user');
    if (userJson) {
      try {
        const user = JSON.parse(userJson) as UserDto;
        this.userSubject.next(user);
      } catch {}
    }
  }
}
