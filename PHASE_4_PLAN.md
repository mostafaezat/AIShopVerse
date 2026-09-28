# Phase 4 — Cart, Checkout, Orders, Payments, Live Status

## Goal

Deliver the transactional vertical: a persisted shopping cart, coupon discounts, checkout (billing address + payment method), order creation/payment, order history & detail, and live order-status updates via SignalR (admin advances status → customer sees it in near-real time).

## Decisions (user-confirmed)

- **Payments = mock / cash checkout.** Order is marked `Paid` immediately at checkout; `Payment` is created `Completed` with a `MOCK-*` `TransactionId`. No external gateway.
- **Coupon validation implemented in full now** (pulled forward from Phase 5): active flag, date-range (`ValidFrom`/`ValidTo`), max-uses, min-order, %/fixed discount.
- **Coupon discount applied in BOTH cart and checkout** via shared `IPricingService`.
- **SignalR**: backend emits status updates; EndUser subscribes for live updates. Since admin and customer run on **different hosts** (separate processes, no backplane), cross-host push can't bridge — so order detail also falls back to a light 10s polling loop for cross-host changes.

## Current State (verified at planning time)

Backend Phase 4 was ~75% built already: cart/order/payment entities, CQRS handlers, and controllers existed. Frontend cart/checkout/order components were minimal (checkout used raw `HttpClient`, no billing/payment/coupon UI, no live updates).

## Work / Status

### Backend
- [x] `Application/Services/IPricingService.cs` — `PricingService.ComputeTotals(subtotal, discount)` (15% tax, free shipping >100 else 10) and `ValidateAndApplyCouponAsync(...)` (full validation → `CouponValidationResult`). Registered `AddScoped<IPricingService, PricingService>()`.
- [x] `CheckoutCommand` — injects `IPricingService`, adds `PaymentMethod` input (enum), validates coupon, marks order `Paid`, creates `Payment` `Completed` with `MOCK-*` `TransactionId`, increments `coupon.UsedCount`.
- [x] `PaymentMethod` enum (`CashOnDelivery`, `CreditCard`) on `Payment.cs`.
- [x] `GetCartQuery` — `CouponCode` property; applies coupon discount; persists `cart.CouponCode`; `CartDto` gained `DiscountAmount`.
- [x] `Application/Features/CartFeatures/Commands/ApplyCartCouponCommand.cs` + `CartController.POST Coupon` endpoint — validates & persists/replaces the cart coupon, returns recomputed `CartDto`.
- [x] SignalR wiring — `UpdateOrderStatusCommand` + `CancelOrderCommand` emit `SendAsync("OrderStatusUpdate", { orderId, status })`; `AddSignalR()` on Admin host; hub mapped at `/notificationHub` on both hosts.
- [x] `[EnableRateLimiting("checkout")]` (10/min) on `POST api/order/Checkout`.
- [x] Ownership check in `RemoveCartItemCommand` (loads cart item + its cart, verifies `UserId`).

### Frontend (EndUser)
- [x] `core/services/signalr.service.ts` — connects to `signalRUrl + "notificationHub"` with `access_token` JWT; exposes `orderStatus$`.
- [x] `order-detail.component` — subscribes to SignalR `OrderStatusUpdate` (matches `orderId`) + 10s polling fallback for cross-host admin changes; status badge + discount row.
- [x] `cart.component` — coupon apply/remove field, shows discount line; `CartService.applyCoupon/removeCoupon` added; `CartDto.discountAmount`.
- [x] `checkout.component` — billing address (toggle "same as shipping"), payment method radio (cash/card), coupon; refactored off raw `HttpClient` to `OrderService.checkout(shipping, billing, coupon, paymentMethod)`.
- [x] `core/models` — `CheckoutRequest.paymentMethod`, `CartDto.discountAmount`.

### Tests
- [x] Angular `.spec.ts`: `CheckoutComponent`, `CartService`, `SignalRService` (+ Phase 3 specs).
- [x] Karma green (25/25) via ChromeHeadless with `CHROME_BIN`.
- [x] Optional xUnit for pricing/coupon logic — deferred.

### Build / docs gate
- [x] `dotnet build` succeeds (0 warnings / 0 errors).
- [x] EndUser `ng build` succeeds.
- [x] Admin `ng build` succeeds.

## Known gaps / deferred
- **Cross-host live status**: admin-side status changes won't push to the customer hub across processes (no backplane); covered by the polling fallback in order detail (10s). A Redis/SignalR backplane or moving to a shared hub would enable true push.
- **Guest/anonymous carts** and **real payment gateway** are out of scope for Phase 4 (mock `MOCK-*` payments; guest carts deferred to a later phase).
- **Order invoice / email receipt** not yet generated.
