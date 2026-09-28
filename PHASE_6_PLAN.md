# Phase 6 — Admin Dashboard Analytics

## Goal

Turn the static admin home page (a 4-widget nav grid) into a live analytics hub: KPI cards, a revenue-over-time line chart, an orders-by-status doughnut, top-products / low-stock / recent-orders tables, and a date-range selector — all backed by a single analytics endpoint.

## Decisions (user-confirmed)

- **Chart.js for rendering.** The admin client targets **Angular 18**, which forces the ng2-charts pin. `ng2-charts@5.0.4` is not standalone-component compatible (its directive requires an NgModule and it exposes no `provideCharts`/`withDefaultRegisterables`); `ng2-charts@6+` requires Angular 19+/CDK 19+. So we use **Chart.js v4 directly** (registered controllers) and dropped ng2-charts + its CDK peer dep. This still delivers the chosen interactive Chart.js charts with no unnecessary Angular-coupling dependency.
- **Single combined endpoint** returning one `DashboardAnalyticsDto` (loads once).
- **Date-range selector** (7 / 30 / 90 / All) that refetches revenue & status series.

## Current State (verified at planning time)

`dashboard.component.ts` was a hard-coded grid of link widgets (Orders/Products/Inventory/Promotions) with no live data. Admin client was zero-dependency (no chart lib), plain-style (no Bootstrap/ngx-toastr), services return raw responses read via `res.data`. Backend followed the CQRS `IUnitOfWork` + `[Authorize(Roles="Admin,SuperAdmin")]` controller pattern.

## Work / Status

### Backend
- [x] `Application/Features/DashboardFeatures/Queries/GetDashboardAnalyticsQuery.cs` — `Days` (int?, default null=all) + `TopProductCount` (default 5) → `Result<DashboardAnalyticsDto>`:
  - KPIs: `TotalRevenue`, `TotalOrders`, `AverageOrderValue`, `TotalUsers`, `TotalProducts`, `LowStockCount` (≤10), `PendingReviewsCount`.
  - `RevenueOverTime` `[{date, amount}]` — grouped by order `CreatedAt`, excludes `Cancelled`/`Refunded`, zero-filled per day for a clean line.
  - `OrdersByStatus` `[{status, count}]` — all `OrderStatus` values (zero-count included for full doughnut).
  - `TopProducts` `[{productName, unitsSold, revenue}]` from `OrderItem`.
  - `LowStockProducts` (top by stock asc, ≤10) + `RecentOrders` (latest 8).
  - Revenue = sum of `Order.Total` over non-cancelled/refunded orders in range.
- [x] `AIShopVerse.AdminPanel.Server/Controllers/DashboardController.cs` — `POST api/dashboard/GetAnalytics`.

### Frontend (AdminPanel)
- [x] Installed `chart.js@^4.5.1` (dropped `ng2-charts`/`@angular/cdk` for A18 compat).
- [x] `core/services/dashboard.service.ts` — `getAnalytics(days?, topProductCount=5)` → `res.data`.
- [x] Replaced `dashboard.component.ts`: KPI card row, range selector (7/30/90/All), Chart.js **line** (revenue) + **doughnut** (status) via `@ViewChild` canvas refs and `Chart.register(...)`; tables for top products, low-stock alerts, recent orders. Charts are destroyed/recreated on refetch.

### Tests / gates
- [x] `dotnet build AIShopVerse.slnx` — 0 warnings / 0 errors.
- [x] Admin `ng build` — success (chart.js bundles, `dashboard-component` lazy chunk ~188 kB raw).
- [x] EndUser `ng build` — success (regression; pre-existing budget/selector warnings only).
- [x] EndUser Karma — **30/30 SUCCESS**.
- Admin client still has no runnable test target (no karma/`"test"` builder); backend query verified via successful build only.

## Known gaps / deferred
- **XUnit coverage** for the analytics aggregation logic (revenue exclusions, day-filling, status counts) — deferred; could be added if a test harness is introduced.
- **Dashboard refresh** is manual (range change / reload); no auto-refresh timer or SignalR-driven refresh on order events.
- **More analytics** (revenue by payment method, coupon usage, repeat-customer rate, refund rate) deferred — easy to add to the same query/DTO.
- Chart.js is bundled into the `dashboard-component` lazy chunk; acceptable since it's only loaded on the admin home page.
