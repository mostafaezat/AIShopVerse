# Phase 3 — Catalog Domain (Category, Brand, Product)

## Goal

Deliver a complete, working catalog slice: categories, brands, products (with image / variant / attribute support), a storefront filtering/search/sort/pagination experience, and the admin management UI. Phase 3 is the storefront-facing "browse" vertical.

## Current State (verified at planning time)

Backend is **substantially implemented**. Entities, EF configs, CQRS feature handlers, controllers, and seed data already exist:

| Area | Status |
|------|--------|
| Entities (`Category`, `Brand`, `Product`, `ProductImage`, `ProductAttribute`, `ProductVariant`) | ✅ Present (`Domain/Entities/CatalogEntities`) |
| EF configurations | ✅ Present (`Infrastructure/Persistence/Configurations/Catalog`) |
| `CategoryFeatures` / `BrandFeatures` / `ProductFeatures` CRUD | ✅ Present |
| `GetAllProductsQuery`, `GetProductByIdQuery` | ✅ Present |
| `GetFilteredProductsQuery` | ✅ Present (⚠️ bugs below) |
| `AdjustStockCommand`, `UploadImageCommand` | ✅ Present |
| Admin controllers (Category/Brand/Product) | ✅ Present |
| EndUser controllers (`CategoryController.Tree`, `ProductController.Filter/{id}`) | ✅ Present |
| Seed demo catalog (JSON) | ✅ Present |
| EndUser Angular list + detail components, `ProductFilterService` | ⚠️ Minimal |
| Admin Angular product/category/brand lists | ⚠️ Minimal |

## Confirmed Bugs (must fix)

1. **`MinRating` filter is dead.** In `GetFilteredProductsQuery` (Application/Features/ProductFeatures/Queries/GetAllProductsQuery.cs) `MinRating` is declared but never applied — no `.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= MinRating)`. The storefront exposes `minRating`, so this silently does nothing.
2. **`SortBy: "rating"` is wrong.** The `"rating"` case sorts by `p.Images.Any()` instead of by review average. It should sort by `p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0` (descending).

## Work Breakdown

### 1. Backend bug fixes
- [ ] Apply `MinRating` filter in `GetFilteredProductsQuery`.
- [ ] Fix `"rating"` sort to order by computed average review rating descending.
- [ ] Ensure `rating` filter is applied before in-memory projection (keep inside EF queryable so it translates to SQL).
- [ ] Re-run `dotnet build AIShopVerse.sln`.

### 2. Backend verification / hardening
- [ ] Smoke-test `POST api/product/Filter` on EndUser host with each dimension (category, brand, min/max price, min rating, in-stock, search, each sort, pagination) and confirm SQL/results are correct.
- [ ] Confirm `ProductDetailDto` includes images, variants, attributes, and reviews (details page needs these).
- [ ] Add/adjust validators for `AddProductCommand` / `UpdateProductCommand` to cover variant & attribute payloads.

### 3. EndUser storefront catalog UI (Phase 6 references this slice)
- [ ] **Filter sidebar**: category tree/list, brand list, price (min/max), min rating (star control), in-stock toggle — each wired to `ProductFilterService.updateFilter(...)`.
- [ ] **Sort control**: newest / price-asc / price-desc / rating, wired to `sortBy`.
- [ ] **Search bar**: free-text search -> `searchTerm`.
- [ ] **Pagination**: page controls wired to `updatePage(...)`; show total count.
- [ ] **Product detail**: gallery (primary image + others), attributes table, variant picker, add-to-cart (defer link to Phase 4 cart), reviews section (read-only here; write gated in Phase 5).
- [ ] Render category nav from `GetCategoryTree` in the shared `AppLayoutComponent`.

### 4. Admin catalog management UI
- [ ] **Products**: grid + add/edit form with image upload (uses `UploadImageCommand`), variant editor, stock display. Wire `AdjustStock`.
- [ ] **Categories**: list + add/edit + (soft) delete, with parent/child for the tree.
- [ ] **Brands**: list + add/edit + delete.
- [ ] Ensure the added/edited product appears immediately in the storefront list.

### 5. Tests
- [ ] Angular `.spec.ts` for `ProductFilterService` (filter state transitions).
- [ ] Angular `.spec.ts` for `ProductListComponent` (renders grid from filter service, drives pagination).
- [ ] Karma tests run green (`npm test` in the client folders).
- [ ] Optional: minimal xUnit for the `GetFilteredProductsQuery` filter/sort logic.

### 6. Build / docs gate
- [ ] `dotnet build AIShopVerse.sln` succeeds.
- [ ] Both Angular clients `ng build` succeed.
- [ ] Update `README.md` / `PROJECT_BLUEPRINT.md` if any convention changed (none expected).

## Definition of Done
- A customer can browse categories, filter by brand/price/rating/stock, search, sort, and paginate the catalog.
- Product detail shows image gallery, attributes, variants.
- An admin can create/edit a category, brand, and product (with image + stock), and see it in the storefront.
- `MinRating` and `rating` sort work correctly.
- Builds pass and catalog tests are green.

---

## Implementation Status (updated)

Backend & frontend Phase 3 work is implemented and verified:

- [x] `MinRating` filter applied + `"rating"` sort fixed in `GetFilteredProductsQuery`.
- [x] `ProductDetailDto` now includes `variants`, images, attributes, and populated `AverageRating`/`ReviewCount`.
- [x] EndUser `BrandController` added (reuses `GetAllBrandsQuery`) for storefront brand filter.
- [x] EndUser storefront: filter sidebar (category/brand/price/min-rating/in-stock), sort control, search bar, pagination, category nav in header.
- [x] EndUser product detail: variant selector, attributes table.
- [x] Admin `getById` endpoint (reuses `GetProductByIdQuery`), product add/edit with image upload + discount + stock adjust, category & brand add/edit UI.
- [x] Fixed pre-existing broken Admin client build (removed dangling `bootstrap`/`ngx-toastr` CSS imports that had no installed packages).
- [x] Angular tests for `ProductFilterService` + `ProductListComponent` (all 12 tests green via ChromeHeadless).

### Known gaps / deferred
- Admin product form does **not** persist `ProductVariant` / `ProductAttribute` — the `AddProductCommand` / `UpdateProductCommand` payloads do not accept variants/attributes. Persisting these would require a backend command enhancement (and a storefront variant/attribute CRUD path), out of scope for this pass. Variants/attributes are readable on the storefront detail page.
- `GetFilteredProductsQuery` `"rating"` sort translates to SQL via `Average`; a large catalog may benefit from a materialized rating column later.
