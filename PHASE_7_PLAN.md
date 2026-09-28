# Phase 7 — Per-User Product Recommendations

## Goal

Give each logged-in shopper a personalized "Recommended for you" section on the **Home** page, computed deterministically from their **existing order history + wishlist**. New/anonymous-light users get a "Popular right now" fallback so the section always has content. No LLM, no new tracking, no schema change.

## Decisions (user-confirmed)

- **Deterministic in-app** logic (no LLM/embeddings/API keys) — matches the project's zero-dependency ethos.
- **Scope**: per-user recommendations only (ranked search/autocomplete, related products, bought-together, data-driven home rebuild all deferred).
- **Personalization source** = existing order history + wishlist (no new explicit-signal tracking).
- **Placement** = a section on the **Home** page, only when logged in.
- **Presentation** = single labeled section ("Recommended for you" vs "Popular right now"); product cards link to detail. No per-card reasons.
- **Fixed count** (default 12); no load-more.

## Current State (verified at planning time)

Search was basic substring `Contains`; there was no recommendation engine. `ICurrentUserService` (in `Application/ApplicationDependencyInjection.cs`) resolves the user id from the `NameIdentifier` claim and is registered for the EndUser host. Order history gives `OrderItem.ProductId` per user; wishlist gives `WishlistItem.ProductId`. `HomeComponent` was a static "Welcome" page. Client pattern: services return raw response read via `res.data`.

## Work / Status

### Backend
- [x] `Application/Features/RecommendationFeatures/Queries/GetRecommendedForUserQuery.cs` (`Count=12` → `Result<RecommendedForUserDto>`):
  - Builds signal set = **purchased product ids** (non-cancelled/refunded orders) ∪ **wishlisted ids**.
  - **Cold start** (`signalIds.Count < 2`): `Mode = "Popular"` — top products by order-item units sold, topped up with newest arrivals.
  - **Personalized**: affinity profile from signal products (category weight, parent-category weight, brand weight, shared attribute name|value set); scores candidate active products excluding already-purchased/wishlisted — category (+5/occurrence), parent category (+3), brand (+3), shared attributes (+1 each), in-stock (+0.5), best-seller-volume tiebreak; returns top-N.
  - Reuses `ProductListItemDto` for cards; single `RecommendedForUserDto { Mode, Products }`.
- [x] `AIShopVerse.EndUser.Server/Controllers/RecommendationController.cs` — `POST api/recommendation/ForMe`, `[Authorize]`.

### Frontend (EndUser)
- [x] `core/services/recommendation.service.ts` — `getForMe(count=12)` → `res.data` (`RecommendedForYou { mode, products }`).
- [x] Rebuilt `home.component.ts`: hero + Bootstrap product-card grid (`/products/:id`) labeled by mode; fetched only when `auth.isLoggedIn()`; graceful hidden/empty state + login prompt for guests.

### Tests / gates
- [x] `dotnet build AIShopVerse.slnx` — 0 warnings / 0 errors.
- [x] EndUser `ng build` — success (pre-existing selector warning only).
- [x] EndUser Karma — **33/33 SUCCESS** (30 prior + 3 new `RecommendationService` specs: created, posts `{count}`, default count 12).
- Admin untouched (regression not exercised this phase).

## Known gaps / deferred
- **Deferred from Phase 7 selection** (user narrowed scope): ranked/autocomplete search, related products, frequently-bought-together, multi-section data-driven home — all reusable future phases.
- **Affinity is recency-unweighted** (counts only) — could weight recent orders higher.
- **No caching**: the query rebuilds on each Home load; fine at this scale, could add a short-lived cache/timer if needed.
- **No per-card "why" reasons** (category/brand attribution) — deferred by choice.
