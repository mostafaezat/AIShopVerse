# Build Prompt: AIShopVerse E-Commerce Platform

Copy everything below into a fresh coding-agent session (e.g. Claude Code) along with `PROJECT_BLUEPRINT.md` in the repo root, and let it work through the phases in order.

---

You are scaffolding a new full-stack e-commerce solution called **AIShopVerse**, using `PROJECT_BLUEPRINT.md` in this repository as the authoritative architecture, naming, and convention reference. Follow it exactly — same layered .NET 8 architecture, same CQRS/MediatR/Repository/UnitOfWork patterns, same integrated-identity approach (no separate `GS.Identity` project), same Angular 18 structure.

## What we're building

A Noon/Amazon-style e-commerce app with two independent web apps sharing one database:

- **Storefront (EndUser)**: customers browse products by category, filter by brand/price/rating/stock, search, view product details, add to cart, checkout, pay, track orders, leave reviews, and manage a wishlist.
- **Admin Panel (AdminPanel)**: staff manage categories, brands, products (with images and variants), stock levels, orders (fulfillment/status), promotions/coupons, and view a sales dashboard.

## Ground rules

1. Read `PROJECT_BLUEPRINT.md` fully before writing any code. Its folder structure, naming conventions, and code patterns are non-negotiable — don't improvise different names or a different layering.
2. Build backend before frontend. Get one vertical slice (Product: entity → EF config → repository → command/query → handler → controller) fully working end-to-end before fanning out to the rest of the catalog.
3. Every command/query returns `Result<T>`. Controllers only call `Mediator.Send` — no business logic in controllers.
4. Cart totals, stock checks, and coupon validation are always recomputed server-side. Never trust a total, price, or stock count sent from the client.
5. Use one `ApplicationDbContext` inheriting `IdentityDbContext<ApplicationUser, ApplicationRole, string>` for both business and identity data.
6. After each phase, the solution must build (`dotnet build`) and, once Angular scaffolding exists, `ng build` must pass before moving to the next phase.

## Phase 1 — Solution & backend skeleton

- Create the `.sln` and the `Domain`, `Application`, `Infrastructure`, `AIShopVerse.AdminPanel.Server`, `AIShopVerse.EndUser.Server` projects with correct project references (per blueprint §3).
- Add `GlobalUsings.cs`, base entity classes (`BaseEntity`, `AuditedEntity`), `Schemas` constants, and the `Result<T>` wrapper.
- Set up `ApplicationDbContext`, Identity (`ApplicationUser`, `ApplicationRole`, `RefreshToken`), and register EF Core + Identity in `InfrastructureDependencyInjection.cs`.
- Wire MediatR, AutoMapper, and FluentValidation pipeline behavior in `Application`.
- Confirm `dotnet build` succeeds with zero business features yet.

## Phase 2 — Auth (integrated, both hosts)

- Domain: identity entities in `Domain/Entities/Identity`.
- Application: `AuthFeatures` with `RegisterUserCommand`, `LoginCommand`, `RefreshTokenCommand`, `GetCurrentUserQuery`; DTOs in `DTO/AuthDtos`; `IAuthService`/`AuthService` for token issuance.
- Both hosts: `AuthenticationController` at `api/auth`, JWT bearer configured in `Program.cs`.
- AdminPanel additionally supports role assignment (`Admin`, `SuperAdmin`) on user creation.
- Seed one `SuperAdmin` user via `Infrastructure/Persistence/Seed`.

## Phase 3 — Catalog domain (Category, Brand, Product)

- Domain entities: `Category`, `Brand`, `Product`, `ProductImage`, `ProductAttribute`, `ProductVariant` in `Domain/Entities/CatalogEntities`, with EF configurations under `Infrastructure/Persistence/Configurations/Catalog`.
- Application: `CategoryFeatures`, `BrandFeatures`, `ProductFeatures` — full CRUD commands/queries for admin, plus `GetFilteredProductsQuery` (category, brand, price range, rating, in-stock, free-text search, sort) and `GetProductByIdQuery` for the storefront.
- AdminPanel: `CategoryController`, `BrandController`, `ProductController` (`GetAll`, `Add`, `Update`, `Delete`, image upload, `AdjustStock`).
- EndUser: `CategoryController` (list/tree for nav), `ProductController` (`Filter`, `{id}` detail).
- Seed demo categories, brands, and ~20 products with images.

## Phase 4 — Cart, Checkout, Orders, Payments

- Domain: `Cart`, `CartItem`, `Order`, `OrderItem`, `Payment` in their respective entity folders; `Order` has a status enum (`Pending`, `Paid`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `Refunded`).
- Application: `CartFeatures` (`AddToCartCommand`, `UpdateCartItemCommand`, `RemoveCartItemCommand`, `GetCartQuery` — totals always recalculated from live product prices), `OrderFeatures` (`CheckoutCommand` that validates stock/coupon, decrements stock, creates the order and payment intent inside one transaction; `GetOrderHistoryQuery`; admin `UpdateOrderStatusCommand`).
- Infrastructure: `PaymentGateway` service wrapping your chosen provider (e.g. Stripe or PayMob), webhook handling for payment confirmation.
- EndUser: `CartController`, `OrderController` (place order, track order, cancel while `Pending`).
- AdminPanel: `OrderController` (list/filter orders, update status, view payment info).
- Wire SignalR `NotificationHub` so customers get a push update when their order status changes.

## Phase 5 — Reviews, Wishlist, Promotions

- Domain: `Review`, `WishlistItem`, `Coupon` entities.
- Application: `ReviewFeatures` (creation gated on a `Delivered` order for that product/user), `WishlistFeatures`, `PromotionFeatures` (coupon CRUD for admin, validation logic reused by `CheckoutCommand`).
- EndUser: `ReviewController`, `WishlistController`.
- AdminPanel: `PromotionController`.

## Phase 6 — Angular clients

- Scaffold both Angular 18 apps per blueprint §7 folder layout, with `environment.ts` pointing at the matching API host.
- **EndUser**: `Catalog` module (product grid, filter sidebar, sort, search bar, pagination), product detail page, `Cart`, `Checkout` (address + payment), `Orders` (history + tracking + SignalR live status), `Wishlist`, `auth` (login/register), shared `AppLayoutComponent` with category nav + cart icon.
- **AdminPanel**: `dashboard` (sales/orders/stock widgets), `Products` (grid + form with image upload and variants), `Categories`, `Brands`, `Inventory` (stock adjustment), `OrdersManagement` (status board), `Promotions`, shared `LayoutPortalComponent` with sidebar nav, `authGuard`/`superAdminGuard`.
- Implement `AuthService`, `CartService`, `ProductFilterService` per the patterns in blueprint §7.

## Phase 7 — Cross-cutting hardening

- Security headers, CORS, server-header removal on both hosts.
- Rate limiting on EndUser, stricter limits on checkout/payment endpoints.
- Swagger with JWT bearer scheme on both hosts.
- Serilog to console + SQL `Logs` table.
- Migration scripts (`add-migration.sh`, `update-database.sh`) and seed data verified end-to-end on a clean database.

## Deliverable checklist

- [ ] `dotnet build AIShopVerse.sln` succeeds
- [ ] Both Angular apps `ng build` successfully
- [ ] Can register/login as a customer and browse/filter/search products
- [ ] Can add to cart, apply a coupon, and complete checkout with stock correctly decremented
- [ ] Order shows up in customer's order history and updates live via SignalR on status change
- [ ] Can log in as admin, create a category/brand/product with images, and see it appear in the storefront
- [ ] Can leave a review only after an order reaches `Delivered`

Work phase by phase. After each phase, summarize what was built and confirm the build/test status before moving on.
