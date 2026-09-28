import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CartService } from './cart.service';
import { environment } from '../../../environments/environment';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  const cartResponse = { data: {
    id: 'c1',
    items: [],
    subtotal: 100,
    discountAmount: 10,
    tax: 13.5,
    shippingCost: 10,
    total: 113.5,
    couponCode: 'SAVE10'
  }};

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CartService]
    });
    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should apply a coupon through the cart coupon endpoint', () => {
    service.applyCoupon('SAVE10').subscribe(cart => {
      expect(cart.couponCode).toBe('SAVE10');
      expect(cart.discountAmount).toBe(10);
    });
    const req = httpMock.expectOne(`${environment.apiEndpoint}cart/coupon`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.couponCode).toBe('SAVE10');
    req.flush(cartResponse);
  });

  it('should remove a coupon by posting an empty code', () => {
    const cleared = JSON.parse(JSON.stringify(cartResponse));
    cleared.data.couponCode = null;
    cleared.data.discountAmount = 0;
    service.removeCoupon().subscribe(cart => {
      expect(cart.couponCode).toBeNull();
    });
    const req = httpMock.expectOne(`${environment.apiEndpoint}cart/coupon`);
    expect(req.request.body.couponCode).toBe('');
    req.flush(cleared);
  });

  it('should expose the latest cart through cart$', () => {
    let latest: any;
    service.cart$.subscribe(c => latest = c);
    service.getCart().subscribe();
    const req = httpMock.expectOne(`${environment.apiEndpoint}cart`);
    req.flush(cartResponse);
    expect(latest?.couponCode).toBe('SAVE10');
  });
});
