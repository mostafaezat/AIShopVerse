# AIShopVerse — Missing Features Implementation Plan & Release-Readiness Tracker

This file is the single source of truth for the P0/P1 implementation work. Update it after every phase.
`PROJECT_BLUEPRINT.md` remains the architectural blueprint. Do not delete or rewrite requirements from this file;
append status/verification history and track literally.

Status markers:

- `[ ]` Not Started
- `[~]` In Progress
- `[x]` Completed
- `[!]` Blocked

---

## Current Progress

Current Phase: P1

Current Feature: P1-8 — UX State Pass

Status: [x] Completed (2026-09-29)

Last Completed Feature: P1-8 (UX State Pass)

Next Feature: P1 list complete (see P1 list below)

Last Verification (P1-8): `dotnet build AIShopVerse.slnx` 0 warnings / 0 errors; `dotnet test` in Application.Tests 71/71 PASS; AdminPanel karma 25/25 + EndUser karma 68/68; `scripts\scan-secrets.ps1` exit 0

Known Blockers: none

Next Recommended Action: Run the Mandatory Verification Before Release (npm builds for both SPAs, migration, E2E smoke).

---

## Bugs Discovered & Fixed (out-of-band)

### BUG-1: JWT runtime crash caused by AutoMapper 16.1.1 (fixed 2026-09-24)
- **Symptom**: `TypeInitializationException: Could not load type 'Microsoft.IdentityModel.Json.JsonConvert' from assembly 'Microsoft.IdentityModel.Tokens, Version=8.14.0.0'` on ANY JWT write (login/refresh) — latent crash affecting both hosts at runtime, surfaced by the new `Application.Tests` refresh-token flow tests.
- **Root cause**: `AutoMapper 16.1.1` transitively requires `Microsoft.IdentityModel.JsonWebTokens >= 8.14.0`, which upgraded `Microsoft.IdentityModel.Tokens` to 8.14.0 while `System.IdentityModel.Tokens.Jwt` stayed at 7.1.2 (from `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11`). The 7.1.2 Jwt handler compiles against `JsonConvert` inside the old Tokens assembly, which no longer exists in 8.14 — every JWT write threw.
- **Fix**: Removed the `AutoMapper 16.1.1` PackageReference from `Application/Application.csproj` (AutoMapper is unused anywhere in the code base). The whole IdentityModel stack now resolves consistently at 7.1.2. Verified: `dotnet test Application.Tests` 6/6 PASS, `dotnet build AIShopVerse.slnx` 0 warnings / 0 errors.

### BUG-2: StripePaymentService crash with empty Stripe keys (fixed 2026-09-24, found during P0-4)
- **Symptom**: `System.ArgumentException: API key cannot be the empty string.` from `StripeClient` ctor whenever `IStripePaymentService` was resolved in a scope with no Stripe keys configured — the card checkout endpoint 500'd before `CreateCheckoutCommandHandler` could return its clean "card unavailable" rejection.
- **Root cause**: `StripePaymentService` built `new StripeClient(_options.SecretKey)` eagerly in its constructor, and the registration is unconditional (`AddScoped<IStripePaymentService, StripePaymentService>()`), so empty `SecretKey` threw at DI resolution.
- **Fix**: `StripePaymentService` now creates the client lazily (`_options.IsEnabled ? new StripeClient(_options.SecretKey) : null`) and the three client-using methods (`CreatePaymentIntentAsync`, `IsPaymentSucceededAsync`, `RefundAsync`) guard with `throw new InvalidOperationException("Stripe is not configured.")`. Handlers reject before reaching the service when `IsEnabled` is false.

---

# Confirmed Implementation Decisions (business)

| # | Decision | Value |
|---|----------|-------|
| 1 | Payment mode (P0-4) | COD-only MVP. `Payment:EnabledMethod=COD`. Stripe stays wired but runtime-disabled; card attempts rejected clearly; card UI hidden; never auto-Paid. |
| 2 | Variant model (P0-6) | `Size`/`Color` columns on `ProductVariant`, unique `(ProductId, Size, Color)` + unique SKU; nullable `VariantId` on `CartItem`/`OrderItem`; product-level Price/StockQuantity remains as fallback for variant-less products. |
| 3 | Backend test DB | `Application.Tests` (xUnit) against dedicated SQL Server DB `AIShopVerse.Tests` on the local instance (connection override via env `AISHOPVERSE_TEST_CONNECTION`). |
| 4 | E2E verification | HTTP-level smoke against both running servers + seed/schema checks via `sqlcmd` + image URL resolution. |
| 5 | Release config (P0-7) | Env-driven placeholders (`AISHOPVERSE_JWT_KEY`, `Cors:AllowedOrigins`, admin seed password env in Production); Production fails fast on missing secrets; dev-only values in `appsettings.Development.json`; no real secrets committed. |

---

# Verified Environment Facts (this machine)

- .NET SDK 10.0.401 + ASP.NET Core runtime 8.0.31 + 10.0.12 (slnx targets net8.0)
- Node v24.13.0, npm 11.6.2
- `dotnet ef` 10.0.8 installed globally
- SQL Server 16.0.1200.5 (`MSSQLSERVER` service Running, trusted connection via `Server=.` OK) — used for migration + integration tests
- Chrome present → Karma `ChromeHeadless` tests runnable in both SPAs
- Repository is NOT a git repo; there is no `.github` CI yet (item under P0-7)

---

# P0-1 — Fix Session Persistence / Login / Register

Current assessment:

* Backend response is correct (`Result<T>` envelope: `isSuccess` / `message` / `data`; `AuthenticationController` returns `Ok(Result)` always, HTTP 200 even for invalid credentials).
* Frontends check `response.success` (wrong) and even navigate on failure (`login.component.ts` / `register.component.ts`). Session is not persisted reliably because `setSession` never runs.

Implement:

1. In the shared/authentication logic of both SPAs:
   * Correctly handle the API envelope.
   * Check `isSuccess`.
   * Unwrap `data`.
2. Ensure login error handling explicitly handles:
   * `isSuccess === false`
   * validation/API errors
   * invalid credentials
3. Persist after successful login:
   * access token
   * refresh token if returned
   * token expiry
   * authenticated user information (new `authUser` sessionStorage key; JWT-decode fallback)
4. Ensure page refresh restores the authenticated session (`loadStoredUser`).
5. Ensure logout clears all authentication state (incl. `authUser`) and best-effort server-side refresh-token revocation via `auth/logout { refreshToken }`.
6. Apply the fix consistently to both EndUser and Admin applications.

Tests:

- Auth service unit tests.
- Successful login.
- Invalid credentials (`isSuccess=false`).
- API-level failure response (HTTP error).
- Page refresh/session restoration.
- Logout.

Do not change the backend contract unless inspection proves it is incorrect (inspection shows it is correct).

Status: [x] Completed

Implementation date: 2026-09-24

Files:
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/src/app/core/services/auth.service.ts`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/src/app/features/auth/login.component.ts`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/src/app/features/auth/register.component.ts`
- `AIShopVerse.AdminPanel/AIShopVerse.adminpanel.client/src/app/core/services/auth.service.ts`
- `AIShopVerse.AdminPanel/AIShopVerse.adminpanel.client/src/app/features/auth/login.component.ts`

Database/migration changes: none.

Tests added:
- `auth.service.spec.ts` in both SPAs (login success / invalid creds / API-level failure / session restore / JWT legacy restore / expired session / logout / register)

Verification result:
- EndUser `ng test` (ChromeHeadless): 54/54 PASS (incl. 9 new auth tests).
- Admin `ng test` (ChromeHeadless): 12/12 PASS (incl. 7 new auth tests).
- EndUser `npm run build`: PASS (only pre-existing `.form-floating>~label` CSS selector warning).
- Admin `npm run build`: PASS (no warnings).
- E2E login smoke on live servers: pending — scheduled at the final mandatory E2E milestone (verification item 6).

Notes:
- Backend contract verified correct (no backend change needed): `AuthenticationController` returns `Ok(Result<T>)` always; envelope is `isSuccess`/`message`/`data`. `Single()` never maps failures to non-200.
- Login/register component `next` handlers now branch on `res.isSuccess` (invalid credentials arrive as HTTP 200 `isSuccess:false`) instead of navigating unconditionally.
- Fixed incidentally: `loadStoredUser` JWT decode produced `roles:[role]` for an array claim; `authUser` JSON persistence restores exact roles (fixes Admin `isSuperAdmin()` after refresh).
- Logout now does best-effort server-side refresh-token revocation via `auth/logout { refreshToken }` and always clears local state (incl. new `authUser` key).

---

# P0-3 — Token Refresh / 401 Handling

Status: [x] Completed (2026-09-24)

Backend findings (2026-09-24 inspection):
- `auth/refresh-token` exists (RefreshTokenCommand). RefreshToken entity has `Revoked` + `ReplacedByToken`.
- Reuse detection was MISSING. Implemented (2026-09-24) in `RefreshTokenCommand.cs`: stored token null → Falid invalid; `Revoked != null` → revoke ALL user's active refresh tokens (family kill) then Falid invalid; `Expires <= UtcNow` → Falid invalid. Normal rotation still sets `Revoked` + `ReplacedByToken`.

Backend tests (new `Application.Tests` xUnit project, 2026-09-24):
- Project added to `AIShopVerse.slnx`; refs Application + Infrastructure + Domain; SQL Server test DB per test (name `AIShopVerseTests_<guid>`, base conn env-overridable via `AISHOPVERSE_TEST_CONNECTION`, default `Server=.;Trusted_Connection=True;TrustServerCertificate=True;`).
- `TestDb` boots the real DI (AddInfrastructureDependencies + AddApplicationDependencies + logging + IConfiguration) and `EnsureCreated()`s a dedicated DB; `TestConfiguration` provides config without the unavailable `Microsoft.Extensions.Configuration.Memory` package.
- `Auth/RefreshTokenFlowTests.cs` — 6 tests: login valid/invalid, rotation revokes old + `ReplacedByToken`, reuse fails + family revoked, unknown token fails, expired refresh token fails. **Result: 6/6 PASS.**
- NOTE: first fix iteration found BUG-1 (AutoMapper poison) — see Bugs Discovered section.

Frontend implemented (2026-09-24, both SPAs):
1. `auth.service.ts` — `refreshAccessToken()` calls `auth/refresh-token { token, refreshToken }`; single-flight via shared `refreshInFlight` observable (`shareReplay(1)` + `finalize` clearing); persists refreshed session only when `isSuccess && data.token` (`setSession`); `throwError` when no access/refresh token. `logout()` gained idempotency guard (no-op when no session) so refresh-failure paths log out exactly once.
2. `auth.interceptor.ts` — full rewrite: bearer attach (skips `/auth/` URLs); on HTTP 401 (non-auth route, no `X-Auth-Retry`) → `refreshAccessToken()` → retry original once with `X-Auth-Retry: true` + new bearer; inner `catchError` placed BEFORE `switchMap` so it catches ONLY the refresh POST error (logout + rethrow original) and the retried request's 401 propagates to the caller without a spurious logout; refresh `isSuccess:false` → logout + rethrow original; no infinite retry loop.
3. Frontend tests:
   - `auth.service.spec.ts` (both SPAs): new `describe('refreshAccessToken')` — persists new session (data), errors without tokens, keeps existing session on envelope failure, single in-flight request shared across concurrent callers. New logout tests: no-op without session, no duplicate logout POST. EndUser file fully rewritten (clean) after a bad substring edit; Admin file appended cleanly.
   - `auth.interceptor.spec.ts` (NEW, both SPAs, 7 tests): attaches bearer, skips auth routes, 401 → refresh → retries exactly once, refresh failure → logout, no infinite loop, concurrent 401s → single refresh, auth-route 401 not refreshed.
   - **Karma setup note**: Angular 18's `provideHttpClient(withInterceptors(...))` + legacy `HttpClientTestingModule` makes the real backend win (karma `/_karma_webpack_/` 404s). Correct recipe: `provideHttpClient(withInterceptors([authInterceptor]))` + `provideHttpClientTesting()` as sibling providers (no `HttpClientTestingModule` import).

Verification result (2026-09-24):
- EndUser `ng test` (ChromeHeadless): **67/67 PASS** (incl. 14 new auth/refresh/interceptor tests).
- Admin `ng test` (ChromeHeadless): **25/25 PASS** (incl. 14 new auth/refresh/interceptor tests).
- EndUser `npm run build`: PASS (only pre-existing `.form-floating>~label` CSS selector warning).
- Admin `npm run build`: PASS (no warnings).
- `dotnet build AIShopVerse.slnx`: 0 warnings / 0 errors.
- Backend tests: 6/6 PASS.
- E2E refresh smoke on live servers: pending — scheduled at the final mandatory E2E milestone (verification item 6).

Implementation date: 2026-09-24

Files:
- `Application/Features/AuthFeatures/Commands/RefreshTokenCommand.cs`
- `Application.Tests/` (`TestDb.cs`, `TestConfiguration.cs`, `Auth/RefreshTokenFlowTests.cs`, `Application.Tests.csproj`), `AIShopVerse.slnx`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/src/app/core/services/auth.service.ts`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/src/app/core/interceptors/auth.interceptor.ts`
- `...auth.service.spec.ts`, `...auth.interceptor.spec.ts` (both SPAs mirrored)

Database/migration changes: none for P0-3.

---

# P0-2 — Enum-string 400 contract gap (checkout/order payloads)

Status: [x] Completed — backend (2026-09-24); frontend payload "omit" cleanup deferred to P0-4/P0-5 where those payloads are already being touched)

Discovered: 2026-09-24 (inspection). NOT in the original plan — added because it blocks P0-4/P0-5 checkout payloads and Admin order GetAll.

Issue:
- Neither ASP.NET host registers a JSON enum string converter, so model binding of `paymentMethod:"CashOnDelivery"` (frontend sends enum member names as strings) and `status:"..."` (Admin order filtering / status body) failed with HTTP 400 before the handler ran.
- Reference rows where the frontends send enum-name strings that previously 400'd: EndUser `CreateOrder`/`Checkout` request bodies publish `paymentMethod`, and Admin order queries/transitions publish `status`.

Implemented (backward-compatible — numeric OUTPUT preserved, string OR number INPUT accepted):
- `EnumStringOrNumberConverterFactory` + `EnumStringOrNumberConverter<T>` (new file each server: `AIShopVerse.EndUser/AIShopVerse.EndUser.Server/EnumStringOrNumberConverter.cs`, `.../AdminPanel/...Server/EnumStringOrNumberConverter.cs`): reads JSON string (case-insensitive `Enum.TryParse`) or number (`Enum.ToObject`); WRITES the underlying integer — so existing API output (Admin `discountType === 0` display, EndUser order status ints) is unchanged and no frontend regressions. Registered in both `Program.cs` via `AddControllers().AddJsonOptions(Converters.Add(new EnumStringOrNumberConverterFactory()))`.
- NOT used: plain `JsonStringEnumConverter` — it writes strings and would break Admin `promotion-list` `discountType === 0` reads (verified by audit).
- No migration needed (serialization change only).

Tests (new `Application.Tests/Serialization/EnumStringOrNumberConverterTests.cs`; test project gained a reference to `AIShopVerse.EndUser.Server`):
- String member name binds (CashOnDelivery), number binds (1 → CreditCard), case-insensitive string, numeric output preserved (`{"PaymentMethod":0}`), unknown string → `JsonException`. **Result: 8/8 PASS** (suite total now 14/14).
- Full backend E2E of checkout request binding is covered by P0-5 checkout tests + Mandatory E2E item 6.

Verification: `dotnet build AIShopVerse.slnx` 0 warnings / 0 errors; `dotnet test` 14/14 PASS.

---

# P0-5 — Transactional Checkout + Stock Integrity

Status: [x] Completed (2026-09-24)

Required:

1. Add `BeginTransaction` / equivalent to UnitOfWork (`Infrastructure/Persistence/Repositories/UnitOfWork.cs` currently has none).
2. Checkout executes atomically.
3. Coupon validation before stock mutation.
4. Stock mutation concurrency-safe.

Chosen approach: **Option A** — atomic stock update `WHERE Stock >= requestedQuantity` and verify affected rows (fits existing SQL Server). EF optimistic concurrency considered, not chosen.

Implemented:

- `IUnitOfWork`/`UnitOfWork` (`Infrastructure/Persistence/Repositories/UnitOfWork.cs`):
  - `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` (idempotent; disposes the `IDbContextTransaction`).
  - `DecrementStockAtomicallyAsync(productId, quantity)` — raw SQL single-statement atomic decrement:
    `UPDATE [catalog].[Product] SET StockQuantity = StockQuantity - {quantity}, LastModifiedAt = {DateTime.UtcNow} WHERE Id = {productId} AND StockQuantity >= {quantity}`;
    returns affected rows (`1` = success). NOTE: table is `[catalog].[Product]` (singular — `nameof(Product)`), NOT `[catalog].[Products]`; an initial typo caused `Invalid object name 'catalog.Products'` until corrected.
- `CheckoutCommand.cs`:
  - Validates the coupon BEFORE any stock mutation (invalid coupon never touches stock).
  - `BeginTransactionAsync` → loop: pre-check `product.StockQuantity < orderItem.Quantity` → Falid + rollback; atomic decrement; `affected != 1` → Falid + rollback; set in-memory `product.StockQuantity` for the low-stock evaluation; `LowStockAlerter.EvaluateAsync` per product.
  - Single `CompleteAsync` + `CommitAsync` at the workflow boundary; `catch` → `RollbackAsync` + rethrow.
  - Payment/order status is still the pre-existing MOCK `Paid`/`Completed` (P0-4 flips COD to `Pending`).
- `LowStockAlerter`: removed its internal independent `CompleteAsync()` (was committing mid-workflow at the old `CheckoutCommand.cs:76`/`ILowStockAlerter.cs:71`); notifications now persist in the workflow's single SaveChanges/commit.
- `AdjustStockCommand.cs`: `EvaluateAsync` reordered to run BEFORE `CompleteAsync` (was committing from inside the alerter).

Tests (new `Application.Tests/Order/CheckoutFlowTests.cs`, 5 tests):
1. Success — order + order items + payment + stock decremented exactly once + cart cleared + low-stock notification persisted.
2. Invalid coupon — fails, stock unchanged, no partial order/payment.
3. Insufficient stock — fails, no partial write.
4. Rollback — multi-product checkout where the later product is insufficient rolls back the earlier stock mutation.
5. Concurrent — two DbContexts (same DB via shared connection string, same user) checkout the same stock-2 product simultaneously: exactly one succeeds, one fails, final stock 0, one order.

Harness changes (`Application.Tests/TestDb.cs`): `TestCurrentUser` now a mutable singleton registered AFTER `AddApplicationDependencies()` (so it wins over the scoped `CurrentUserService` registration — originally a "User not authenticated." bug); `SetCurrentUser(userId)` added; `currentUserId` constructor param removed. All 5 tests create the user first then `SetCurrentUser(user.Id)` to keep cart ownership and current-user in sync.

Verification result:
- `dotnet test` (Application.Tests): **19/19 PASS** (6 refresh + 8 enum converter + 5 checkout).
- `dotnet build AIShopVerse.slnx`: 0 warnings / 0 errors.
- E2E checkout smoke on live servers: pending — scheduled at the final mandatory E2E milestone (verification item 6).

Implementation date: 2026-09-24

Files:
- `Infrastructure/Persistence/Repositories/UnitOfWork.cs`
- `Application/Features/OrderFeatures/Commands/CheckoutCommand.cs`
- `Application/Features/InventoryFeatures/ILowStockAlerter.cs`
- `Application/Features/ProductFeatures/Commands/AdjustStockCommand.cs`
- `Application.Tests/TestDb.cs`, `Application.Tests/Order/CheckoutFlowTests.cs`

Database/migration changes: none (no schema change).

---

# P0-4 — Payment Mode

Status: [x] Completed (2026-09-24)

Decision: **COD-only MVP** (`Payment:EnabledMethod=COD`).

Required behavior:

### COD
- Checkout creates order `Pending`. Payment record `Pending`. Payment collected on delivery. Payment NOT marked Paid at checkout. On Delivered → payment `Pending → Completed` per existing domain rule (`PaymentStatus` enum: Pending/Completed/Failed/Refunded).

### Stripe
- If Stripe not configured (or method = COD): card attempts rejected clearly; never mark Paid without confirmed payment; checkout must not expose unusable Stripe flow.
- Remove mock-Paid branches (`CheckoutCommand.cs:107,128-139`, `CreateCheckoutCommand.cs:123-157`).

Frontend:
- Hide/disable card payment UI when Stripe unavailable; check `paymentPublicKey` before initializing Stripe; COD fully usable.

Implemented:

Backend:
- `Application/Features/OrderFeatures/Commands/CheckoutCommand.cs` (COD path): order now `OrderStatus.Pending`; payment now `PaymentStatus.Pending`, no MOCK `TransactionId`/`PaidAt`, `GatewayResponse = "Payment pending — collect on delivery."`. (CreditCard via CheckoutCommand still rejected with the existing clear message.)
- `Application/Features/PaymentFeatures/Commands/CreateCheckoutCommand.cs` (card/Stripe path): MOCK-paid branch REMOVED (`Mode="mock"`, `order.Status=Paid`, mock payment, `ApplyOrderSideEffectsAsync`, `_notifier`/`_lowStockAlerter` deps, `ToOrderDto` all deleted). Added early guard: `if (!_stripePayment.IsEnabled) return Falid("Credit card payments are unavailable. Please use Cash on Delivery.")` BEFORE the cart is loaded. Stripe-enabled intent flow unchanged.
- BUG-2 fixed (out-of-band, discovered by P0-4 test): `StripePaymentService` ctor built `new StripeClient(SecretKey)` eagerly → `ArgumentException: API key cannot be the empty string` on ANY scope resolving the handler when Stripe keys were absent (card endpoint 500'd before the handler's clean rejection ran). Now `_client` is created lazily (`_options.IsEnabled ? new StripeClient(...) : null`) and the three client-using methods guard with `_client ?? throw new InvalidOperationException("Stripe is not configured.")`. Stripe stays wired but runtime-disabled.
- `Infrastructure/Services/PaymentGateway/ExpiredPendingOrdersHostedService.cs`: previously cancelled **any** `OrderStatus.Pending` order after `PendingOrderTimeoutMinutes` — with COD orders now `Pending`, that would have auto-cancelled COD orders 30 min after placement. Filtered to ONLY abandoned CreditCard pendings (`db.Payments.Any(p => p.OrderId == o.Id && p.Status == Pending && p.Method == PaymentMethod.CreditCard.ToString())`); COD pendings are no longer expired. Doc comment updated.
- Config: `"Payments": { "EnabledMethod": "COD", ... }` added to both hosts' `appsettings.json` (`AIShopVerse.EndUser.Server`, `AIShopVerse.AdminPanel.Server`). Card enforcement is key-based via `StripeOptions.IsEnabled` (consistent with the frontend `publishableKey` gate).

Frontend (EndUser `checkout.component.ts`):
- Card radio + card-details block now `*ngIf="publishableKey"` (hidden when Stripe unavailable).
- Removed the "No Stripe key configured — payment will be recorded as a mock card payment" note.
- `submitCard` no longer handles `res.mode === 'mock'` (backend no longer returns it).
- `onSubmit` guards: selecting CreditCard without a `publishableKey` → toastr `'Card payments are currently unavailable. Please use Cash on Delivery.'`, no API call, no navigation. COD path unchanged.

Tests:
- `CheckoutFlowTests` success assertions updated: order `Pending` (was `Paid`), payment `Pending` (was `Completed`), plus `Assert.Null(payment.TransactionId)`.
- New: `Checkout_WithCreditCard_IsRejectedWithoutTouchingStock` (CheckoutCommand + CreditCard → Falid "Credit card ...", no order/payment, stock unchanged).
- New: `CreateCheckout_WithStripeUnavailable_RejectsCard` (CreateCheckoutCommand, Stripe disabled → Falid "Credit card payments are unavailable", no order/payment, stock unchanged). This test surfaced BUG-2.
- `checkout.component.spec.ts`: replaced the mock-fallback test with a DOM-visibility test (card option hidden without key, shown with key) and a guard test (no key + CreditCard → createCheckout NOT called, toastr error, no navigation); card-failure test now uses `publishableKey = 'pk_test_123'`.

Verification result:
- Backend `dotnet test`: **21/21 PASS** (6 refresh + 8 enum + 7 checkout/order).
- Backend `dotnet build AIShopVerse.slnx`: 0 warnings / 0 errors.
- EndUser karma: **68/68 PASS**; EndUser `npm run build`: PASS (only pre-existing `.form-floating>~label` CSS warning).
- Admin karma: **25/25 PASS**; Admin `npm run build`: PASS (no warnings).
- E2E checkout smoke on live servers: pending — scheduled at the final mandatory E2E milestone (verification item 6).

Implementation date: 2026-09-24

Files:
- `Application/Features/OrderFeatures/Commands/CheckoutCommand.cs`
- `Application/Features/PaymentFeatures/Commands/CreateCheckoutCommand.cs`
- `Infrastructure/Services/PaymentGateway/StripePaymentService.cs`
- `Infrastructure/Services/PaymentGateway/ExpiredPendingOrdersHostedService.cs`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.Server/appsettings.json`, `.../AdminPanel.Server/appsettings.json`
- `Application.Tests/Order/CheckoutFlowTests.cs`
- `AIShopVerse.EndUser/AIShopVerse.EndUser.client/.../checkout.component.ts` + `.spec.ts`

Database/migration changes: none.

Note on ordering: this phase also updated the P0-5 checkout success test to expect a `Pending` COD order — P0-5 remains fully verified alongside.

---

# P0-6 — Purchasable Product Variants

Status: [x] Completed (2026-09-27)

Decision: **Size/Color columns + product-level fallback.**

Done — size/color columns + unique `(ProductId, Size, Color)` + unique SKU (migration `20260927142636_AddProductVariants`, applied to dev DB); nullable `VariantId`/`VariantLabel` snapshot on `CartItem` + `OrderItem`; variant-aware product detail (must select size/color), add-to-cart (merge by `(ProductId, VariantId)`, variant price/stock), cart update, COD checkout (per-variant atomic stock decrement), Stripe create-checkout guard/snapshot, cancel-order restore, low-stock alerts per variant, admin variant editor (SKU/price/stock/size/color, deactivate-on-remove); `HasVariants` on all list DTOs. Tests 38/38 PASS. Note: `dotnet ef` migrations must use `--output-dir Migrations` (stale scripts at repo root reference `Persistence/Migrations` which is empty).

Required:

- `ProductVariant`: add `Size`, `Color`; keep SKU; unique `(ProductId, Size, Color)` + unique SKU.
- Add `VariantId` (nullable) to `CartItem` + `OrderItem`.
- Update flows: product details, add to cart, cart update, checkout, order creation, stock deduction, price calculation. Selected variant's price/stock/SKU MUST be used.
- Admin: variant management UI (create/edit/delete-deactivate, SKU, price, stock, size, color).
- Product details must require/select variant before add-to-cart when variants exist.
- Create and apply EF migration.

Tests:
- Multi-variant product; select size/color; correct VariantId/price/stock; variant stock decremented; independent variant stock; duplicate SKU rejected; duplicate product/size/color rejected.

---

# P0-7 — Configuration / Release Hygiene

Status: [x] Completed (2026-09-27)

Done:

- **JWT signing key**: `JwtKeyProvider.Resolve(IConfiguration)` (`Application/Features/AuthFeatures/Services`) used by `JwtTokenService`, `RefreshTokenCommand`, and both `Program.cs`. Resolution order: env `AISHOPVERSE_JWT_KEY`, then config `Jwt:Key`, else throw (Production-specific message when env is missing/Production). Resolved AFTER `builder.Build()` so `dotnet ef` design-time builds are not blocked (verified `migrations list`). Dev key lives ONLY in `appsettings.Development.json` (both hosts); base `appsettings.json` has no `Jwt:Key`. Key rotation: set `AISHOPVERSE_JWT_KEY` to the new value, restart both app pools (existing sessions invalidated on next refresh-token validation), then delete the old key.
- **CORS**: policy `AllowAngular` reads `Cors:AllowedOrigins` (EndUser localhost:4000, AdminPanel localhost:4200 in Development; absent in base config). Non-development startup throws if the section is missing/empty (no wildcard-with-credentials fallback). Checked eagerly after `builder.Build()`.
- **SQL Server**: base `appsettings.json` (both hosts) no longer sets `TrustServerCertificate`; Development keeps `TrustServerCertificate=True` for local self-signed certs. Production must provision a connection string with validated TLS (e.g. env `ConnectionStrings__DefaultConnection`).
- **Secret scanning**: `scripts\scan-secrets.ps1` (run manually; CI intentionally not used per decision #5). Scans tracked source; exits 1 on AWS/Azure/private-key/JWT/password usage. Allow-lists the dev JWT key locations, the explicit test key in `Application.Tests\TestDb.cs`, and documented test fixtures (`Str0ng!Passw0rd`, dummy/unknown sentinels). Verified: exit 0 on clean repo, exit 1 when a fake AWS key is introduced. No secrets in repo history by construction (JWT dev key was already committed pre-P0-7 — acceptable, Development-only).
- **SPA publish pipeline**: both server `.csproj` `PublishRunWebpack` target (`AfterTargets=ComputeFilesToPublish`, skippable via `-p:SkipPublishRunWebpack=true`): `npm ci`, `npm run build -- --configuration production`, then stages `dist/<app>/browser|dist/<app>` contents into publish `wwwroot\` via `ResolvedFileToPublish` (RelativePath computed with `GetRelativePath` from the detected output dir; avoids project-`wwwroot` pollution and works with Angular 17 application-builder `browser\` layout). Production serving switched from `UseSpa` to `UseStaticFiles` + `app.MapFallbackToFile("index.html")` (both hosts).
- **Verified**: EndUser publish → 89 DLLs + `wwwroot\index.html`; AdminPanel publish → index.html staged; published EndUser app smoke-run (env JWT key + CORS + connection string with trusted cert) served index.html and all 11 JS/CSS assets HTTP 200. Manual provisioning docs below.

Required:

- JWT: move signing key out of source-controlled config → env `AISHOPVERSE_JWT_KEY`; dev-only key in `appsettings.Development.json`; fail fast in Production if missing; document key rotation; do not commit secrets.
- CORS: production origins explicit (config `Cors:AllowedOrigins`), no broad wildcard with credentials. EndUser :4000, Admin :4200, API origins.
- SQL Server: `TrustServerCertificate` disabled in production; proper cert validation.
- Secret scanning: add commit/CI-time scanning where appropriate; no secrets in repo history/logs/test output/examples.
- SPA deployment: verify both server `.csproj` correctly build+copy Angular `dist`; document manual provisioning or add `.github` workflow per decision #5 (CI is intentionally not used → document manual provisioning; optionally add build/scan workflow).

Manual provisioning steps (no CI by decision #5):

1. `dotnet publish AIShopVerse.EndUser/AIShopVerse.EndUser.Server -c Release -o <deploy>/enduser` and likewise for `.../AIShopVerse.AdminPanel.Server` (Angular dist is built and staged into `wwwroot` automatically; requires `npm`/Node on the build machine).
2. Deploy the publish folders; set app pool identity environment variables:
   - `AISHOPVERSE_JWT_KEY` (32+ random chars, SAME value for both apps so tokens verify cross-app if shared issuer/audience are intended),
   - `ConnectionStrings__DefaultConnection` (SQL Server with validated server cert; e.g. `Encrypt=True;TrustServerCertificate=False`),
   - `Cors__AllowedOrigins__0` / `__1` (exact origins the browsers will call),
   - `ASPNETCORE_ENVIRONMENT=Production` (this is the default when unset — fail-fast actively defends a half-configured deploy).
3. Optionally run `powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1` before shipping to confirm no secrets.

---

# Remaining P1 Features

## P1-1 Admin Order Detail
- Dedicated admin order-detail query, no user filter, role guarded; include items, variant info, payment, customer, status. Do not reuse user-scoped query (`GetOrderByIdQuery` currently filters by current user via `GetOrderHistoryQuery.cs:82-83`; AdminPanel `OrderController.cs:20-22` broken).

Status: [x] Completed (2026-09-27)

- Backend: new `Application/Features/OrderFeatures/Queries/GetAdminOrderByIdQuery.cs` — `GetAdminOrderByIdQuery` (no `ICurrentUserService`, loads by `OrderId` only; `Falid("Order not found.")` when missing) returning new `AdminOrderDetailDto` with nested `AdminOrderItemDto` (incl. `VariantId`/`VariantLabel`), `AdminOrderCustomerDto` (FullName/UserName/Email/PhoneNumber via `Repository<ApplicationUser>`), `AdminOrderPaymentDto` (Amount/Method/TransactionId/Status/PaidAt/GatewayResponse); all null-safe. Admin `OrderController.cs:20-22` now uses the new query; controller keeps `[Authorize(Roles = "Admin,SuperAdmin")]`. EndUser `GetOrderByIdQuery` left user-scoped (customer-only). No migration (query-only).
- Frontend (AdminPanel): new standalone `features/orders-management/order-detail.component.ts` at route `orders/:id` (authGuard), read-only detail page (customer, shipping/billing, items table w/ variant, totals, payment block, status badge); `order-list` Order # now links to `/orders/:id`. Status transitions/refund intentionally remain on the list page (P1-2 scope).
- Tests: `Application.Tests/Order/AdminOrderDetailTests.cs` (3): another user's order returned with customer + variant items (no user filter); unknown id → Falid; payment mapped (amount = order total, method/status/transaction/PaidAt). 41/41 PASS overall.
- Verification: slnx build 0 warnings/0 errors; Admin ng build PASS + karma 25/25; EndUser ng build + karma 68/68 (regression).

## P1-2 Order Status Transition Matrix
- Define and enforce valid transitions from existing domain rules (enum: `Pending, Paid, Processing, Shipped, Delivered, Cancelled, Refunded`). Document matrix + enforce in backend; frontend exposes only valid transitions. Proposed matrix (derived, user-approved placeholders):
  * `Pending → Paid | Processing | Cancelled`
  * `Processing → Shipped | Cancelled`
  * `Shipped → Delivered | Cancelled`
  * `Delivered → (terminal)` — triggers COD payment `Pending → Completed`
  * `Paid → Refunded`
  * `Cancelled`, `Refunded` terminal; no same-status transitions.

Status: [x] Completed (2026-09-27)

- Backend: new `Application/Features/OrderFeatures/OrderStatusTransitions.cs` — single source of truth `Can(from, to)` (rejects same-status; `Delivered/Cancelled/Refunded` terminal) + `Targets(from)`. `UpdateOrderStatusCommand` now gates via matrix (`Invalid status transition from X to Y.`, `Order is already X.`) and, on `Delivered` with a `CashOnDelivery` payment in `Pending`, completes it (`Completed`, `PaidAt`, `GatewayResponse = "Collected on delivery."`). `RefundOrderCommand` restricted to `Paid → Refunded` (`Only paid orders can be refunded (current status: X).`).
- Also fixed latent validator bug: `UpdateOrderStatusCommandValidator` had `.NotEmpty()` on the enum, which rejected `Pending` (value 0) via the throwing `ValidationBehavior`; replaced with `.IsInEnum()`. User-initiated `CancelOrderCommand` (Pending-only, stock restore) untouched.
- Frontend (AdminPanel): `order-list` per-row dropdown now offers only the matrix-valid target statuses (from `allowedTransitions` map); terminal statuses render as plain text; `Refund` button only for `Paid` orders.
- Tests: `Application.Tests/Order/UpdateOrderStatusTests.cs` (7): full Pending→Processing→Shipped→Delivered walk; Delivered completes COD payment; invalid skip (Pending→Delivered) fails + order unchanged; terminal (Delivered→Cancelled) fails; same-status fails; Refund succeeds from Paid (payment also Refunded); Refund fails from Pending. 48/48 PASS overall.
- Verification: slnx build 0 warnings/0 errors; Admin ng build PASS + karma 25/25; EndUser karma 68/68 (regression).

## P1-3 Cancellation Policy
- Restore stock only if actually deducted; prevent double restoration, restoration after delivery, restoring for never-deducted stock. Tests around each relevant status.

Status: [x] Completed (2026-09-27)

- Design (user-approved): new `StockDeducted` bool on `Order` (single source of truth, set coincident with the two actual decrement points — COD checkout and card payment-complete); restore path is conditional on it and clears it → idempotent, no double restoration.
- Migration: `AddOrderStockDeducted` (`bit`, default false) added to `Infrastructure/Migrations`, applied to the dev DB. NOTE: `dotnet ef migrations add` requires a fresh build first — `--no-build` yields an empty migration.
- Deduction flag set: `CheckoutCommand` (COD, after atomic decrement loop) and `PaymentCompletionService.CompleteAsync` (card success path, before save). Card-failure path (order→Cancelled) never sets it → never restored.
- New `Application/Services/StockRestorer.cs` (`IStockRestorer`, registered in `AddApplicationDependencies`): per-line variant/product restore + `StockDeducted=false`; no low-stock alert on restore (stock only increases). Wired into `CancelOrderCommand` (user, Pending), `UpdateOrderStatusCommand` (admin cancel from Pending/Processing/Shipped via P1-2 matrix, now `Include(o => o.Items)`), and `RefundOrderCommand` (Paid→Refunded, `Include(o => o.Items)`).
- After-delivery protection unchanged: `Delivered` is terminal in the P1-2 matrix, so no cancellation/refund path exists post-delivery.
- Tests: `Application.Tests/Order/CancellationPolicyTests.cs` (6): user-cancel COD Pending restores stock (flag cleared); never-deducted card Pending cancel leaves stock untouched; admin-cancel from Processing AND Shipped restores; refund restores exactly once (double refund rejected, stock not double-restored); Delivered terminal — cancel rejected, stock stays deducted. 54/54 PASS overall.
- Verification: slnx build 0 warnings/0 errors; Admin karma 25/25 + EndUser karma 68/68 (regression); scan script exit 0.

## P1-4 Admin Expiry Worker
- Background worker for expired orders/payments (currently only EndUser has `ExpiredPendingOrdersHostedService`). Idempotent, restart-safe, logged, configurable, tested; use existing background-service pattern.

Status: [x] Completed (2026-09-27)

- Extraction: expiry logic moved out of the hosted service into `Infrastructure/Services/PaymentGateway/ExpiredOrderProcessor.cs` (`IExpiredOrderProcessor.ProcessAsync`) — returns processed count, uses `IUnitOfWork` + `IRealtimeNotifier`. Semantics preserved exactly: only Pending orders with an unpaid `CreditCard` payment older than `Payments:PendingOrderTimeoutMinutes` (default 30) are cancelled; their pending payments → `Failed` ("Order expired before payment was completed."); an OrderStatus notification + realtime event are sent. COD orders stay Pending (deliberate, documented). No stock restore: card checkouts never deduct at Pending (deduction only on payment success).
  - Layering note: `ExpiredOrderProcessor` lives in Infrastructure (NOT Application) because Infrastructure already depends on `IUnitOfWork`/`IRealtimeNotifier` and cannot reference Application (Application → Infrastructure only). Interface + registration added in `AddInfrastructureDependencies`.
- Hosted service `ExpiredPendingOrdersHostedService` is now a thin loop: scope-per-pass, resolves `IExpiredOrderProcessor`, logs when it expires orders; interval configurable via new `Payments:OrderExpiryIntervalMinutes` (default 1, min clamped to 1) in `PaymentOptions`.
- Idempotent + restart-safe: each pass only targets Pending orders past the persisted `CreatedAt` cutoff → safe for concurrent/duplicate passes (both server apps run the worker against the shared DB). Registered in BOTH `EndUser.Server` (existing) and `AdminPanel.Server` (new `AddHostedService` + using).
- Tests: `Application.Tests/Order/ExpiredOrderProcessorTests.cs` (3): old unpaid card order expired (order Cancelled, payment Failed, notification, stock untouched, second pass → 0 = idempotent); recent card order left Pending; old COD order + its deducted stock untouched. 57/57 PASS overall.
- Verification: slnx build 0 warnings/0 errors; Admin karma 25/25 + EndUser karma 68/68 (regression); scan script exit 0.

## P1-5 IDOR Protection
- Every cart-item operation requires `cartItem.Cart.UserId == currentUser`. Audit Get/Update/Delete/quantity/checkout + any endpoint accepting cart/cart-item ids (`UpdateCartItemCommand.cs:31-36` currently lacks ownership check). Add authz tests for another user's cart item.

Status: [x] Completed (2026-09-27)

- Audit result — cart surface: `GetCartQuery` (user-scoped by `UserId`, safe), `AddToCartCommand` (user-scoped cart lookup, safe), `ApplyCartCouponCommand` (user-scoped, safe), `RemoveCartItemCommand` (already had `cart.UserId == currentUser` gate), `CheckoutCommand` (user-scoped cart + current user, safe), `CartController` (only the above, no raw-id GET). **The single gap was `UpdateCartItemCommand`** (loaded cart item by id with no ownership check, then could mutate quantity/price on another user's cart item).
- Fix: `UpdateCartItemCommand.cs` now loads the item's `Cart` by `cartItem.CartId` and rejects with `"You do not own this cart item."` (same message/pattern as `RemoveCartItemCommand`) when `cart.UserId != currentUser`, before any product/qty work.
- Tests: `Application.Tests/CartFeatures/CartItemOwnershipTests.cs` (3): another user's cart item update rejected + quantity unchanged; owner's update succeeds (qty + DTO reflect change); another user's remove rejected + item still present. NOTE: test namespace is `Application.Tests.CartFeatures` — a `Application.Tests.Cart` namespace would shadow the `Domain.Entities.CartEntities.Cart` type for the other test files. 60/60 PASS overall.
- Verification: slnx build 0 warnings/0 errors; Admin karma 25/25 + EndUser karma 68/68 (regression); scan script exit 0.

## P1-6 Email Provider Abstraction
- `IEmailService` + SMTP implementation, config-driven, no hard-coded credentials, async API, clear failure handling, logging without leaking secrets, testable. Decouple business handlers from SMTP.

Status: [x] Completed (2026-09-27)

- Layering note: Infrastructure cannot reference Application, so the SMTP implementation lives in Application (same assembly as `IEmailService` + the old `DevEmailService`), keeping handlers decoupled. Only consumer today is `ForgotPasswordCommand` — already depends on `IEmailService`, so no handler changes were needed.
- New types (namespace `Application.Services`):
  - `EmailOptions` — config POCO: `Enabled`, `SmtpHost`, `SmtpPort` (default 587), `Username`, `Password`, `FromAddress`, `FromDisplayName` (default "AIShopVerse"), `UseSsl` (default true). Bound from the `Email` config section; no credentials are committed (appsettings.json ships `Enabled: false` + empty fields).
  - `EmailMessage` — `To`/`Subject`/`Body`/`IsHtml`.
  - `ISmtpTransport` — `SendAsync(message, options, ct)` seam so service logic is testable without a mail server.
  - `SmtpClientTransport` — real SMTP via `System.Net.Mail.SmtpClient` (framework, no new package): only sets `NetworkCredential` when `Username` is present; `ConfigureAwait(false)`.
  - `SmtpEmailService` — resolves options from `IConfiguration.GetSection("Email").Get<EmailOptions>()`. When disabled OR `SmtpHost`/`FromAddress` blank → logs a warning with the reset link (dev fallback, replaces old `DevEmailService` behavior) and no-ops. When enabled → delegates to `ISmtpTransport`; transport exceptions are caught, logged (host/email, never the password), and NOT propagated so the anonymous forgot-password UX never 500s.
- Registration: `AddApplicationDependencies` now registers `ISmtpTransport → SmtpClientTransport` (singleton) and `IEmailService → SmtpEmailService` (singleton); `DevEmailService.cs` deleted.
- Config: `Email` section added to `AIShopVerse.EndUser/appsettings.json` and `AIShopVerse.AdminPanel/appsettings.json` (disabled + empty creds). Operators enable via env vars/secrets, e.g. `Email__Enabled=true`, `Email__SmtpHost=...`; SMTP credentials are supplied only through the env-var/secret path — the committed config contains no credentials.
- Tests: `Application.Tests/Auth/EmailServiceTests.cs` (4) with an in-memory `TestConfiguration` + fake `ISmtpTransport`: not-configured no-op fallback; enabled delegates message/options (To, subject, body link, HTML, From, port, SSL); enabled-but-host-missing no-op; transport failure caught without propagating. 64/64 PASS overall.

## P1-7 Query Optimizations
- Only where evidence: N+1 (`GetCartDtoAsync`), full-table loads (dashboard/recommendations), missing `AsNoTracking`, pagination. No speculative changes.
- Status: [x] Completed.
- Evidence-based audit (before/after):
  - Cart N+1: `GetCartQuery`, `AddToCartCommand`, `UpdateCartItemCommand` each had a private `GetCartDtoAsync` that fetched one Product/query per cart item (3 duplicate copies). Fixed with a shared `CartProjector` (`Application/Features/CartFeatures/CartProjector.cs`) that issues a single products query (`productIds.Contains(p.Id)`, with Images+Variants, `AsNoTracking`) and sets `item.UnitPrice` to the current price (behavior-preserving). `UpdateCartItemCommand` also dropped its duplicate cart load — the ownership gate (`Cart.UserId == currentUser`, P1-5) now runs on a single `.Include(c => c.Items)` load.
  - Dashboard: `GetDashboardAnalyticsQuery` loaded ALL orders, then filtered in memory. Now `AsNoTracking` + SQL `Where(o => o.CreatedAt >= fromDate)` only when `Days.HasValue`; orderItems and low-stock queries also `AsNoTracking`.
  - Recommendations: `GetRecommendedForUserQuery.GetPersonalizedAsync` loaded every active product with all 6 includes (Category, Parent, Brand, Attributes, Images, Variants, Reviews) to score in memory. Now a narrow candidate projection (Id, CategoryId, CategoryParentId, BrandId, StockQuantity, CreatedAt, Attributes) with signal-product exclusion pushed to SQL (`!excludeIds.Contains(p.Id)`) + `AsNoTracking`; identical scoring; only the top-N result ids are re-fetched with full includes. Signal/popular/fill queries gained `AsNoTracking`.
  - `AsNoTracking` added where missing: `ProductDiscoveryQueries` (related + frequently-bought), `GetNewArrivalsQuery`, `GetPromotionalProductsQuery`, `GetBestSellersQuery` (products + fill).
  - Untouched (already bounded): catalog list/search/filter, orders, inventory, reviews.
- Test infra: `Application.Tests/TestQueryCounter.cs` (thread-safe `DbCommandInterceptor` capturing command text; `SelectCount(tableToken)` case-insensitive; `TestDb` registers it via `services.AddSingleton<IInterceptor>(...)` and exposes `QueryCounter`). `InfrastructureDependencyInjection.AddDbContext` now resolves DI-registered interceptors into the options (`AddInterceptors(provider.GetServices<IInterceptor>())`, empty in prod) so interceptors actually attach to the test context.
- Tests: `Application.Tests/CartFeatures/CartQueryPerformanceTests.cs` (2 — single product SELECT drives a 3-item cart; AddToCart with an existing item upserts without N+1), `Application.Tests/DashboardFeatures/DashboardQueryPerformanceTests.cs` (2 — 14-day filter pushed to SQL (`[CreatedAt] >= @__...`) with 1 order/250 revenue; no-Days path returns all 2 orders/349, no `>=` in SQL), `Application.Tests/RecommendationFeatures/GetRecommendedForUserTests.cs` (3 — personalized ranking excludes signals and ranks the shared-category candidate; SQL split evidence: a narrow `[catalog].[Product]` scan with `IsActive` + `NOT IN` and no `[dbo].[Review]`, plus a top-N detail fetch WITH `[dbo].[Review]`/images/variants; cold-start falls back to Popular mode via a single bestseller query). 71/71 PASS, build 0/0, karma regressions green, secrets scan exit 0 (2026-09-29).

## P1-8 UX State Pass
- Loading/empty/error states, validation messages, unauthorized/expired sessions, checkout/payment/stock errors, success feedback, disabled/loading buttons, duplicate-submit guards. No redesign.
- Status: [x] Completed (2026-09-29).
- Session expiry: both SPAs' `auth.interceptor.ts` fixed so a 401 on the retried request (or a failed/successful-but-empty refresh) calls `logout(true)` and the retried `next()` is wrapped in `catchError` — previously the second 401 escaped downstream of `catchError` with no logout/redirect. `auth.service.logout(expired = false)` clears local state before the best-effort server revocation, then navigates to `/login?sessionExpired=1` (+ `returnUrl` on EndUser) when expired. `auth.guard.ts` now passes `returnUrl`; both `login.component.ts` read `sessionExpired` and `returnUrl`. Admin `auth.interceptor.spec.ts` and EndUser `auth.interceptor.spec.ts` updated to assert logout + expired-login navigation on the second 401 (previously codified the broken no-logout behavior).
- EndUser: `checkout` surfaces server message (`err?.error?.message`) and navigates to `/orders/:id` after success with an order-number toast (spec updated); `cart` (loading/loadError/empty gating, `processingIds` Set, `couponBusy`, invalid-qty reset); `order-history` switched to `OrderService.getHistory()` with loading/loadError; `order-detail` polling pipeline reordered to `catchError → delay → repeat` (no longer dies on first error) with loading/loadError and an `if (this.order)` status guard; `wishlist` (loading/loadError/`removingId`); `product-detail` (loading/loadError + `cartBusy`/`wishlistBusy`/`reviewBusy` guards, `(items || []).some` null guard, server messages); `product-list` (loadError + retry, inject `ToastrService` for wishlist errors, `wishlistBusy` per-id, `returnUrl` on login redirect); `home`/`product-slider` (loadError banner, per-product in-flight add-to-cart guard + server message).
- Admin: all list/detail components gained `loading`/`loadError` + retry + gated empty rows. `order-list` (`refundingId`, `statusBusy` with status revert on error + feedback); `order-detail` (loading + loadError distinct from "Order not found.", `loadOrder()` refactor); `dashboard` (`loading`/`loadError`, `ngOnDestroy` `takeUntil` closes the SignalR `newOrder$`/`lowStock$` leak); `product-list` (`saving` guard + required-field validation (nameEN/nameAR/sku/categoryId, price > 0, discount < price) + server message + `adjusting` guard); `category-list`/`brand-list` (`saving`/`loading`/`loadError` + name-required validation); `promotion-list` (`saving`/`loading`/`loadError`/empty row + `togglingId` + code/value/percent≤100/date-range validation); `review-list` (`loading`/`loadError` + `actionId` in-flight guards + confirm-reject); `inventory` (`loading`/`loadError`/empty row + `adjustingId` guard + integer-qty validation + feedback + `ngOnDestroy` `takeUntil` for the SignalR subscription).
- Verification: `dotnet build AIShopVerse.slnx` 0 warnings / 0 errors; `dotnet test` (Application.Tests) 71/71 PASS; EndUser karma 68/68; AdminPanel karma 25/25; `scripts\scan-secrets.ps1` exit 0 (2026-09-29).

---

# Testing Strategy

Create `Application.Tests` (xUnit) targeting SQL Server `AIShopVerse.Tests`. Focused coverage:

- Authentication: successful login, failed login, refresh, refresh reuse, logout.
- Checkout: successful COD, invalid coupon, insufficient stock, concurrent checkout, transaction rollback, payment state.
- Variants: selection, pricing, stock, duplicate SKU, duplicate attributes.
- Authorization: IDOR protection, admin order access, user/admin role boundaries.
- Order lifecycle: valid transitions, invalid transitions, cancellation, stock restoration.

Frontend: `auth.service.spec.ts` (both SPAs); payload specs per phase.

---

# Mandatory Verification Before Release

Record ACTUAL results for each; do not claim pass without running.

1. **Backend build** — `dotnet build AIShopVerse.slnx` → PASS, no errors.
2. **EndUser build** — `npm run build` (in EndUser client dir) → PASS; record warnings vs errors.
3. **Admin build** — `npm run build` (in Admin client dir) → PASS; record warnings vs errors.
4. **Backend tests** — `dotnet test` (Application.Tests) → record total/passed/failed/skipped.
5. **Migration** — `dotnet ef database update --project Infrastructure --startup-project AIShopVerse.EndUser/AIShopVerse.EndUser.Server --context ApplicationDbContext` (== `update-database.sh`); verify migration exists, schema updated, no destructive changes, variant constraints present.
6. **E2E login + checkout** — HTTP-level smoke, EndUser (login, refresh page, browse, select variant, add to cart, COD checkout, verify order + stock) and Admin (login, view order, detail, status transitions, payment/order state). Record results.
7. **Seed verification** — counts, roles, admin data, products, variants, categories, images, image URLs resolve.

---

# Important Implementation Rules

1. Modify the existing project. 2. No parallel implementation. 3. Do not rewrite unrelated working features. 4. Follow existing Clean Architecture boundaries. 5. Domain logic in the appropriate layer. 6. Business rules enforced by backend, not Angular. 7. Never trust frontend authorization. 8. Do not silently swallow exceptions. 9. Do not log tokens/passwords/connection strings/secrets. 10. Never commit real secrets. 11. Use migrations for schema changes. 12. Avoid breaking API contracts unless necessary; prefer backward-compatible changes. 13. Reuse existing abstractions before creating new ones. 14. Do not claim completion without verification. 15. If a genuinely ambiguous decision affects business behavior, stop and report rather than inventing a rule. 16. Issues outside this plan that block release → document separately as `BLOCKER` under Newly Discovered Issues. 17. Keep changes minimal and production-oriented.

---

# Newly Discovered Issues

None yet.

---

# Phase Completion Checklist

A phase is `[x] Completed` only when: implementation complete + tests added + tests pass + build succeeds + migrations applied where applicable + manual/E2E verification done where required + this file updated with actual results. Otherwise keep `[~]` or `[!]`.