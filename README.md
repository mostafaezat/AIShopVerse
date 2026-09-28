# AIShopVerse

Full-stack e-commerce platform built with .NET 8 (CQRS/MediatR/Repository/UnitOfWork) and Angular 18.

## Architecture

- **Domain** — Entities, base classes, constants
- **Application** — CQRS features (commands/queries), DTOs, MediatR handlers
- **Infrastructure** — EF Core, repositories, UnitOfWork, SignalR, seeding
- **AIShopVerse.AdminPanel.Server** — Admin API (port 7377)
- **AIShopVerse.EndUser.Server** — Customer API (port 7125)
- **AIShopVerse.adminpanel.client** — Admin Angular SPA (port 4200)
- **AIShopVerse.EndUser.client** — Customer Angular SPA (port 4000)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 8, ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Patterns | CQRS, MediatR, Repository, UnitOfWork |
| Auth | JWT Bearer (ASP.NET Identity) |
| Realtime | SignalR |
| Frontend | Angular 18 (Standalone Components) |
| DB | SQL Server |

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- SQL Server

### Database Setup
```bash
# Run from solution root
bash update-database.sh
```

### Run Backend
```bash
# Admin Panel API (port 7377)
dotnet run --project AIShopVerse.AdminPanel/AIShopVerse.AdminPanel.Server

# EndUser API (port 7125)
dotnet run --project AIShopVerse.EndUser/AIShopVerse.EndUser.Server
```

### Run Frontend
```bash
# Admin Angular (port 4200)
cd AIShopVerse.AdminPanel/AIShopVerse.adminpanel.client
npm install && npm start

# EndUser Angular (port 4000)
cd AIShopVerse.EndUser/AIShopVerse.EndUser.client
npm install && npm start
```

## Seed Data

- **Roles**: SuperAdmin, Admin, Customer
- **Catalog**: 12 categories, 10 brands, 20 products
- **Default admin**: admin@aishopverse.com / Admin@123

## Key Features

- JWT authentication with refresh tokens
- Product catalog with search, filter, and pagination
- Shopping cart and checkout flow
- Order management with status tracking
- Product reviews (delivery-gated)
- Wishlist
- Coupon/promotion system
- Admin dashboard with inventory management
- Role-based access (SuperAdmin, Admin, Customer)
- Rate limiting and security headers
