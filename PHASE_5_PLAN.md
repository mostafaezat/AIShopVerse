# Phase 5 — Realtime Notifications & Engagement Hardening

## Goal

Close the realtime gap left in Phase 4 and harden the engagement surfaces: DB-persisted notifications delivered to the customer via **polling (source of truth) + best-effort SignalR push**, a notification bell with unread badge in the app header, product-card wishlist hearts, user-facing review management (delete own review, show reviewer name), admin **review moderation**, and **promotions toggle** (activate/deactivate a coupon).

## Decisions (user-confirmed)

- **DB-persisted notifications + polling is the source of truth**; SignalR is a best-effort accelerant. Cross-host push can't bridge without a backplane, so the bell always refreshes on a 30s poll and on any live `OrderStatusUpdate`, and never relies solely on push.
- **Wishlist on product cards is client-side**: product list fetches the user's wishlist id-set (same as product detail's `checkWishlist` pattern) and toggles hearts locally, instead of threading `isInWishlist` through the catalog query.
- **Admin review moderation** (`[Authorize(Roles="Admin,SuperAdmin")]`) and **coupon active toggle** land in the Admin client this phase.

## Current State (verified at planning time)

Phase 4 built checkout/orders/payments/SignalR, but SignalR `start()` was **never invoked** client-side (inert; only order-detail 10s polling worked). Coupon validation existed but admin could not toggle a coupon's active state, review moderation was absent, and ratings/catalog aggregates counted un-approved reviews.

## Work / Status

### Backend
- [x] `Notification` entity + `NotificationType` enum (`OrderStatus`/`Sale`/`Info`); `NotificationConfiguration`; `DbSet<Notification>` + `ApplicationUser.Notifications` nav. Table auto-created via `EnsureCreated()`.
- [x] `Application/Features/NotificationFeatures/` — `CreateNotificationCommand`, `MarkNotificationReadCommand`, `MarkAllNotificationsReadCommand`, `GetMyNotificationsQuery`, `GetUnreadNotificationsCountQuery`, `NotificationDto`.
- [x] Persisted `Notification` (Type=OrderStatus, `order.OrderNumber`/`order.Status`) inside the same commit in `UpdateOrderStatusCommand` + `CancelOrderCommand`.
- [x] EndUser `NotificationController` (`[Authorize]`): GET (onlyUnread/take), `UnreadCount`, `{id}/Read`, `ReadAll`.
- [x] Admin host `Program.cs`: SignalR `OnMessageReceived` `access_token` on `/notificationHub` + `app.MapHub`.
- [x] Reviews hardening: `GetProductReviewsQuery` fetches `ApplicationUser` names (`ReviewDto.UserName`); `GetAllProductsQuery` — `AverageRating`, `MinRating` filter, and `rating` sort count **only approved** reviews.
- [x] Admin moderation: `GetAllReviewsQuery` (paginated `AdminReviewDto`), `ModerateReviewCommand` (`ApproveReviewCommand`/`RejectReviewCommand`/`AdminDeleteReviewCommand`), Admin `ReviewController`.
- [x] Wishlist unique index `(UserId, ProductId)` in `WishlistConfiguration`; `IsInWishlistQuery`.
- [x] Promotions: `ToggleCouponCommand` + Admin `PromotionController.PUT Toggle/{id}`.

### Frontend (EndUser)
- [x] `core/services/notification.service.ts` — `getAll(onlyUnread?, take?)`, `getUnreadCount()`, `markRead(id)`, `markAllRead()`.
- [x] `signalr.service.ts` — auto-`start()`/`stop()` by subscribing to `auth.currentUser$` (fixes the inert-start gap); keeps `orderStatus$`.
- [x] `app.component.ts` — notification bell + unread badge + dropdown (latest 20) + **30s polling** when logged in; listens to `orderStatus$` to refresh unread; `markRead`/`markAllRead` wired; notification click navigates to order when `orderId` present.
- [x] `product-list.component` — wishlist heart badge on each card (fetches wishlist id-set; toggle add/remove; prompts login when anon).
- [x] `product-detail.component` — shows `review.userName`; "Delete" button on own reviews (via existing `deleteReview`); added `AuthService.currentUserId` getter.
- [x] `AuthService` — `currentUserId` getter.

### Frontend (AdminPanel)
- [x] `core/services/review.service.ts` — `getAll(page, size, approved?)`, `approve(id)`, `reject(id)`, `delete(id)`.
- [x] `features/reviews/review-list.component` — table + status filter (All/Approved/Pending) + pagination + Approve/Reject/Delete.
- [x] Promotions: `PromotionService.toggle(id)` + Activate/Deactivate button in `promotion-list.component`.
- [x] Route `/reviews` + sidebar nav link.

### Tests
- [x] EndUser specs: new `NotificationService`; updated `SignalRService` (mock `currentUser$: of(null)`), `ProductListComponent` (RouterTestingModule + WishlistService/AuthService mocks).
- [x] Admin client has no test target (no karma/`"test"` builder) — added `review.service.spec.ts` for when one is added; harmless, not executed by `ng build`.
- [x] Karma green: **30/30 SUCCESS** (ChromeHeadless + `CHROME_BIN`).

### Build gates
- [x] `dotnet build AIShopVerse.slnx` — 0 warnings / 0 errors.
- [x] EndUser `ng build` succeeds (only pre-existing initial-bundle budget warning).
- [x] Admin `ng build` succeeds.

## Known gaps / deferred
- **Cross-host/cross-process push** still requires a Redis/SignalR backplane (or a single shared hub) to be truly realtime across EndUser ↔ Admin; the 30s bell poll + per-event refresh is the source of truth.
- **Sale/Info notification types** are defined but not yet produced by any flow (only OrderStatus is emitted); future marketing/sale events can reuse `CreateNotificationCommand`.
- **PWA push notifications**, **notification in-app preferences/routing per type**, and **guest/anonymous carts** remain out of scope.
- **Admin inventory low-stock → notification** hook not yet wired.
