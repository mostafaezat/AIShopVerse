import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PaymentService } from './payment.service';
import { environment } from '../../../environments/environment';

describe('PaymentService', () => {
  let service: PaymentService;
  let httpMock: HttpTestingController;
  const api = environment.apiEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PaymentService]
    });
    service = TestBed.inject(PaymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('createCheckout posts body and returns data', () => {
    const payload = { mode: 'card', orderId: 'o1', paymentIntentId: 'pi_1', clientSecret: 'cs_1' };
    service.createCheckout({ shippingAddress: 'Main St', couponCode: 'SAVE' }).subscribe(res => {
      expect(res.mode).toBe('card');
      expect(res.paymentIntentId).toBe('pi_1');
    });
    const req = httpMock.expectOne(`${api}payment/CreateCheckout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ shippingAddress: 'Main St', couponCode: 'SAVE' });
    req.flush({ data: payload });
  });

  it('complete posts orderId and paymentIntentId and returns order', () => {
    const payload = { id: 'o1', orderNumber: 'ORD-1', status: 'Paid', subtotal: 10, tax: 1.5, shippingCost: 0, total: 11.5, items: [] };
    service.complete('o1', 'pi_1').subscribe(res => {
      expect(res.id).toBe('o1');
    });
    const req = httpMock.expectOne(`${api}payment/Complete`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ orderId: 'o1', paymentIntentId: 'pi_1' });
    req.flush({ data: payload });
  });
});
