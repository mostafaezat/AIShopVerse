import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { CheckoutComponent } from './checkout.component';
import { OrderService } from '../../core/services/order.service';
import { PaymentService } from '../../core/services/payment.service';
import { ToastrService } from 'ngx-toastr';
import { Order } from '../../core/models';

describe('CheckoutComponent', () => {
  let component: CheckoutComponent;
  let fixture: ComponentFixture<CheckoutComponent>;
  let orderServiceStub: any;
  let paymentServiceStub: any;
  let toastrStub: any;
  let routerStub: any;

  const mockOrder: Order = {
    id: 'o1',
    orderNumber: 'ORD-20260828-ABC12345',
    subtotal: 100,
    discountAmount: 0,
    tax: 15,
    shippingCost: 10,
    total: 125,
    status: 'Paid' as any,
    shippingAddress: 'Test St',
    items: []
  };

  beforeEach(async () => {
    orderServiceStub = jasmine.createSpyObj('OrderService', ['checkout']);
    orderServiceStub.checkout.and.returnValue(of(mockOrder));
    paymentServiceStub = jasmine.createSpyObj('PaymentService', ['createCheckout', 'complete']);
    paymentServiceStub.publishableKey = '';
    toastrStub = jasmine.createSpyObj('ToastrService', ['success', 'error']);
    routerStub = { navigateByUrl: jasmine.createSpy('navigateByUrl') };

    await TestBed.configureTestingModule({
      imports: [CheckoutComponent, HttpClientTestingModule, FormsModule],
      providers: [
        { provide: OrderService, useValue: orderServiceStub },
        { provide: PaymentService, useValue: paymentServiceStub },
        { provide: Router, useValue: routerStub },
        { provide: ToastrService, useValue: toastrStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CheckoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should default payment method to Cash on Delivery', () => {
    expect(component.paymentMethod).toBe('CashOnDelivery');
  });

  it('should submit cash on delivery with billing address and coupon', async () => {
    component.shippingAddress = '123 Main St';
    component.billingAddress = 'Billing Ave';
    component.sameAsShipping = false;
    component.paymentMethod = 'CashOnDelivery';
    component.couponCode = 'SAVE10';

    await component.onSubmit();

    expect(orderServiceStub.checkout).toHaveBeenCalledWith(
      '123 Main St', 'Billing Ave', 'SAVE10', 'CashOnDelivery'
    );
    expect(routerStub.navigateByUrl).toHaveBeenCalledWith('/orders');
  });

  it('should omit billing address on cash path when same as shipping', async () => {
    component.shippingAddress = 'Main St';
    component.sameAsShipping = true;
    await component.onSubmit();
    const args = orderServiceStub.checkout.calls.mostRecent().args;
    expect(args[1]).toBeUndefined();
    expect(args[2]).toBeUndefined();
  });

  it('should hide the card option when Stripe is unavailable', () => {
    expect(component.publishableKey).toBe('');
    fixture.detectChanges();
    const cardRadio = fixture.debugElement.nativeElement.querySelector('input[value="CreditCard"]');
    expect(cardRadio).toBeNull();

    paymentServiceStub.publishableKey = 'pk_test_123';
    fixture.detectChanges();
    const shown = fixture.debugElement.nativeElement.querySelector('input[value="CreditCard"]');
    expect(shown).not.toBeNull();
  });

  it('should reject card payment without a Stripe key', async () => {
    component.shippingAddress = 'Card St';
    component.paymentMethod = 'CreditCard';

    await component.onSubmit();

    expect(paymentServiceStub.createCheckout).not.toHaveBeenCalled();
    expect(toastrStub.error).toHaveBeenCalledWith(
      'Card payments are currently unavailable. Please use Cash on Delivery.',
      'Error'
    );
    expect(routerStub.navigateByUrl).not.toHaveBeenCalled();
    expect(component.submitting).toBe(false);
  });

  it('should surface an error on card checkout failure', async () => {
    paymentServiceStub.publishableKey = 'pk_test_123';
    paymentServiceStub.createCheckout.and.returnValue(
      throwError(() => ({ message: 'Cart is empty.' }))
    );
    component.shippingAddress = 'Card St';
    component.paymentMethod = 'CreditCard';
    await component.onSubmit();

    expect(toastrStub.error).toHaveBeenCalledWith('Cart is empty.', 'Error');
    expect(component.submitting).toBe(false);
  });

  it('should surface an error on cash checkout failure', async () => {
    orderServiceStub.checkout.and.returnValue(
      throwError(() => ({ error: { message: 'Cart is empty.' } }))
    );
    await component.onSubmit();

    expect(toastrStub.error).toHaveBeenCalled();
    expect(component.submitting).toBe(false);
  });
});
