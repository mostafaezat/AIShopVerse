# Phase 10 — Realtime Backplane + Alerts & Payments Polish (Phase 9 gaps)

## Goal
Make the app truly realtime across both hosts (EndUser :7125 and AdminPanel :7377 are separate
processes) with low-stock/back-in-stock alerts, wire the currently-unused `Sale`/`Info` notification
types, and close the Phase 9 payment gaps (reject CreditCard on the old COD checkout, pending-order
expiry, payment-failure notifications).

## Key constraint
No Docker / WSL / Redis on the dev machine, so a live Redis SignalR backplane cannot be run or
E2E-tested locally. Approach:

- **Production path (config-gated):** when `Redis:Configuration` is set, `AddSignalR().AddStackExchangeRedis(...)` is used so both hosts share one hub fabric.
- **Local fallback:** each push is written to a SQL **outbox** (`RealtimeEvent` table) and each host
  runs a `RealtimeRelayHostedService` that relays events destined for it to its own hub clients.
  This gives real cross-host realtime locally with no polling, and becomes redundant (but harmless)
  when Redis is on.

## Backend changes (Domain / Infrastructure / Application / both servers)

### Domain
- `CatalogEntities/Product.cs`: add `int? LowStockThreshold`.
- New `RealtimeEvent` entity (schema `Sales`):
  `Id`, `Kind` (`OrderStatus|NewOrder|LowStock|BackInStock|Info`), `TargetUserId?`,
  `PayloadJson`, `Source` (host that produced it), `Consumer` (host that should relay),
  `CreatedAt`, `ProcessedAt?`.
- `DbContext`: add `DbSet<RealtimeEvent>` (needed because `EnsureCreated` builds tables from DbSets).
  NOTE: `EnsureCreated` does NOT alter existing tables -> adding `Product.LowStockThreshold` (a new
  column) requires the dev `AIShopVerse` DB to be dropped/recreated once.

### Infrastructure
- `Infrastructure.csproj`: add `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.
- `InfrastructureDependencyInjection.cs`: gate SignalR on `Redis:Configuration`; register
  `IOptions<InventoryOptions>`; register `IRealtimeNotifier`, hosted services.
- `Signalr/NotificationHub.cs`: add `OnConnectedAsync`/`OnDisconnectedAsync` (wire
  `ConnectedUserTracker`) and join role-based `"admins"` group for Admin/SuperAdmin users.
- New `Services/Realtime/SignalRRealtimeNotifier.cs` (implements `IRealtimeNotifier`): writes outbox
  row (when Redis off) then pushes locally via `IHubContext`.
- New `Services/Realtime/RealtimeRelayHostedService.cs`: polls unprocessed outbox rows whose
  `Consumer == thisHost && ProcessedAt == null`, pushes to local hub, marks processed.
- New `Services/Realtime/RealtimeEventKind.cs` enum helper.

### Application
- New `Services/IRealtimeNotifier.cs` + `RealtimeEventKind`.
- New `Features/InventoryFeatures/Commands/` low-stock support:
  - `ILowStockAlerter`/`LowStockAlerter` service to evaluate and emit low-stock / back-in-stock
    events (writes `RealtimeEvent` to `"admins"` group + admin `Notification`).
  - `InventoryOptions` (`LowStockThreshold`, default 10).
- Update stock-mutating paths to call the alerter:
  - `Services/PaymentCompletionService.DecrementStockAsync`
  - `ProductFeatures/Commands/AdjustStockCommand`
  - `OrderFeatures/Commands/CheckoutCommand` (decrement at creation — COD)
  - `PaymentFeatures/Commands/CreateCheckoutCommand.ApplyOrderSideEffectsAsync` (mock path)
  - `OrderFeatures/Commands/CancelOrderCommand` (stock restore -> back-in-stock)
- Update `UpdateOrderStatusCommand` / `CancelOrderCommand` to push via `IRealtimeNotifier`.
- `PaymentFeatures/Commands/CheckoutCommand`: reject `PaymentMethod.CreditCard` (fail) instead of
  mock-completing; COD unchanged.
- `PaymentFeatures/Commands/CreateCheckoutCommand` + `CompletePaymentCommand`: emit `NewOrder`
  realtime event to `"admins"` group; on card failure emit user notification + realtime push.
- `HandleStripeWebhookCommand`: emit user notification + realtime push on failed/refunded.
- `ProductFeatures/Commands/AddProductCommand`/`UpdateProductCommand`: add optional
  `LowStockThreshold`.
- New `NotificationsService` wrapping `CreateNotificationCommand` + `IRealtimeNotifier`.

### Server apps
- EndUser `Program.cs`: register hosted services (`RealtimeRelayHostedService`,
  `ExpiredPendingOrdersHostedService`). `appsettings.json`: add optional `Redis`, `Inventory`,
  `Payments:PendingOrderTimeoutMinutes` sections.
- AdminPanel `Program.cs`: register `RealtimeRelayHostedService`. `appsettings.json`: same sections.
- New AdminPanel `Controllers/NotificationController.cs`: broadcast `Sale`/`Info` notifications to
  users (makes orphaned `CreateNotificationCommand` reachable).
- New `ExpiredPendingOrdersHostedService` (EndUser): cancel `Pending` orders older than
  `Payments:PendingOrderTimeoutMinutes` (default 30). Restore stock only where reserved at creation
  (COD/mock). Cards don't reserve -> just cancel/stale them (confirmed decision).

## Frontend changes

### AdminPanel client
- `package.json`: add `@microsoft/signalr`.
- New `core/services/signalr.service.ts` (connect to `environment.signalRUrl` + `/notificationHub`,
  `access_token` query, auto start/stop on auth).
- `app.component.ts`: on realtime events show a toast/alert (low stock, new order); keep it simple
  (plain HTML, matches admin panel style).
- `inventory.component.ts` / `dashboard.component.ts`: auto-refresh on `NewOrder`/`LowStock` events.
- `features/products/product-list.component.ts`: allow setting per-product `LowStockThreshold`.

### EndUser client
- `core/services/signalr.service.ts`: add `NewOrder`/`Info` handlers if any apply to end users
  (back-in-stock, info notifications) -> refresh notification bell. Existing polling kept as fallback.

## Gates
- `dotnet build` (0 errors / 0 warnings) from repo root.
- EndUser: `ng build` + Karma tests green (update affected specs).
- AdminPanel: `ng build` green.
- Manual: admin order-status change reaches EndUser bell/list near-real-time (outbox relay);
  low-stock alert appears on admin on stock drop; CreditCard via old `/order/Checkout` rejected;
  pending orders auto-expire.

## Notes
- Redis go-live: set `Redis:Configuration` (e.g. `localhost:6379`) in BOTH server appsettings, then
  install/run a Redis server (Docker: `docker run -p 6379:6379 redis`). No code change needed.
- Dev DB: `AIShopVerse` did NOT exist on the local SQL instance at Phase 10 build time, so
  `EnsureCreated()` on next startup builds the full new schema automatically, including the
  `Products.LowStockThreshold` column and the new `RealtimeEvent` table. No manual migration/drop
  required. If you already have an existing `AIShopVerse` DB from earlier phases, drop it once so
  `EnsureCreated` recreates it with the new column + table (seed data re-populates on startup).
