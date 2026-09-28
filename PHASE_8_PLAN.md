# Phase 8 — Search & Discovery Expansion

## Goal

Upgrade product discovery end-to-end: **ranked relevance full-text search**, a **debounced header-search autocomplete**, and **two new product-detail sections** ("Related products" + "Frequently bought together"). Builds directly on Phase 7's `ProductListItemDto` card pattern and the search groundwork. No schema change (read-only analysis of existing catalog/orders).

## Decisions (user-confirmed)

- Phase 8 scope = **Search & discovery expansion**:
  1. Ranked relevance search (weighted scoring; wider recall).
  2. Header search autocomplete (debounced, suggestions dropdown).
  3. "Related products" on product detail.
  4. "Frequently bought together" on product detail.
- Deterministic, in-hit under-the-hood logic (no Lucene/Elasticsearch — stays in-process, EF-translatable where possible).
- Autocomplete goes in the shared header (`AppComponent`), so it works on every page.

## Current State (at planning time)

Search (`GetAllProductsQuery` handler) was plain `Contains` over NameEN/NameAR/SKU, ordered by user-chosen sort (default `newest`) — no ranking, no description/brand/category recall. No header search box. Product detail had no related/bought-together content. Cross-page product cards reused `primaryImageUrl` + `(discountPrice ?? price)`; discovery endpoints only existed for per-user recommendations (`POST recommendation/ForMe`).

## Work / Status

### Backend
- [x] `GetAllProductsQuery` handler — ranked relevance:
  - Wider recall: name (EN/AR), SKU, `Category.NameEN`, `Brand.NameEN`, `DescriptionEN/AR`.
  - When a search term is present (and no explicit sort chosen, or `newest`/`relevance`), order by EF-translatable **relevance rank**: name-prefix match → name-contains → SKU → attrs/description/brand → newest. User-selected sorts (price/rating) still override.
- [x] New `Application/Features/SearchFeatures/Queries/SearchSuggestQuery.cs` — lightweight autocomplete (`SearchTerm`, `Count` default 8 clamped 1–20), prefix-preferring, returns `SearchSuggestionDto { Id, NameEN, NameAR, SKU, Price, DiscountPrice, ImageUrl }`.
- [x] New `Application/Features/ProductFeatures/Queries/ProductDiscoveryQueries.cs`:
  - `GetRelatedProductsQuery` — content-based (exact same category +8, sibling sub-category +5, same brand +4, shared attribute name|value +1 each; requires score>0), returns `List<ProductListItemDto>`.
  - `GetFrequentlyBoughtTogetherQuery` — order co-occurrence (non-cancelled/refunded orders containing the product; co-purchased items ranked by total quantity), `List<ProductListItemDto>`.
- [x] EndUser controllers: `SearchController` (`POST api/search/Suggest`) and `DiscoveryController` (`POST api/discovery/Related`, `POST api/discovery/BoughtTogether`).
- [x] `dotnet build AIShopVerse.slnx` → 0 warnings / 0 errors.

### Frontend (EndUser)
- [x] New `core/services/search.service.ts` — `SearchService.suggest(term?, count?)` → `SearchSuggestion[]`.
- [x] `core/services/recommendation.service.ts` — added `getRelated()` + `getBoughtTogether()` (→ `discovery/*`).
- [x] `AppComponent` header — search input with 300 ms debounce (rxJS `Subject` + `debounceTime/distinctUntilChanged/switchMap`), suggestions dropdown (thumb+name+price), Enter → full results (`/products?search=` + filter service), suggestion click → product detail; blur-close with `mousedown` precedence. No `FormsModule` (pure event handlers) to keep the main bundle lean.
- [x] `product-detail.component.ts` — added "Frequently bought together" + "Related products" card-grid sections (reuse placeholder fallback + currency display), fetched in `ngOnInit`.
- [x] `angular.json` — raised initial bundle `maximumWarning` 600 kB → 700 kB to reflect legitimate header-feature growth (build was green, only budget warning was new).

### Tests / gates
- [x] `dotnet build AIShopVerse.slnx` — 0/0.
- [x] EndUser `ng build` — success (pre-existing `.form-floating>~label` selector warning only).
- [x] EndUser Karma — **38/38 SUCCESS** (new `SearchService` spec: created, posts term+count, default count 8; `RecommendationService` spec extended: `getRelated`, `getBoughtTogether` post productId+count → data). Only pre-existing `img/a`,`img/b` 404 web-server warnings.
- Admin untouched (no admin-facing change this phase).

## Known gaps / deferred
- **No fuzzy/tolerant matching** (typos, stemming, pluralization) — exact substring only. A future phase could add Levenshtein/Soundex filtering or index-based search.
- **Autocomplete is whole-term substring**, not incremental prefix ranking on compound phrases.
- **Discovery caching**: related/bought-together recompute per page view; fine at this scale, could cache per product.
- **Section placement/limited controls**: fixed count (8), no shuffle, no "load more" — consistent with Phase 7 decisions.
- **Search results page still a single bundled listing** (ranked); no faceted "did you mean" or highlight snippets.
