import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';
import { ToastrService } from 'ngx-toastr';
import { OrderService } from '../../core/services/order.service';
import { PaymentService } from '../../core/services/payment.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">Checkout</h2>
      <form (ngSubmit)="onSubmit()" #checkoutForm="ngForm">
        <div class="mb-3">
          <label class="form-label">Shipping Address</label>
          <textarea class="form-control" [(ngModel)]="shippingAddress" name="address"
                    rows="3" required></textarea>
        </div>

        <div class="form-check mb-3">
          <input class="form-check-input" type="checkbox" id="sameAsShipping"
                 [(ngModel)]="sameAsShipping" name="same" (change)="toggleBilling()">
          <label class="form-check-label" for="sameAsShipping">Billing address same as shipping</label>
        </div>

        <div class="mb-3" *ngIf="!sameAsShipping">
          <label class="form-label">Billing Address</label>
          <textarea class="form-control" [(ngModel)]="billingAddress" name="billing"
                    rows="3" required></textarea>
        </div>

        <div class="mb-3">
          <label class="form-label">Payment Method</label>
          <div class="form-check">
            <input class="form-check-input" type="radio" id="cash" value="CashOnDelivery"
                   [(ngModel)]="paymentMethod" name="payment">
            <label class="form-check-label" for="cash">Cash on Delivery</label>
          </div>
          <div class="form-check" *ngIf="publishableKey">
            <input class="form-check-input" type="radio" id="card" value="CreditCard"
                   [(ngModel)]="paymentMethod" name="payment">
            <label class="form-check-label" for="card">Credit / Debit Card</label>
          </div>
        </div>

        <div class="mb-3" *ngIf="paymentMethod === 'CreditCard' && publishableKey">
          <label class="form-label d-block">Card Details</label>
          <div id="card-element" class="form-control stripe-card"></div>
        </div>

        <div class="mb-3">
          <label class="form-label">Coupon Code (optional)</label>
          <input type="text" class="form-control" [(ngModel)]="couponCode" name="coupon">
        </div>

        <button type="submit" class="btn btn-primary" [disabled]="checkoutForm.invalid || submitting">
          {{ submitting ? 'Processing...' : 'Place Order' }}
        </button>
      </form>
    </div>
  `,
  styles: [`
    .stripe-card { padding: 12px; }
  `]
})
export class CheckoutComponent {
  shippingAddress = '';
  billingAddress = '';
  sameAsShipping = true;
  paymentMethod = 'CashOnDelivery';
  couponCode = '';
  submitting = false;

  private stripe: Stripe | null = null;
  private cardElement: StripeCardElement | null = null;

  constructor(
    private orderService: OrderService,
    private paymentService: PaymentService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  get publishableKey(): string {
    return this.paymentService.publishableKey;
  }

  toggleBilling() {
    if (this.sameAsShipping) this.billingAddress = '';
  }

  async onSubmit() {
    this.submitting = true;
    try {
      const billing = this.sameAsShipping ? undefined : this.billingAddress;
      if (this.paymentMethod === 'CashOnDelivery') {
        await this.submitCash(billing);
      } else if (!this.publishableKey) {
        this.toastr.error('Card payments are currently unavailable. Please use Cash on Delivery.', 'Error');
      } else {
        await this.submitCard(billing);
      }
    } catch (err: any) {
      this.toastr.error(err?.message || 'Checkout failed', 'Error');
    } finally {
      this.submitting = false;
    }
  }

  private async submitCash(billing?: string) {
    await firstValueFrom(this.orderService.checkout(
      this.shippingAddress,
      billing,
      this.couponCode || undefined,
      'CashOnDelivery'
    ));
    this.onSuccess();
  }

  private async submitCard(billing?: string) {
    const res = await firstValueFrom(this.paymentService.createCheckout({
      shippingAddress: this.shippingAddress,
      billingAddress: billing,
      couponCode: this.couponCode || undefined
    }));

    const stripe = await this.ensureCardElement();
    const { error, paymentIntent } = await stripe.confirmCardPayment(res.clientSecret!, {
      payment_method: { card: this.cardElement! }
    });
    if (error) {
      throw new Error(error.message || 'Card payment declined.');
    }
    if (!paymentIntent) {
      throw new Error('Payment not finalized.');
    }
    await firstValueFrom(this.paymentService.complete(res.orderId, paymentIntent.id));
    this.onSuccess();
  }

  private async ensureCardElement(): Promise<Stripe> {
    if (this.stripe && this.cardElement) return this.stripe;
    this.stripe = await loadStripe(this.publishableKey);
    if (!this.stripe) throw new Error('Stripe failed to initialize.');
    this.cardElement = this.stripe.elements().create('card');
    this.cardElement.mount('#card-element');
    return this.stripe;
  }

  private onSuccess() {
    this.toastr.success('Order placed!', 'Success');
    this.router.navigateByUrl('/orders');
  }
}
