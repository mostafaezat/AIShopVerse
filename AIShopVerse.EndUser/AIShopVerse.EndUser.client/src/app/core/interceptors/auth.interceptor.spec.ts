import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService, AuthSessionData, UserDto } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;
  const api = environment.apiIdentityEndpoint;

  const user: UserDto = { id: 'usr-1', fullName: 'John Doe', email: 'john@example.com', phoneNumber: '01000000000', roles: ['Customer'] };
  const futureExpiry = '2099-01-01T00:00:00Z';
  const session: AuthSessionData = {
    token: 'old.access.sig',
    refreshToken: 'rt-1',
    expiresAt: futureExpiry,
    user
  };
  const refreshedSession: AuthSessionData = {
    token: 'new.access.sig',
    refreshToken: 'rt-2',
    expiresAt: futureExpiry,
    user
  };

  beforeEach(() => {
    sessionStorage.clear();
    routerSpy = jasmine.createSpyObj('Router', ['navigateByUrl', 'navigate']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('attaches the bearer token to non-auth requests', () => {
    sessionStorage.setItem('accessToken', session.token);

    http.get<{ ok: boolean }>('/api/protected').subscribe(res => expect(res.ok).toBeTrue());

    const req = httpMock.expectOne('/api/protected');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${session.token}`);
    req.flush({ ok: true });
  });

  it('does not attach the bearer token to auth routes', () => {
    sessionStorage.setItem('accessToken', session.token);

    http.post(`${api}auth/login`, { email: 'a@b.c', password: 'x' }).subscribe();

    const req = httpMock.expectOne(`${api}auth/login`);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({ isSuccess: true, data: session });
  });

  it('on 401 refreshes the token and retries the original request exactly once', () => {
    sessionStorage.setItem('accessToken', session.token);
    sessionStorage.setItem('refreshToken', session.refreshToken);

    http.get<{ ok: boolean }>('/api/protected').subscribe(res => expect(res.ok).toBeTrue());

    httpMock.expectOne('/api/protected')
      .flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne(`${api}auth/refresh-token`);
    expect(refreshReq.request.body).toEqual({ token: session.token, refreshToken: session.refreshToken });
    refreshReq.flush({ isSuccess: true, message: 'Token refreshed.', data: refreshedSession });

    const retried = httpMock.expectOne('/api/protected');
    expect(retried.request.headers.get('Authorization')).toBe(`Bearer ${refreshedSession.token}`);
    expect(retried.request.headers.get('X-Auth-Retry')).toBe('true');
    retried.flush({ ok: true });

    expect(sessionStorage.getItem('accessToken')).toBe(refreshedSession.token);
    expect(sessionStorage.getItem('refreshToken')).toBe(refreshedSession.refreshToken);
  });

  it('logs out and does not retry when the refresh itself fails', () => {
    sessionStorage.setItem('accessToken', session.token);
    sessionStorage.setItem('refreshToken', session.refreshToken);

    let failed = false;
    http.get('/api/protected').subscribe({ error: () => { failed = true; } });

    httpMock.expectOne('/api/protected')
      .flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne(`${api}auth/refresh-token`)
      .flush({ isSuccess: false, message: 'Invalid refresh token.', data: null });

    httpMock.expectOne(`${api}auth/logout`).flush({ isSuccess: true, data: true });

    expect(failed).toBeTrue();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login'], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ sessionExpired: '1' })
    }));
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });

  it('never retries a 401 response more than once (no infinite loop)', () => {
    sessionStorage.setItem('accessToken', session.token);
    sessionStorage.setItem('refreshToken', session.refreshToken);

    let failures = 0;
    http.get('/api/protected').subscribe({ error: () => { failures++; } });

    httpMock.expectOne('/api/protected')
      .flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne(`${api}auth/refresh-token`).flush({ isSuccess: true, data: refreshedSession });

    const retried = httpMock.expectOne('/api/protected');
    expect(retried.request.headers.get('X-Auth-Retry')).toBe('true');
    retried.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne(`${api}auth/logout`).flush({ isSuccess: true, data: true });

    httpMock.expectNone(`${api}auth/refresh-token`);
    expect(failures).toBe(1);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login'], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ sessionExpired: '1' })
    }));
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });

  it('coalesces concurrent 401s into a single refresh request', () => {
    sessionStorage.setItem('accessToken', session.token);
    sessionStorage.setItem('refreshToken', session.refreshToken);

    let done = 0;
    http.get('/api/a').subscribe({ next: () => done++, error: () => fail('request a should succeed') });
    http.get('/api/b').subscribe({ next: () => done++, error: () => fail('request b should succeed') });

    httpMock.match((req) => req.url === '/api/a' || req.url === '/api/b').forEach(r =>
      r.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' }));

    httpMock.expectOne(`${api}auth/refresh-token`).flush({ isSuccess: true, data: refreshedSession });

    const retried = httpMock.match((req) => req.url === '/api/a' || req.url === '/api/b');
    expect(retried.length).toBe(2);
    retried.forEach(r => {
      expect(r.request.headers.get('X-Auth-Retry')).toBe('true');
      expect(r.request.headers.get('Authorization')).toBe(`Bearer ${refreshedSession.token}`);
      r.flush({ ok: true });
    });

    expect(done).toBe(2);
    httpMock.expectNone(`${api}auth/refresh-token`);
  });

  it('does not attempt a refresh for 401s from auth routes', () => {
    sessionStorage.setItem('accessToken', session.token);
    sessionStorage.setItem('refreshToken', session.refreshToken);

    let failed = false;
    http.post(`${api}auth/forgot-password`, { email: 'a@b.c' }).subscribe({ error: () => { failed = true; } });

    httpMock.expectOne(`${api}auth/forgot-password`)
      .flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBeTrue();
    httpMock.expectNone(`${api}auth/refresh-token`);
    httpMock.expectNone(`${api}auth/logout`);
  });
});