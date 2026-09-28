# PROJECT_BLUEPRINT.md

Use this document as a system prompt for scaffolding a new e-commerce project that follows the same structure, naming, architecture, and implementation patterns as `AIEmPowerYourself`, adapted for a Noon/Amazon-style product catalog with an admin back office. As in the reference repo, do not create a separate `GS.Identity` project. Authentication and user management must live inside the AdminPanel and EndUser API hosts and use the same application database.

## 1. Project Identity

Build a multi-project .NET 8 and Angular 18 solution modeled after `AIEmPowerYourself`, named `AIShopVerse` (rename freely, but keep every reference consistent across projects, namespaces, and configs).

The solution contains:

- A layered backend split into `Domain`, `Application`, and `Infrastructure`.
- Two ASP.NET Core hosts:
  - `AIShopVerse.AdminPanel.Server` for admin APIs, product/category/brand management, inventory, orders management, coupons, admin authentication, Swagger, and Angular SPA hosting.
  - `AIShopVerse.EndUser.Server` for public/storefront APIs, product browsing/search/filter, cart, checkout, orders, reviews, wishlist, end-user registration/login, SignalR (order status + notifications), and Angular SPA hosting.
- Two Angular clients:
  - `AIShopVerse.adminpanel.client` on port `4200`.
  - `AIShopVerse.EndUser.client` on port `4000`.
- One shared application database accessed through `Infrastructure/Persistence/ApplicationDbContext.cs`.

Do not scaffold `GS.Identity`. Identity tables, user entities, roles, refresh tokens, and auth controllers must be part of the main layered solution and attached to the same database used by products, categories, orders, carts, reviews, and admin data.

## 2. Folder/File Structure

Scaffold this root structure:

```text
.
|-- .github/
|   |-- copilot-instructions.md
|   `-- workflows/
|       `-- azure-webapps-dotnet-core.yml
|-- AIShopVerse.sln
|-- README.md
|-- add-migration.sh
|-- update-database.sh
|-- Domain/
|   |-- Domain.csproj
|   |-- GlobalUsings.cs
|   |-- Common/
|   |-- Const/
|   |-- Entities/
|   |   |-- BaseEntities/
|   |   |-- Identity/
|   |   |-- CatalogEntities/
|   |   |   |-- Product.cs
|   |   |   |-- ProductVariant.cs
|   |   |   |-- ProductImage.cs
|   |   |   |-- ProductAttribute.cs
|   |   |   |-- Category.cs
|   |   |   |-- Brand.cs
|   |   |-- CartEntities/
|   |   |-- OrderEntities/
|   |   |-- PaymentEntities/
|   |   |-- ReviewEntities/
|   |   |-- WishlistEntities/
|   |   |-- PromotionEntities/
|   |   `-- NotificationEntities/
|-- Application/
|   |-- Application.csproj
|   |-- ApplicationDependencyInjection.cs
|   |-- GlobalUsings.cs
|   |-- Base/
|   |   |-- Wrapper/
|   |-- DTO/
|   |   |-- AuthDtos/
|   |   |-- ProductDtos/
|   |   |-- CartDtos/
|   |   `-- OrderDtos/
|   |-- Features/
|   |   |-- AuthFeatures/
|   |   |-- ProductFeatures/
|   |   |-- CategoryFeatures/
|   |   |-- BrandFeatures/
|   |   |-- InventoryFeatures/
|   |   |-- CartFeatures/
|   |   |-- OrderFeatures/
|   |   |-- PaymentFeatures/
|   |   |-- ReviewFeatures/
|   |   |-- WishlistFeatures/
|   |   |-- PromotionFeatures/
|   |   `-- <BusinessArea>Features/
|   |-- Handlers/
|   |   |-- UserFeature/
|   |   |-- ProductService/
|   |   |-- OrderService/
|   |   `-- <BusinessArea>Feature/
|   |-- Interfaces/
|   `-- Services/
|-- Infrastructure/
|   |-- Infrastructure.csproj
|   |-- InfrastructureDependencyInjection.cs
|   |-- GlobalUsings.cs
|   |-- Common/
|   |-- Persistence/
|   |   |-- ApplicationDbContext.cs
|   |   |-- Configurations/
|   |   |-- Migrations/
|   |   |-- Repositories/
|   |   `-- Seed/
|   |-- Services/
|   |   |-- PaymentGateway/
|   |   `-- FileStorage/
|   `-- Signalr/
|-- AIShopVerse.AdminPanel/
|   |-- AIShopVerse.AdminPanel.Server/
|   |   |-- Controllers/
|   |   |   |-- AuthControllers/
|   |   |   |-- Base/
|   |   |   |-- ProductController.cs
|   |   |   |-- CategoryController.cs
|   |   |   |-- BrandController.cs
|   |   |   |-- InventoryController.cs
|   |   |   |-- OrderController.cs
|   |   |   |-- PromotionController.cs
|   |   |   `-- <Feature>Controller.cs
|   |   |-- Program.cs
|   |   |-- appsettings.json
|   |   `-- appsettings.Development.json
|   `-- AIShopVerse.adminpanel.client/
|       |-- angular.json
|       |-- package.json
|       `-- src/
|-- AIShopVerse.EndUser/
|   |-- AIShopVerse.EndUser.Server/
|   |   |-- Controllers/
|   |   |   |-- AuthControllers/
|   |   |   |-- ProductController.cs
|   |   |   |-- CategoryController.cs
|   |   |   |-- CartController.cs
|   |   |   |-- OrderController.cs
|   |   |   |-- ReviewController.cs
|   |   |   |-- WishlistController.cs
|   |   |   `-- <Feature>Controller.cs
|   |   |-- Program.cs
|   |   |-- appsettings.json
|   |   `-- appsettings.Development.json
|   `-- AIShopVerse.EndUser.client/
|       |-- angular.json
|       |-- package.json
|       `-- src/
`-- packages/
```

Folder purposes:

- `Domain`: POCO entities for catalog, inventory, cart, orders, payments, reviews, wishlist, promotions, auth/identity entities, enums, schema names, and constants.
- `Application`: business use cases (product search/filter, cart pricing, checkout, order lifecycle, coupon redemption), auth use cases, MediatR commands/queries, DTOs, validation, mapping, and result wrappers.
- `Infrastructure`: EF Core database context, migrations, configurations, repositories, unit of work, seed JSON (categories, brands, demo products), blob storage for product images, Stripe payment gateway (`Services/PaymentGateway/`), and SignalR services.
- `AIShopVerse.AdminPanel.Server`: admin API host — product/category/brand CRUD, stock adjustments, order fulfillment, promotions/coupons, admin auth endpoints, Swagger, Serilog, migrations/seeding, Angular publishing.
- `AIShopVerse.EndUser.Server`: storefront API host — product listing/search/filter/sort, product detail, cart, checkout, payment initiation, order tracking, reviews, wishlist, registration/login, SignalR order-status notifications, rate limiting, migrations/seeding, Angular publishing.
- `AIShopVerse.*.client`: Angular clients using feature modules, shared UI, route guards, interceptors, environment files, and colocated specs.

Naming conventions:

- Backend projects use PascalCase names: `Domain`, `Application`, `Infrastructure`, `AIShopVerse.AdminPanel.Server`, `AIShopVerse.EndUser.Server`.
- Do not use a project named `GS.Identity`.
- Domain entity folders are grouped by business area: `CatalogEntities`, `InventoryEntities`, `CartEntities`, `OrderEntities`, `PaymentEntities`, `ReviewEntities`, `WishlistEntities`, `PromotionEntities`, `Identity`.
- Application features use `{EntityOrArea}Features`: `ProductFeatures`, `CategoryFeatures`, `CartFeatures`, `OrderFeatures`, `AuthFeatures`, `PromotionFeatures`.
- Commands end with `Command`: `AddProductCommand`, `UpdateProductCommand`, `AddToCartCommand`, `PlaceOrderCommand`, `LoginCommand`, `RegisterUserCommand`.
- Queries end with `Query`: `GetAllProductsQuery`, `GetProductByIdQuery`, `GetFilteredProductsQuery`, `GetCurrentUserQuery`, `GetOrderHistoryQuery`.
- Handlers are nested in the same file and end with `Handler`: `GetFilteredProductsQueryHandler`.
- Services use `I<ServiceName>` plus implementation: `IAuthService` / `AuthService`, `IProductService` / `ProductService`, `ICartService` / `CartService`, `IPaymentService` / `PaymentService`.
- Controllers end with `Controller`: `AuthenticationController`, `ProductController`, `CategoryController`, `CartController`, `OrderController`.
- Angular components use `name.component.ts`, `name.component.html`, `name.component.scss`, `name.component.spec.ts`.
- Angular feature folders live under `src/app/Modules/<feature>` such as `Catalog`, `Cart`, `Checkout`, `Orders`, `Auth`, `Wishlist`.

## 3. Architecture & Design Patterns

Use layered architecture with CQRS-style application use cases.

Layer flow:

```text
Angular Client
-> ASP.NET Controller
-> MediatR Command/Query Handler
-> Application service
-> UnitOfWork/RepositoryBase
-> EF Core ApplicationDbContext
-> One shared SQL Server database
```

Project references:

- `Domain` has no project references.
- `Infrastructure` references `Domain`.
- `Application` references `Infrastructure` in this codebase style and uses repository/unit-of-work types directly.
- `AIShopVerse.AdminPanel.Server` references `Application`.
- `AIShopVerse.EndUser.Server` references `Application`.

Primary patterns:

- CQRS with MediatR: commands/queries implement `IRequest<Result<T>>` or `IPaginatedRequest<Result<T>>`.
- Repository pattern: `IRepositoryBase<T>` exposes `FindAll`, `FindByCondition`, `Create`, `Update`, `Delete`, attach methods, and transaction helpers.
- Unit of Work: `IUnitOfWork.Repository<T>()` creates repositories and `CompleteAsync()` commits changes.
- Dependency injection: each layer exposes extension methods such as `AddDomainDependencies` and `AddInfrastructureDependencies`.
- AutoMapper: one central `MappingProfile` maps commands, DTOs, and entities.
- FluentValidation: validators are registered from the Application assembly and executed through `IPipelineBehavior<,>`.
- Result wrapper: all use cases return `Result<T>.Success(...)`, `Result<T>.Falid(...)`, or `Result<T>.Info(...)`.
- Integrated identity: auth handlers, auth services, user entities, roles, refresh tokens live in the same layer structure as other features.
- Faceted filtering: product queries build dynamic `IQueryable<Product>` predicates from category, brand, price range, rating, and attribute filters before pagination.
- Cart/order consistency: cart totals and stock checks are recomputed server-side at checkout, never trusted from the client.
- SignalR: use `NotificationHub`, `CustomUserIdProvider`, and `ConnectedUserTracker` for order-status push updates.

Controller base pattern:

```csharp
[Route("api/[controller]")]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender _mediator = null!;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected async Task<TResult> QueryAsync<TResult>(IRequest<TResult> query)
        => await Mediator.Send(query);

    protected async Task<TResult> CommandAsync<TResult>(IRequest<TResult> command)
        => await Mediator.Send(command);

    protected ActionResult<T> Single<T>(T data)
        => data == null ? NotFound() : Ok(data);
}
```

Storefront feature endpoint pattern (EndUser):

```csharp
[Route("api/[controller]")]
[ApiController]
public class ProductController : ApiControllerBase
{
    [HttpPost("Filter")]
    public async Task<ActionResult<Result<PaginatedResult<ProductListItemDTO>>>> GetFilteredProducts(GetFilteredProductsQuery query)
        => Single(await QueryAsync(query));

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ProductDetailDTO>>> GetProductById(string id)
        => Single(await QueryAsync(new GetProductByIdQuery(id)));
}

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ApiControllerBase
{
    [HttpPost("Add")]
    public async Task<ActionResult<Result<CartDTO>>> AddToCart(AddToCartCommand command)
        => Single(await CommandAsync(command));

    [HttpPost("Checkout")]
    public async Task<ActionResult<Result<OrderDTO>>> Checkout(CheckoutCommand command)
        => Single(await CommandAsync(command));
}
```

Admin feature endpoint pattern (AdminPanel):

```csharp
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ProductController : ApiControllerBase
{
    [HttpPost("GetAll")]
    public async Task<ActionResult<Result<PaginatedResult<ProductDTO>>>> GetAllProducts(GetAllProductsQuery query)
        => Single(await QueryAsync(query));

    [HttpPost("Add")]
    public async Task<ActionResult<Result<string>>> AddProduct(AddProductCommand command)
        => Single(await CommandAsync(command));

    [HttpPut("Update")]
    public async Task<ActionResult<Result<string>>> UpdateProduct(UpdateProductCommand command)
        => Single(await CommandAsync(command));

    [HttpPost("AdjustStock")]
    public async Task<ActionResult<Result<int>>> AdjustStock(AdjustStockCommand command)
        => Single(await CommandAsync(command));
}
```

Integrated auth endpoint pattern:

```csharp
[Route("api/auth")]
[ApiController]
public class AuthenticationController : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Login(LoginCommand command)
        => Single(await CommandAsync(command));

    [HttpPost("register")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Register(RegisterUserCommand command)
        => Single(await CommandAsync(command));
}
```

## 4. Tech Stack

Backend:

- C# / .NET 8: all backend projects target `net8.0`.
- ASP.NET Core Web API: controllers, Swagger, CORS, authentication, static files, and SPA fallback.
- Entity Framework Core 8: SQL Server provider, migrations, model configurations, and seed data.
- ASP.NET Core Identity 8: integrated into `ApplicationDbContext`, not a separate project.
- MediatR 12.2.0: CQRS request/handler dispatch.
- AutoMapper 13.0.1: entity/DTO/command mapping.
- FluentValidation 11.9.1: request validation through pipeline behavior.
- Dapper 2.1.35: direct SQL/report-style access for dashboards and heavy filter queries when needed.
- EPPlus 8.0.6: bulk product import/export via Excel.
- Azure.Storage.Blobs 12.25.0 (or HuaweiCloud.SDK.OBS 3.0.3): product image and asset storage.
- A payment gateway SDK (e.g. Stripe.net or PayMob REST integration): checkout/payment intents and webhooks.
- Serilog.AspNetCore 8.0.1 and Serilog.Sinks.MSSqlServer 6.6.1: structured logging to console and SQL `Logs` table.
- Swashbuckle.AspNetCore 6.6.2: Swagger/OpenAPI with JWT and custom API-key headers.

Frontend:

- Angular 18.2.x with TypeScript 5.5.2.
- RxJS 7.8.x and Zone.js 0.14.x.
- Angular Material/CDK, ng-zorro-antd, ng-bootstrap: UI frameworks, especially for admin data grids and forms.
- ngx-translate and Transloco: localization.
- angular-auth-oidc-client and MSAL Angular/Browser: optional OAuth/OIDC (Google/Microsoft social login on the storefront).
- `@microsoft/signalr`: real-time order-status notifications in EndUser.
- Bootstrap, Font Awesome, Remix Icon: styling/icons.
- SweetAlert2, ngx-toastr, ngx-spinner: feedback and loading states.
- Chart.js/ng2-charts/ngx-charts, d3: admin dashboards (sales, top products, stock levels).
- xlsx, exceljs, file-saver, jspdf: exports, invoices, and order documents.
- ngx-slider or similar: price-range filter controls.
- Karma/Jasmine: Angular unit tests.

## 5. Backend Code Conventions

Use namespace-per-folder style and mutable DTOs/entities:

```csharp
namespace Application.Features.ProductFeatures.Commands
{
    public class AddProductCommand : IRequest<Result<string>>, IMapFrom<Product>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; }

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        [Required]
        public string CategoryId { get; set; }

        public string? BrandId { get; set; }

        public int StockQuantity { get; set; }

        public List<string>? ImageUrls { get; set; } = [];
    }
}
```

Entity conventions:

- Entities are POCO classes in `Domain/Entities/<BusinessArea>`.
- Identity entities are also in `Domain/Entities/Identity`.
- Use `[Table(nameof(Entity), Schema = Schemas.<SchemaName>)]` for schema-specific tables.
- Navigation properties use `ICollection<T>` or `List<T>`.
- Audit fields come from `AuditedEntity` or `BaseEntity`.

Example domain entity:

```csharp
[Table(nameof(Product), Schema = Schemas.CATALOG)]
public class Product : AuditedEntity
{
    public string Id { get; set; }
    public string NameAR { get; set; }
    public string NameEN { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string CategoryId { get; set; }
    public Category Category { get; set; }
    public string? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public ICollection<ProductImage> Images { get; set; }
    public ICollection<ProductAttribute> Attributes { get; set; }
    public ICollection<Review> Reviews { get; set; }
}
```

Example integrated user entity:

```csharp
[Table(nameof(ApplicationUser), Schema = Schemas.Identity)]
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
```

Schema constants:

```csharp
public static class Schemas
{
    public const string DEFAULT = "dbo";
    public const string CATALOG = "catalog";
    public const string Identity = "Identity";
    public const string Inventory = "inventory";
    public const string Sales = "sales";
    public const string Content = "Content";
}
```

Result conventions:

```csharp
return await _unitOfWork.CompleteAsync(cancellationToken) > 0
    ? Result<string>.Success(product.Id, Localizer[ResourcesLocalizationKeys.AddSuccess])
    : Result<string>.Falid(null, Localizer[ResourcesLocalizationKeys.AddFailed]);
```

Repository/filter conventions:

```csharp
var query = _unitOfWork.Repository<Product>().FindAll()
    .Where(p => p.IsActive);

if (!string.IsNullOrEmpty(request.CategoryId))
    query = query.Where(p => p.CategoryId == request.CategoryId);

if (!string.IsNullOrEmpty(request.BrandId))
    query = query.Where(p => p.BrandId == request.BrandId);

if (request.MinPrice.HasValue)
    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= request.MinPrice);

if (request.MaxPrice.HasValue)
    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= request.MaxPrice);

query = request.SortBy switch
{
    "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
    "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
    "newest" => query.OrderByDescending(p => p.CreatedAt),
    _ => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
};

var paginated = await query.ToPaginatedListAsync(request);
```

Configuration conventions:

- Backend configuration lives in each host's `appsettings.json` and `appsettings.Development.json`.
- Use one `DefaultConnection` for application data and identity data.
- Use named sections: `PaymentGateway`, `Jwt`, `Google`, `BlobStorageSettings`, `HostsType`.
- Angular clients use `src/environments/environment.ts` and `environment.prod.ts`.
- Use the same API endpoint for business and auth routes when identity is integrated:

```ts
export const environment = {
  production: false,
  apiEndpoint: 'https://localhost:7125/api/',
  apiIdentityEndpoint: 'https://localhost:7125/api/',
  signalRUrl: 'https://localhost:7125/',
  paymentPublicKey: '<payment-gateway-public-key>',
  googleClientId: '<google-client-id>'
};
```

## 6. Core Backend Components

ApplicationDbContext:

- Lives at `Infrastructure/Persistence/ApplicationDbContext.cs`.
- Use one context for catalog/order entities and identity entities.
- Prefer inheriting from `IdentityDbContext<ApplicationUser, ApplicationRole, string>` when scaffolding integrated identity.
- Define `DbSet<T>` properties grouped by business areas.
- Apply EF configurations using `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);`.

Example:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

Repository and unit of work:

```csharp
public interface IUnitOfWork
{
    IFileRepository FileRepository { get; }
    IRepositoryBase<T> Repository<T>() where T : class;
    Task<int> CompleteAsync(CancellationToken cancellationToken);
    Task<int> CompleteAsync();
}
```

Dependency injection:

```csharp
public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorageSettings"));
    services.Configure<PaymentGatewaySettings>(configuration.GetSection("PaymentGateway"));

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

    services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    services.AddTransient<IUnitOfWork, UnitOfWork>();
    services.AddTransient(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
    services.AddSignalR();
    services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
    services.AddSingleton<IConnectedUserTracker, ConnectedUserTracker>();

    return services;
}
```

Application service registration:

```csharp
private static void AddServices(this IServiceCollection services)
{
    services.AddSingleton<ICurrentUserService, CurrentUserService>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<ICartService, CartService>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IPaymentService, PaymentService>();
    services.AddScoped<IInventoryService, InventoryService>();
}
```

Authentication rules:

- Create users from AdminPanel (admins) or EndUser (customers) through commands in `Application/Features/AuthFeatures/Commands`.
- Store users, roles, claims, refresh tokens in the same SQL Server database as the app data.
- AdminPanel can create admin users and assign roles such as `Admin` and `SuperAdmin`.
- EndUser can create customer accounts, allow guest checkout if desired, and link orders/cart to `ApplicationUser.Id`.
- Use JWT bearer authentication for API calls.
- Support Google (and optionally Microsoft) token validation in EndUser for social login.
- Extract SignalR tokens from `/notificationHub?access_token=...` for order-status pushes.
- Controllers use `[Authorize]`, `[Authorize(Roles = "Admin,SuperAdmin")]`, or a project-local custom auth attribute.

E-commerce domain rules:

- `Product.StockQuantity` is decremented transactionally at order placement, inside the same unit-of-work commit as the order — never as a separate call.
- `Cart` totals (subtotal, discount, tax, shipping) are recalculated server-side from current `Product` prices at every cart read and at checkout; the client never sends authoritative totals.
- `Coupon` validation (date range, usage limit, minimum order value) happens inside `CheckoutCommandHandler`, not in the client.
- `Order` has a status enum (`Pending`, `Paid`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `Refunded`); status transitions are admin-only except customer-initiated `Cancelled` while `Pending`.
- `Review` creation is restricted to users with a `Delivered` order containing that product.
- Product search/filter supports: category, subcategory, brand, price range, rating, in-stock only, and free-text search across `NameAR`/`NameEN`/`SKU`.

Security middleware:

- Configure host-specific CORS.
- Remove server disclosure headers: `Server`, `X-Powered-By`, `X-AspNet-Version`, `X-AspNetMvc-Version`.
- Add clickjacking headers: `X-Frame-Options: DENY` and `Content-Security-Policy: frame-ancestors 'none';`.
- EndUser uses fixed-window rate limiting, 60 requests per minute per IP (checkout/payment endpoints use a stricter limit).

## 7. Angular Structure & Conventions

Each Angular client follows this shape (Angular 18 standalone components):

```text
src/
|-- app/
|   |-- app.config.ts
|   |-- app.routes.ts
|   |-- core/
|   |   |-- guards/
|   |   |-- interceptors/
|   |   |-- models/          # EndUser only; AdminPanel types are inline in services
|   |   `-- services/
|   `-- features/
|       |-- auth/
|       |-- Catalog/         # EndUser: product list, filters, product detail
|       |-- Cart/            # EndUser
|       |-- Checkout/        # EndUser
|       |-- Orders/          # EndUser
|       |-- Wishlist/        # EndUser
|       |-- landing/         # EndUser
|       |-- dashboard/       # AdminPanel
|       |-- products/        # AdminPanel
|       |-- categories/      # AdminPanel
|       |-- brands/          # AdminPanel
|       |-- inventory/       # AdminPanel
|       |-- orders-management/# AdminPanel
|       |-- promotions/      # AdminPanel
|       `-- reviews/         # AdminPanel
|-- assets/
`-- environments/
```

Routing conventions:

- Use lazy-loaded standalone components with `loadComponent()` (not `loadChildren`).
- Wrap feature routes in layout components:
  - EndUser: `AppLayoutComponent` (header, category nav, search bar, cart icon, footer).
  - AdminPanel: `LayoutPortalComponent` (sidebar with Products/Categories/Orders/Promotions).
- Protect authenticated branches with guards such as `authGuard` or `superAdminGuard`.
- Use wildcard fallback: `{ path: '**', redirectTo: '', pathMatch: 'full' }`.

Guard pattern:

```ts
export const authGuard: CanActivateFn = (route, state) => {
  const accService = inject(AccountService);
  const router = inject(Router);

  if (accService.isLoggedIn()) return true;

  router.navigateByUrl('/login');
  return false;
};
```

Product filter state pattern (EndUser catalog):

```ts
export interface ProductFilterState {
  categoryId?: string;
  brandId?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  inStockOnly?: boolean;
  searchTerm?: string;
  sortBy?: 'newest' | 'price_asc' | 'price_desc' | 'rating';
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ProductFilterService {
  private filterSubject = new BehaviorSubject<ProductFilterState>({ page: 1, pageSize: 24 });
  filter$ = this.filterSubject.asObservable();

  updateFilter(partial: Partial<ProductFilterState>) {
    this.filterSubject.next({ ...this.filterSubject.value, ...partial, page: 1 });
  }
}
```

Integrated auth service pattern:

```ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    this.loadStoredUser();
  }

  login(model: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${environment.apiIdentityEndpoint}auth/login`, model)
      .pipe(tap((response) => {
        if (response.success && response.token) this.setSession(response);
      }));
  }

  setSession(authResult: AuthResponseDto) {
    if (authResult.token) {
      sessionStorage.setItem('acessToken', authResult.token);
      sessionStorage.setItem('refreshToken', authResult.refreshToken!);
      sessionStorage.setItem('expiresAt', authResult.expiresAt!.toString());
    }
  }
}
```

Cart service pattern:

```ts
@Injectable({ providedIn: 'root' })
export class CartService {
  private cartSubject = new BehaviorSubject<CartDto | null>(null);
  cart$ = this.cartSubject.asObservable();

  constructor(private http: HttpClient) {}

  addToCart(productId: string, quantity: number): Observable<CartDto> {
    return this.http.post<Result<CartDto>>(`${environment.apiEndpoint}cart/add`, { productId, quantity })
      .pipe(map(r => r.data!), tap(cart => this.cartSubject.next(cart)));
  }
}
```

## 8. Testing Setup

Frontend tests:

- Angular unit tests are colocated with components/services/pipes as `*.spec.ts`.
- Test runner is Karma with Jasmine.
- Script: `npm test` runs `ng test`.
- Keep component specs beside implementation files:

```text
product-card.component.ts
product-card.component.html
product-card.component.scss
product-card.component.spec.ts
```

Backend tests:

- If adding backend tests to a new scaffold, create `Application.Tests/Application.Tests.csproj` referencing `Application`, `Domain`, and `Infrastructure`.
- Prioritize handler-level tests for pricing/discount calculation, stock decrement on checkout, and coupon validation, since these carry the most business risk.

## 9. Build, Run, and Deployment

Backend build:

```bash
dotnet restore AIShopVerse.sln
dotnet build AIShopVerse.sln
```

Angular development:

```bash
cd AIShopVerse.AdminPanel/AIShopVerse.adminpanel.client
npm install -f
npm start
```

```bash
cd AIShopVerse.EndUser/AIShopVerse.EndUser.client
npm install -f
npm start
```

Ports:

- AdminPanel Angular: `https://localhost:4200`.
- EndUser Angular: `https://localhost:4000`.
- EndUser API local environment uses `https://localhost:7125/api/`.
- AdminPanel API local environment uses `https://localhost:7377/api/`.
- Auth routes are hosted by the same API servers, for example `https://localhost:7125/api/auth/login` and `https://localhost:7377/api/auth/login`.

SPA publish pattern:

- Server `.csproj` files define `SpaRoot`, `SpaProxyLaunchCommand`, and `SpaProxyServerUrl`.
- Publish target runs `npm install -f`, then `npm run build --prod`, then copies Angular `dist` output into published `wwwroot`.

Migration scripts:

```bash
./add-migration.sh MigrationName
./update-database.sh
```

CI/CD:

- GitHub Actions workflow: `.github/workflows/azure-webapps-dotnet-core.yml`.
- Trigger: merged pull request into `Production`.
- Requires PR label matching matrix `name`, e.g. `AIShopVerse-EndUser`, `AIShopVerse-AdminPanel`.
- Uses `actions/checkout@v4`, `actions/setup-dotnet@v4` with `dotnet-version: 8.x`.
- Publishes with `dotnet publish -c Release -o published`.
- Deploys with `azure/webapps-deploy@v2` and an Azure publish profile secret.

## 10. Scaffold Rules for New Projects

When generating this project from the blueprint:

1. Create `Domain`, `Application`, `Infrastructure`, `AIShopVerse.AdminPanel.Server`, `AIShopVerse.EndUser.Server`, and both Angular clients.
2. Do not create `GS.Identity`.
3. Put identity entities in `Domain/Entities/Identity`; put catalog/cart/order/payment/review entities in their own `Domain/Entities/<BusinessArea>` folders.
4. Put auth commands, queries, DTOs, and services in `Application/Features/AuthFeatures`, `Application/DTO/AuthDtos`, and `Application/Services`.
5. Put auth controllers in both API hosts under `Controllers/AuthControllers` or `Controllers/AuthenticationController.cs`.
6. Use one `ApplicationDbContext` and one `DefaultConnection` for business data and identity data.
7. Let AdminPanel create/manage products, categories, brands, inventory, promotions, and order fulfillment.
8. Let EndUser create/manage customer accounts, carts, checkout, orders, reviews, and wishlists — all in the same database.
9. Keep business logic out of controllers; controllers only dispatch MediatR commands/queries.
10. Use `Result<T>` for all command/query outcomes.
11. Use `IUnitOfWork.Repository<TEntity>()` for data access inside handlers/services.
12. Add EF configurations under `Infrastructure/Persistence/Configurations/<BusinessArea>`.
13. Add seed JSON under `Infrastructure/Persistence/Seed/SeedData/<BusinessArea>` (seed at least demo categories, brands, and products).
14. Register all infrastructure/application/auth/payment services through dependency injection extension methods.
15. Keep Angular features under `src/app/features/<feature>` (not `Modules/`) using Angular 18 standalone components with lazy-loaded routes via `loadComponent()`. AdminPanel types are defined inline in services rather than a separate `models/` directory — both are acceptable conventions.
16. Use `environment.ts` for API endpoints and client IDs; never hardcode API URLs inside components.
17. Preserve Swagger security definitions for `Bearer` and any custom API-key headers.
18. Preserve localization defaults and supported cultures: `en-US`, `ar-EG` (extend as needed), with a defined backend default.
19. Preserve security headers, CORS policies, server-header removal, and stricter rate limiting on checkout/payment endpoints.
20. Preserve publish behavior where ASP.NET hosts build and copy Angular clients into `wwwroot`.
21. Recompute cart totals and validate stock/coupon rules server-side at every checkout — never trust client-submitted prices or totals.
22. Gate review creation on a `Delivered` order for that product/user pair.
