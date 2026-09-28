import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SignalRService } from './signalr.service';
import { AuthService } from './auth.service';

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        SignalRService,
        { provide: AuthService, useValue: { getToken: () => 'test-token', currentUser$: of(null) } }
      ]
    });
    service = TestBed.inject(SignalRService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should expose orderStatus$ starting as null', (done) => {
    let value: any = 'unset';
    service.orderStatus$.subscribe(v => value = v);
    expect(value).toBeNull();
    done();
  });

  it('should stop safely when no connection exists', () => {
    expect(() => service.stop()).not.toThrow();
  });

  it('should not throw when starting with a token available', () => {
    // start() attempts an async connection; guard only against synchronous errors.
    expect(() => service.start()).not.toThrow();
  });
});
