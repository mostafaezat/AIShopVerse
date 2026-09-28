# Phase 9 — Real Payment Gateway (Stripe)

## Goal

Replace the mock, synchronous "Credit Card" payment with a **real Stripe PaymentIntent** two-phase flow: server creates a payment intent → client collects card details via Stripe.js and confirms → order is finalized (Paid, stock decremented, cart cleared) after confirmation, with the **Stripe webhook as the authoritative completion path**. Add **admin-triggered real refunds** that call Stripe and mark the order/payment Refunded. When no Stripe keys are configured, checkout gracefully falls back to the existing mock flow so the app stays fully usable/testable offline.

## Decisions (user-confirmed)

- **Gateway**: Stripe, PaymentIntent + Elements (card form in the checkout page), not the hosted Checkout page.
- **Refunds**: admin-triggered **real** Stripe refunds (not record-only).
- **Config-driven with mock fallback**: if `Stripe:SecretKey`/`PublishableKey` are empty, `CreditCard` checkout degrades to the existing mock-complete behavior (no Stripe.js, no webhook).

## Current State (at planning time)

`CheckoutCommand` created an order and a `MOCK-*` `Payment` marked `Completed` **synchronously** regardless of `PaymentMethod`, clearing the cart and decrementing stock in the same transaction. There was a client `paymentPublicKey` env slot (empty) and a `Payment` entity with `PaymentStatus`/`PaymentMethod` enums (incl. `Refunded`), plus `OrderStatus.Refunded`. Dependency graph: Domain ← Infrastructure ← Application ← Server (so the Stripe service belongs in Infrastructure; Application may reference it).

## Work / Status

### Backend
- [x] `Stripe.net` 52.4.0 added to `Infrastructure`.
- [x] `Infrastructure/Services/Payments/`:
  - `StripeOptions` (`SecretKey`/`PublishableKey`/`WebhookSecret`, `IsEnabled`).
  - `IStripePaymentService` (create intent, verify succeeded, refund, webhook-read).
  - `StripePaymentService` (StripeClient-backed; `PaymentIntentCreateOptions` w/ metadata + `usd`; `RefundService.Create`; `EventUtility.ConstructEvent` signature-verified webhook read that returns the `PaymentIntent` id/status).
  - Registered via `Configure<StripeOptions>` + `AddScoped` in `InfrastructureDependencyInjection`.
- [x] `appsettings.json` added `Stripe` section (empty) on **both** EndUser and AdminPanel servers.
- [x] `Application/Services/PaymentCompletionService.cs` (`IPaymentCompletionService`) — idempotent finalizer: verifies payment succeeded (when Stripe enabled), marks order `Paid` + payment `Completed`/`PaidAt`, **decrements stock**, **clears cart**, **increments coupon usage**.
- [x] `Application/Features/PaymentFeatures/Commands/`:
  - `CreateCheckoutCommand` — validates cart/coupon, builds totals (reuses `IPricingService`), creates a **`Pending`** order + order-items; when Stripe enabled creates the PaymentIntent and a `Pending` payment linked to its id, returns `{ Mode="card", ClientSecret }`; when disabled performs the mock-complete path, returns `{ Mode="mock", Order }`.
  - `CompletePaymentCommand` (`OrderId`+`PaymentIntentId`) → `IPaymentCompletionService`.
  - `HandleStripeWebhookCommand` — signature-verified; handles `payment_intent.succeeded` (finalize), `payment_intent.payment_failed` (mark Failed + order Cancelled), `charge.refunded` (mark Refunded).
  - `RefundOrderCommand` — calls Stripe refund (when enabled + non-mock txn) then marks Payment `Refunded` + Order `Refunded`.
- [x] `EndUser/Servers/Controllers/PaymentController.cs` — `POST api/payment/CreateCheckout` & `Complete` (`[Authorize]`); `POST api/payment/Webhook` (`[AllowAnonymous]`, reads raw body + `Stripe-Signature`).
- [x] `AdminPanel/.../OrderController.cs` — added `POST api/order/Refund`.
- [x] `dotnet build AIShopVerse.slnx` → 0 warnings / 0 errors.

### Frontend (EndUser)
- [x] Installed `@stripe/stripe-js` (9.14.0); uses existing `environment.paymentPublicKey`.
- [x] `core/services/payment.service.ts` — `createCheckout()` → `CreateCheckoutResponse`; `complete(orderId, paymentIntentId)` → `Order`. (+ spec)
- [x] `features/checkout/checkout.component.ts` — **Cash on Delivery** keeps the existing `order.checkout` path; **Credit/Debit Card** now: `createCheckout` → (mock → success) or (card → mount Stripe `card` Element → `confirmCardPayment(clientSecret)` → `payment.complete`) → Toastr success + `/orders`. Card details only shown when a publishable key is configured; otherwise a "mock card" note + fallback. `onSubmit` is now async.
- [x] Rewrote `checkout.component.spec.ts` for the two-phase flow (COD + mock-card + error paths; mocks `PaymentService`/`OrderService`/`ToastrService`).

### Frontend (AdminPanel)
- [x] `core/services/order.service.ts` — added `refund(orderId)` → `POST order/Refund`.
- [x] `features/orders-management/order-list.component.ts` — added **Refund** button (visible unless Refunded/Cancelled) with `confirm()` + `alert()` feedback, and `Refunded` (6) in the status/filter dropdowns and status map. Component-local `.btn-refund` style.

### Tests / gates
- [x] `dotnet build AIShopVerse.slnx` — 0/0.
- [x] EndUser `ng build` — success (pre-existing `.form-floating>~label` selector warning only).
- [x] EndUser Karma — **43/43 SUCCESS** (added `PaymentService` spec; rewrote `CheckoutComponent` spec). Pre-existing `img/a`,`img/b` 404 warnings only.
- [x] AdminPanel `ng build --configuration production` — success (no warnings).

## How to go live (config)
1. Sign up at Stripe, add a payment method, get test **Publishable** + **Secret** keys.
2. Set `Stripe:SecretKey` and `Stripe:PublishableKey` in `EndUser.Server/appsettings.json`; mirror keys in `AdminPanel.Server/appsettings.json` (for refunds).
3. Set the **public** key in `EndUser.Client/src/environments/environment.ts` + `.prod.ts` (`paymentPublicKey`).
4. For webhooks: create a webhook endpoint for `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded` pointing at `https://<host>/api/payment/Webhook`, and put its **webhook signing secret** in `Stripe:WebhookSecret`. Use the Stripe CLI (`stripe listen --forward-to https://localhost:7125/api/payment/Webhook`) for local testing.
5. Test with Stripe test cards (4242 4242 4242 4242).

## Known gaps / deferred
- **`CheckoutCommand` ($/order/Checkout) still accepts `CreditCard`** and would create a mock payment if called directly with that method. The new client routes cards via `payment/create-checkout`, but this endpoint wasn't tightened — could reject `CreditCard` (or is intentionally the COD path).
- **No pending-order cleanup/expiry**: a card checkout that is never paid leaves a `Pending` order + `Pending` payment with stock untouched and cart intact. Could add a scheduled cleanup / intent-expiry handling later.
- **Single-currency assumption** (`usd`), **no SCA/async-return-url integration** beyond what `confirmCardPayment` handles, **no payment-failure retry UI beyond showing the error**.
- **Webhook and confirm are both finalizers** and share `IPaymentCompletionService` (idempotent) so ordering (webhook first vs confirm first) is safe — but there is no dedup/marker beyond the `Paid` status check.
- **No live E2E run** was possible in this environment (no real keys); verified via build + unit tests + mock fallback path.
