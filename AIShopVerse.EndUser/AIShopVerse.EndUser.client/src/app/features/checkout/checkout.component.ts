import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { OrderService } from '../../core/services/order.service';
import { PaymentService } from '../../core/services/payment.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">{{ 'checkout.title' | translate }}</h2>
      <form (ngSubmit)="onSubmit()" #checkoutForm="ngForm">
        <div class="mb-3">
          <label class="form-label" for="shippingAddress">{{ 'checkout.shippingAddress' | translate }}</label>
          <textarea id="shippingAddress" class="form-control" [(ngModel)]="shippingAddress" name="address"
                    rows="3" required
                    [attr.placeholder]="'checkout.shippingAddressPlaceholder' | translate"></textarea>
        </div>

        <div class="form-check mb-3">
          <input class="form-check-input" type="checkbox" id="sameAsShipping"
                 [(ngModel)]="sameAsShipping" name="same" (change)="toggleBilling()">
          <label class="form-check-label" for="sameAsShipping">{{ 'checkout.billingSameAsShipping' | translate }}</label>
        </div>

        <div class="mb-3" *ngIf="!sameAsShipping">
          <label class="form-label" for="billingAddress">{{ 'checkout.billingAddress' | translate }}</label>
          <textarea id="billingAddress" class="form-control" [(ngModel)]="billingAddress" name="billing"
                    rows="3" required
                    [attr.placeholder]="'checkout.billingAddressPlaceholder' | translate"></textarea>
        </div>

        <div class="mb-3">
          <span class="form-label d-block" id="paymentMethodLabel">{{ 'checkout.paymentMethod' | translate }}</span>
          <div class="form-check">
            <input class="form-check-input" type="radio" id="cash" value="CashOnDelivery"
                   [(ngModel)]="paymentMethod" name="payment">
            <label class="form-check-label" for="cash">{{ 'checkout.cashOnDelivery' | translate }}</label>
          </div>
          <div class="form-check" *ngIf="publishableKey">
            <input class="form-check-input" type="radio" id="card" value="CreditCard"
                   [(ngModel)]="paymentMethod" name="payment">
            <label class="form-check-label" for="card">{{ 'checkout.creditCard' | translate }}</label>
          </div>
        </div>

        <div class="mb-3" *ngIf="paymentMethod === 'CreditCard' && publishableKey">
          <label class="form-label d-block">{{ 'checkout.cardDetails' | translate }}</label>
          <div id="card-element" class="form-control stripe-card" dir="ltr"></div>
        </div>

        <div class="mb-3">
          <label class="form-label" for="couponCode">{{ 'checkout.couponOptional' | translate }}</label>
          <input id="couponCode" type="text" class="form-control" [(ngModel)]="couponCode" name="coupon">
        </div>

        <button type="submit" class="btn btn-primary" [disabled]="checkoutForm.invalid || submitting">
          {{ submitting ? ('checkout.processing' | translate) : ('checkout.placeOrder' | translate) }}
        </button>
      </form>
    </div>
  `,
  styles: [`
    .stripe-card { padding: 12px; }
  `]
})
export class CheckoutComponent {
  private translate = inject(TranslateService);
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
        this.toastr.error(this.translate.instant('checkout.cardUnavailable'), this.translate.instant('common.error'));
      } else {
        await this.submitCard(billing);
      }
    } catch (err: any) {
      this.toastr.error(err?.error?.message || err?.message || this.translate.instant('checkout.failed'), this.translate.instant('common.error'));
    } finally {
      this.submitting = false;
    }
  }

  private async submitCash(billing?: string) {
    const order = await firstValueFrom(this.orderService.checkout(
      this.shippingAddress,
      billing,
      this.couponCode || undefined,
      'CashOnDelivery'
    ));
    this.onSuccess(order);
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
      throw new Error(error.message || this.translate.instant('checkout.paymentDeclined'));
    }
    if (!paymentIntent) {
      throw new Error(this.translate.instant('checkout.paymentNotFinalized'));
    }
    const order = await firstValueFrom(this.paymentService.complete(res.orderId, paymentIntent.id));
    this.onSuccess(order || res);
  }

  private onSuccess(order: any) {
    const t = this.translate;
    const orderLabel = order && order.orderNumber
      ? t.instant('checkout.orderNumber') + ' ' + order.orderNumber + ' — ' + t.instant('checkout.orderPlaced')
      : t.instant('checkout.orderPlaced');
    this.toastr.success(orderLabel, t.instant('common.success'));
    const orderId = order?.id || order?.orderId;
    if (orderId) {
      this.router.navigateByUrl(`/orders/${orderId}`);
    } else {
      this.router.navigateByUrl('/orders');
    }
  }

  private async ensureCardElement(): Promise<Stripe> {
    if (this.stripe && this.cardElement) return this.stripe;
    this.stripe = await loadStripe(this.publishableKey);
    if (!this.stripe) throw new Error(this.translate.instant('checkout.stripeInitFailed'));
    this.cardElement = this.stripe.elements().create('card');
    this.cardElement.mount('#card-element');
    return this.stripe;
  }
}