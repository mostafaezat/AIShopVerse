import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService, AuthSessionData, UserDto } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;
  const api = environment.apiIdentityEndpoint;

  const user: UserDto = { id: 'usr-1', fullName: 'John Doe', email: 'john@example.com', phoneNumber: '01000000000', roles: ['Customer'] };
  const futureExpiry = '2099-01-01T00:00:00Z';
  const session: AuthSessionData = {
    token: 'header.payload.sig',
    refreshToken: 'rt-1',
    expiresAt: futureExpiry,
    user
  };

  beforeEach(() => {
    sessionStorage.clear();
    routerSpy = jasmine.createSpyObj('Router', ['navigateByUrl']);
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('persists session and exposes user on success envelope', () => {
      let emitted: UserDto | null | undefined;
      service.currentUser$.subscribe(u => emitted = u);

      service.login({ email: user.email, password: 'secret' }).subscribe(res => {
        expect(res.isSuccess).toBeTrue();
        expect(res.data?.token).toBe(session.token);
      });

      const req = httpMock.expectOne(`${api}auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: user.email, password: 'secret' });
      req.flush({ isSuccess: true, message: 'Login successful.', data: session });

      expect(sessionStorage.getItem('accessToken')).toBe(session.token);
      expect(sessionStorage.getItem('refreshToken')).toBe(session.refreshToken);
      expect(sessionStorage.getItem('expiresAt')).toBe(futureExpiry);
      expect(service.isLoggedIn()).toBeTrue();
      expect(emitted?.id).toBe(user.id);
    });

    it('does not persist session when envelope has isSuccess=false (invalid credentials)', () => {
      let emitted: UserDto | null | undefined;
      service.currentUser$.subscribe(u => emitted = u);

      service.login({ email: user.email, password: 'wrong' }).subscribe(res => {
        expect(res.isSuccess).toBeFalse();
        expect(res.message).toBe('Invalid email or password.');
      });

      const req = httpMock.expectOne(`${api}auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush({ isSuccess: false, message: 'Invalid email or password.', data: null });

      expect(service.isLoggedIn()).toBeFalse();
      expect(sessionStorage.getItem('accessToken')).toBeNull();
      expect(emitted).toBeNull();
    });

    it('does not persist session on API-level failure (HTTP error)', () => {
      let failed = false;
      service.login({ email: user.email, password: 'secret' }).subscribe({
        next: () => fail('should not emit a value'),
        error: () => { failed = true; }
      });

      const req = httpMock.expectOne(`${api}auth/login`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Server Error' });

      expect(failed).toBeTrue();
      expect(service.isLoggedIn()).toBeFalse();
      expect(sessionStorage.getItem('accessToken')).toBeNull();
    });

    it('does not persist session when success envelope lacks a token', () => {
      service.login({ email: user.email, password: 'secret' }).subscribe();

      const req = httpMock.expectOne(`${api}auth/login`);
      req.flush({ isSuccess: true, message: 'Login successful.', data: { ...session, token: '' } });

      expect(service.isLoggedIn()).toBeFalse();
      expect(sessionStorage.getItem('accessToken')).toBeNull();
    });
  });

  describe('register', () => {
    it('persists session on successful registration', () => {
      service.register({ fullName: 'John Doe', email: user.email, password: 'secret123' }).subscribe(res => {
        expect(res.isSuccess).toBeTrue();
      });

      const req = httpMock.expectOne(`${api}auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ fullName: 'John Doe', email: user.email, password: 'secret123' });
      req.flush({ isSuccess: true, message: 'Registered.', data: session });

      expect(service.isLoggedIn()).toBeTrue();
      expect(sessionStorage.getItem('refreshToken')).toBe(session.refreshToken);
    });

    it('does not persist session when registration fails', () => {
      service.register({ fullName: 'John Doe', email: user.email, password: 'secret123' }).subscribe(res => {
        expect(res.isSuccess).toBeFalse();
      });

      const req = httpMock.expectOne(`${api}auth/register`);
      req.flush({ isSuccess: false, message: 'Email already registered.', data: null });

      expect(service.isLoggedIn()).toBeFalse();
    });
  });

  describe('session restoration on page refresh', () => {
    function freshService(): AuthService {
      return new AuthService(TestBed.inject(HttpClient), routerSpy);
    }

    it('restores the stored user from authUser on construction', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);
      sessionStorage.setItem('expiresAt', futureExpiry);
      sessionStorage.setItem('authUser', JSON.stringify(user));

      const restored = freshService();

      expect(restored.isLoggedIn()).toBeTrue();
      let current: UserDto | null | undefined;
      restored.currentUser$.subscribe(u => current = u);
      expect(current?.id).toBe(user.id);
      expect(current?.roles).toEqual(['Customer']);
    });

    it('restores roles from multi-value JWT claim when authUser is absent (legacy sessions)', () => {
      const header = btoa('{"alg":"HS256"}');
      const payload = btoa(JSON.stringify({ sub: 'usr-1', role: ['SuperAdmin', 'Admin'], email: user.email }));
      sessionStorage.setItem('accessToken', `${header}.${payload}.sig`);
      sessionStorage.setItem('expiresAt', futureExpiry);

      const restored = freshService();

      let current: UserDto | null | undefined;
      restored.currentUser$.subscribe(u => current = u);
      expect(current?.id).toBe('usr-1');
      expect(current?.roles).toEqual(['SuperAdmin', 'Admin']);
    });

    it('does not restore a session when the token has expired', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('expiresAt', '2000-01-01T00:00:00Z');

      const restored = freshService();

      expect(restored.isLoggedIn()).toBeFalse();
      let current: UserDto | null | undefined;
      restored.currentUser$.subscribe(u => current = u);
      expect(current).toBeNull();
    });
  });

  describe('logout', () => {
    it('clears all auth state and navigates home', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);
      sessionStorage.setItem('expiresAt', futureExpiry);
      sessionStorage.setItem('authUser', JSON.stringify(user));

      service.logout();

      const req = httpMock.expectOne(`${api}auth/logout`);
      expect(req.request.body).toEqual({ refreshToken: session.refreshToken });
      req.flush({ isSuccess: true, data: true });

      expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/');
      expect(service.isLoggedIn()).toBeFalse();
      expect(sessionStorage.getItem('accessToken')).toBeNull();
      expect(sessionStorage.getItem('refreshToken')).toBeNull();
      expect(sessionStorage.getItem('expiresAt')).toBeNull();
      let current: UserDto | null | undefined;
      service.currentUser$.subscribe(u => current = u);
      expect(current).toBeNull();
    });

    it('is a no-op when there is no active session', () => {
      service.logout();

      httpMock.expectNone(`${api}auth/logout`);
      expect(routerSpy.navigateByUrl).not.toHaveBeenCalled();
    });

    it('does not issue the server logout call a second time after state is cleared', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);

      service.logout();
      httpMock.expectOne(`${api}auth/logout`).flush({ isSuccess: true, data: true });

      service.logout();
      httpMock.expectNone(`${api}auth/logout`);
    });
  });

  describe('refreshAccessToken', () => {
    it('calls auth/refresh-token with the stored tokens and persists the new session', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);
      sessionStorage.setItem('expiresAt', futureExpiry);

      service.refreshAccessToken().subscribe(res => {
        expect(res.isSuccess).toBeTrue();
      });

      const req = httpMock.expectOne(`${api}auth/refresh-token`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token: session.token, refreshToken: session.refreshToken });

      const newSession: AuthSessionData = { ...session, token: 'new.access.sig', refreshToken: 'rt-2' };
      req.flush({ isSuccess: true, message: 'Token refreshed.', data: newSession });

      expect(sessionStorage.getItem('accessToken')).toBe('new.access.sig');
      expect(sessionStorage.getItem('refreshToken')).toBe('rt-2');
    });

    it('emits an error and does not call the API when no tokens are stored', () => {
      let failed = false;
      service.refreshAccessToken().subscribe({
        next: () => fail('should not emit a value'),
        error: () => { failed = true; }
      });

      expect(failed).toBeTrue();
      httpMock.expectNone(`${api}auth/refresh-token`);
    });

    it('keeps the old tokens when the refresh envelope reports failure', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);

      service.refreshAccessToken().subscribe(res => {
        expect(res.isSuccess).toBeFalse();
      });
      const req = httpMock.expectOne(`${api}auth/refresh-token`);
      req.flush({ isSuccess: false, message: 'Invalid refresh token.', data: null });

      expect(sessionStorage.getItem('accessToken')).toBe(session.token);
      expect(sessionStorage.getItem('refreshToken')).toBe(session.refreshToken);
    });

    it('shares a single in-flight refresh request across concurrent callers', () => {
      sessionStorage.setItem('accessToken', session.token);
      sessionStorage.setItem('refreshToken', session.refreshToken);

      service.refreshAccessToken().subscribe();
      service.refreshAccessToken().subscribe();

      const req = httpMock.expectOne(`${api}auth/refresh-token`);
      req.flush({ isSuccess: true, message: 'Token refreshed.', data: session });

      httpMock.expectNone(`${api}auth/refresh-token`);
    });
  });
});