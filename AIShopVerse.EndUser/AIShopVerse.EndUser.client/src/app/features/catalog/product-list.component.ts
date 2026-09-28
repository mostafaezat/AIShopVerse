import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ProductService } from '../../core/services/product.service';
import { CategoryService, CategoryWithCount } from '../../core/services/category.service';
import { BrandService, BrandWithCount } from '../../core/services/brand.service';
import { ProductFilterService, ProductFilterState } from '../../core/services/product-filter.service';
import { Product, PagedResult } from '../../core/models';
import { WishlistService } from '../../core/services/wishlist.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">Products <span class="text-muted fs-6" *ngIf="result">{{ result.totalItems }} item(s)</span></h2>

      <div class="d-flex flex-wrap gap-2 mb-4">
        <div class="flex-grow-1" style="max-width:420px;">
          <input type="text" class="form-control" placeholder="Search products..."
                 [(ngModel)]="searchTerm" (ngModelChange)="onSearch()">
        </div>
        <select class="form-select" style="width:220px;" [(ngModel)]="sortBy" (ngModelChange)="onFilterChange()">
          <option value="newest">Newest</option>
          <option value="price_asc">Price: Low to High</option>
          <option value="price_desc">Price: High to Low</option>
          <option value="rating">Top Rated</option>
        </select>
        <button class="btn btn-outline-secondary" (click)="clearFilters()">Clear Filters</button>
      </div>

      <div class="row">
        <aside class="col-lg-3 mb-4">
          <div class="card">
            <div class="card-header fw-bold">Filters</div>
            <div class="card-body">

              <div class="filter-section">
                <label class="filter-label">Category</label>
                <div class="filter-list">
                  <a class="filter-item" [class.active]="categoryId === ''" (click)="selectCategory('')">
                    <span>All Categories</span>
                    <span class="badge bg-secondary">{{ allCategoryCount }}</span>
                  </a>
                  <a class="filter-item" *ngFor="let c of filteredCategories"
                     [class.active]="categoryId === c.id"
                     (click)="selectCategory(c.id)">
                    <span>{{ c.nameEN }}</span>
                    <span class="badge bg-secondary">{{ c.productCount }}</span>
                  </a>
                </div>
              </div>

              <div class="filter-section">
                <label class="filter-label">Brand</label>
                <div class="filter-list">
                  <a class="filter-item" [class.active]="brandId === ''" (click)="selectBrand('')">
                    <span>All Brands</span>
                    <span class="badge bg-secondary">{{ allBrandCount }}</span>
                  </a>
                  <a class="filter-item" *ngFor="let b of filteredBrands"
                     [class.active]="brandId === b.id"
                     (click)="selectBrand(b.id)">
                    <span>{{ b.nameEN }}</span>
                    <span class="badge bg-secondary">{{ b.productCount }}</span>
                  </a>
                </div>
              </div>

              <div class="filter-section">
                <label class="filter-label">Price</label>
                <div class="d-flex gap-2 align-items-center">
                  <input type="number" class="form-control form-control-sm" placeholder="Min" [(ngModel)]="minPrice" (ngModelChange)="onFilterChange()">
                  <span>—</span>
                  <input type="number" class="form-control form-control-sm" placeholder="Max" [(ngModel)]="maxPrice" (ngModelChange)="onFilterChange()">
                </div>
              </div>

              <div class="filter-section">
                <label class="filter-label">Minimum Rating</label>
                <div class="d-flex gap-1 flex-wrap" role="group">
                  <button *ngFor="let r of [0,1,2,3,4,5]" type="button"
                          class="btn btn-sm me-1"
                          [class.btn-warning]="minRating === r"
                          [class.btn-outline-secondary]="minRating !== r"
                          (click)="setRating(r)">
                    <ng-container *ngIf="r > 0">{{ r }}★</ng-container><ng-container *ngIf="r === 0">Any</ng-container>
                  </button>
                </div>
              </div>

              <div class="form-check mt-2">
                <input class="form-check-input" type="checkbox" id="inStock"
                       [(ngModel)]="inStockOnly" (ngModelChange)="onFilterChange()">
                <label class="form-check-label" for="inStock">In stock only</label>
              </div>
            </div>
          </div>
        </aside>

        <section class="col-lg-9">
          <div *ngIf="loading" class="text-center py-5 text-muted">Loading products...</div>

          <div *ngIf="!loading && products.length === 0" class="text-center py-5 text-muted">
            No products match your filters.
          </div>

          <div class="row row-cols-1 row-cols-sm-2 row-cols-md-3 g-3">
            <div class="col" *ngFor="let product of products">
              <a [routerLink]="['/products', product.id]" class="text-decoration-none text-dark">
                <div class="card h-100 product-card">
                  <button class="wishlist-heart"
                          [class.active]="wishlistSet.has(product.id)"
                          (click)="toggleWishlist(product.id, $event)"
                          title="Toggle wishlist">♥</button>
                  <img [src]="product.primaryImageUrl || 'https://via.placeholder.com/300x220?text=No+Image'"
                       class="card-img-top" style="height:180px;object-fit:contain;" [alt]="product.nameEN">
                  <div class="card-body">
                    <h6 class="card-title mb-1">{{ product.nameEN }}</h6>
                    <small class="text-muted" *ngIf="product.brandName">{{ product.brandName }}</small>
                    <div class="text-warning" *ngIf="product.averageRating">
                      {{ product.averageRating | number:'1.1-1' }} ★
                      <span class="text-muted fs-8">({{ product.reviewCount }})</span>
                    </div>
                    <div class="mt-2">
                      <span class="fw-bold text-primary" *ngIf="product.discountPrice">{{ product.discountPrice | currency }}</span>
                      <span class="fw-bold" *ngIf="!product.discountPrice">{{ product.price | currency }}</span>
                      <span class="text-muted text-decoration-line-through ms-2" *ngIf="product.discountPrice">{{ product.price | currency }}</span>
                    </div>
                    <span class="badge" [class.bg-success]="product.stockQuantity > 0" [class.bg-danger]="product.stockQuantity === 0">
                      {{ product.stockQuantity > 0 ? 'In Stock' : 'Out of Stock' }}
                    </span>
                  </div>
                </div>
              </a>
            </div>
          </div>

          <nav *ngIf="result && result.totalPages > 1" class="mt-4">
            <ul class="pagination justify-content-center">
              <li class="page-item" [class.disabled]="!result.hasPrevious">
                <button class="page-link" (click)="changePage(result.page - 1)">Previous</button>
              </li>
              <li class="page-item" *ngFor="let p of pages">
                <button class="page-link" [class.active]="p === result.page" (click)="changePage(p)">{{ p }}</button>
              </li>
              <li class="page-item" [class.disabled]="!result.hasNext">
                <button class="page-link" (click)="changePage(result.page + 1)">Next</button>
              </li>
            </ul>
          </nav>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .product-card { transition: transform .15s ease, box-shadow .15s ease; }
    .product-card:hover { transform: translateY(-3px); box-shadow: 0 4px 12px rgba(0,0,0,.12); }
    .fs-8 { font-size: .75rem; }
    .product-card { position: relative; }
    .wishlist-heart { position: absolute; top: 8px; right: 8px; width: 34px; height: 34px; z-index: 5;
      border-radius: 50%; border: none; background: rgba(255,255,255,.9); color: #bbb;
      font-size: 1.2rem; line-height: 1; cursor: pointer; display: flex; align-items: center;
      justify-content: center; transition: color .15s ease, transform .15s ease; }
    .wishlist-heart:hover { transform: scale(1.15); color: #dc3545; }
    .wishlist-heart.active { color: #dc3545; }
    .filter-section { margin-bottom: 1rem; }
    .filter-label { display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 6px; color: #333; }
    .filter-list { max-height: 220px; overflow-y: auto; }
    .filter-item {
      display: flex; justify-content: space-between; align-items: center;
      padding: 6px 10px; border-radius: 6px; cursor: pointer; font-size: 0.85rem;
      color: #333; text-decoration: none; transition: background 0.15s;
    }
    .filter-item:hover { background: #f0f0f0; }
    .filter-item.active { background: #e7f1ff; color: #0d6efd; font-weight: 600; }
    .filter-item .badge { font-size: 0.7rem; min-width: 28px; }
  `]
})
export class ProductListComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  result?: PagedResult<Product>;
  loading = false;

  filteredBrands: BrandWithCount[] = [];
  filteredCategories: CategoryWithCount[] = [];
  allBrandCount = 0;
  allCategoryCount = 0;

  searchTerm = '';
  sortBy = 'newest';
  categoryId = '';
  brandId = '';
  minPrice?: number;
  maxPrice?: number;
  minRating = 0;
  inStockOnly = false;

  pages: number[] = [];
  wishlistSet = new Set<string>();
  private sub?: Subscription;
  private wishlistSub?: Subscription;

  constructor(
    private productService: ProductService,
    private filterService: ProductFilterService,
    private categoryService: CategoryService,
    private brandService: BrandService,
    private route: ActivatedRoute,
    private router: Router,
    private wishlistService: WishlistService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadFilterOptions();
    this.loadWishlist();
    const queryCategory = this.route.snapshot.queryParamMap.get('category');
    const f = this.filterService.getFilter();
    this.searchTerm = f.searchTerm || '';
    this.sortBy = f.sortBy || 'newest';
    this.categoryId = f.categoryId || queryCategory || '';
    this.brandId = f.brandId || '';
    this.minPrice = f.minPrice;
    this.maxPrice = f.maxPrice;
    this.minRating = f.minRating || 0;
    this.inStockOnly = !!f.inStockOnly;

    this.sub = this.filterService.filter$.subscribe(filter => this.applyFilter(filter));
    if (!f.categoryId && queryCategory) {
      this.filterService.updateFilter({ categoryId: queryCategory });
    }
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
    this.wishlistSub?.unsubscribe();
  }

  loadFilterOptions() {
    this.brandService.getFiltered(this.categoryId || undefined).subscribe(brands => {
      this.filteredBrands = brands || [];
      this.allBrandCount = this.filteredBrands.reduce((sum, b) => sum + b.productCount, 0);
    });

    this.categoryService.getFiltered(this.brandId || undefined).subscribe(cats => {
      this.filteredCategories = cats || [];
      this.allCategoryCount = this.filteredCategories.reduce((sum, c) => sum + c.productCount, 0);
    });
  }

  loadWishlist() {
    if (!this.authService.isLoggedIn()) return;
    this.wishlistSub = this.wishlistService.getWishlist().subscribe({
      next: items => {
        this.wishlistSet = new Set((items || []).map(i => i.productId));
      },
      error: () => {}
    });
  }

  toggleWishlist(productId: string, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    const inWishlist = this.wishlistSet.has(productId);
    const target = inWishlist
      ? this.wishlistService.removeFromWishlist(productId)
      : this.wishlistService.addToWishlist(productId);
    target.subscribe({
      next: () => {
        if (inWishlist) {
          this.wishlistSet.delete(productId);
        } else {
          this.wishlistSet.add(productId);
        }
      },
      error: () => {}
    });
  }

  selectCategory(id: string) {
    this.categoryId = id;
    this.onFilterChange();
    this.loadFilterOptions();
  }

  selectBrand(id: string) {
    this.brandId = id;
    this.onFilterChange();
    this.loadFilterOptions();
  }

  onSearch() {
    this.filterService.updateFilter({ searchTerm: this.searchTerm || undefined });
  }

  onFilterChange() {
    this.filterService.updateFilter({
      categoryId: this.categoryId || undefined,
      brandId: this.brandId || undefined,
      minPrice: this.minPrice,
      maxPrice: this.maxPrice,
      minRating: this.minRating || undefined,
      inStockOnly: this.inStockOnly,
      sortBy: (this.sortBy as ProductFilterState['sortBy'])
    });
  }

  setRating(r: number) {
    this.minRating = r;
    this.onFilterChange();
  }

  changePage(page: number) {
    this.filterService.updatePage(page);
  }

  clearFilters() {
    this.searchTerm = '';
    this.sortBy = 'newest';
    this.categoryId = '';
    this.brandId = '';
    this.minPrice = undefined;
    this.maxPrice = undefined;
    this.minRating = 0;
    this.inStockOnly = false;
    this.filterService.updateFilter({
      searchTerm: undefined, categoryId: undefined, brandId: undefined,
      minPrice: undefined, maxPrice: undefined, minRating: undefined,
      inStockOnly: false, sortBy: 'newest'
    });
    this.loadFilterOptions();
  }

  private applyFilter(filter: ProductFilterState) {
    this.loading = true;
    this.productService.getFiltered(filter).subscribe({
      next: res => {
        this.result = res;
        this.products = res?.items || [];
        this.pages = this.buildPages(res);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.products = [];
      }
    });
  }

  private buildPages(res?: PagedResult<Product>): number[] {
    if (!res || res.totalPages <= 1) return [];
    const current = res.page;
    let start = Math.max(1, current - 2);
    const end = Math.min(res.totalPages, start + 4);
    start = Math.max(1, end - 4);
    const out: number[] = [];
    for (let i = start; i <= end; i++) out.push(i);
    return out;
  }
}
