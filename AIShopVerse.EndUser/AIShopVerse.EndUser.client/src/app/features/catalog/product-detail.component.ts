import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { ReviewService } from '../../core/services/review.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { LanguageService } from '../../core/services/language.service';
import { Product, ProductImage, Review } from '../../core/models';
import { PricePipe, LocalizedDatePipe } from '../../shared/pipes/localized-format.pipes';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TranslatePipe, PricePipe, LocalizedDatePipe],
  template: `
    <div class="container py-4">
      <div *ngIf="loading" class="text-center py-5 text-muted">{{ 'products.loading' | translate }}</div>

      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          {{ 'products.loadError' | translate }}
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadProduct()">{{ 'common.retry' | translate }}</button></div>
        </div>
      </div>

      <ng-container *ngIf="!loading && !loadError && product">
      <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
          <li class="breadcrumb-item"><a routerLink="/products">{{ 'navigation.products' | translate }}</a></li>
          <li class="breadcrumb-item active">{{ productName }}</li>
        </ol>
      </nav>

      <div class="row">
        <div class="col-md-6">
          <div class="card mb-3">
            <img [src]="primaryImage || 'https://via.placeholder.com/500x400?text=No+Image'"
                 class="card-img-top" style="max-height:400px;object-fit:contain;" [alt]="productName">
          </div>
          <div class="d-flex gap-2" *ngIf="product.images && product.images.length > 1">
            <img *ngFor="let img of product.images" [src]="img.imageUrl" class="img-thumbnail"
                 style="width:70px;height:70px;object-fit:cover;cursor:pointer;"
                 (click)="selectImage(img)" [alt]="productName">
          </div>
        </div>

        <div class="col-md-6">
          <h1 class="mb-2">{{ productName }}</h1>
          <div class="mb-2">
            <span *ngFor="let i of stars" class="text-warning fs-5">★</span>
            <span class="text-muted" *ngIf="product.reviewCount">({{ product.reviewCount }}) {{ 'productDetail.customerReviews' | translate }}</span>
          </div>
          <h3 class="text-primary">{{ displayPrice | price }}</h3>
          <p *ngIf="product.discountPrice && !activeVariants.length" class="text-muted"><s>{{ product.price | price }}</s></p>

          <div class="my-3">
            <p><strong>{{ 'productDetail.brand' | translate }}:</strong> {{ product.brandName || '—' }}</p>
            <p><strong>{{ 'productDetail.category' | translate }}:</strong> {{ product.categoryName }}</p>
            <p><strong>{{ 'productDetail.sku' | translate }}:</strong> {{ (activeVariants.length ? (selectedVariant?.sku || product.sku) : product.sku) }}</p>
            <p *ngIf="!activeVariants.length" [class.text-danger]="product.stockQuantity === 0">
              {{ product.stockQuantity > 0 ? ('productDetail.inStock' | translate: { count: product.stockQuantity }) : ('productDetail.outOfStock' | translate) }}
            </p>
            <p *ngIf="activeVariants.length && selectedVariant" [class.text-danger]="selectedVariant.stockQuantity === 0">
              {{ selectedVariant.stockQuantity > 0 ? ('productDetail.inStock' | translate: { count: selectedVariant.stockQuantity }) : ('productDetail.outOfStock' | translate) }}
            </p>
            <p *ngIf="activeVariants.length && !selectedVariant" class="text-danger">{{ 'productDetail.selectVariant' | translate }}</p>
          </div>

          <div class="mb-3" *ngIf="activeVariants.length">
            <label class="fw-semibold d-block mb-1">{{ 'productDetail.size' | translate }}</label>
            <select class="form-select mb-2" style="max-width:300px;" [(ngModel)]="selectedSize" (ngModelChange)="onSizeChange()">
              <option *ngFor="let s of sizes" [ngValue]="s">{{ s }}</option>
            </select>

            <label class="fw-semibold d-block mb-1">{{ 'productDetail.color' | translate }}</label>
            <select class="form-select mb-2" style="max-width:300px;" [(ngModel)]="selectedColor" (ngModelChange)="updateSelectedVariant()">
              <option *ngFor="let c of colors" [ngValue]="c">{{ c }}</option>
            </select>
          </div>

          <div class="d-flex gap-2 mb-3">
            <button class="btn btn-primary btn-lg"
                    *ngIf="auth.isLoggedIn()"
                    [disabled]="cartBusy || (activeVariants.length && !selectedVariant) || (activeVariants.length ? selectedVariantStock === 0 : product.stockQuantity === 0)"
                    (click)="addToCart()">{{ cartBusy ? ('productDetail.adding' | translate) : ('productDetail.addToCart' | translate) }}</button>
            <button class="btn btn-outline-danger btn-lg"
                    *ngIf="auth.isLoggedIn()"
                    [disabled]="wishlistBusy"
                    (click)="toggleWishlist()">{{ wishlistBusy ? ('productDetail.updating' | translate) : (inWishlist ? ('productDetail.removeFromWishlist' | translate) : ('productDetail.addToWishlist' | translate)) }}</button>
          </div>
          <p *ngIf="!auth.isLoggedIn() && product.stockQuantity > 0" class="text-muted">
            <a routerLink="/login">{{ 'productDetail.loginToAdd' | translate }}</a>
          </p>
        </div>
      </div>

      <div class="mt-4" *ngIf="productDescription">
        <h3>{{ 'productDetail.description' | translate }}</h3>
        <p>{{ productDescription }}</p>
      </div>

      <div class="mt-4" *ngIf="product.attributes && product.attributes.length">
        <h3>{{ 'productDetail.specifications' | translate }}</h3>
        <table class="table table-bordered">
          <tbody>
            <tr *ngFor="let attr of product.attributes">
              <th style="width:30%">{{ attr.name }}</th>
              <td>{{ attr.value }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mt-5" *ngIf="boughtTogether.length">
        <h3>{{ 'productDetail.frequentlyBoughtTogether' | translate }}</h3>
        <div class="row row-cols-2 row-cols-md-4 g-3">
          <div class="col" *ngFor="let p of boughtTogether">
            <a [routerLink]="['/products', p.id]" class="text-decoration-none text-dark">
              <div class="card h-100">
                <img [src]="p.primaryImageUrl || 'https://via.placeholder.com/300x200?text=No+Image'"
                     class="card-img-top" style="height:150px;object-fit:contain;" [alt]="localizedName(p)">
                <div class="card-body py-2">
                  <div class="fs-6 fw-semibold text-truncate">{{ localizedName(p) }}</div>
                  <div class="text-primary fw-bold">{{ (p.discountPrice ?? p.price) | price }}</div>
                </div>
              </div>
            </a>
          </div>
        </div>
      </div>

      <div class="mt-5" *ngIf="related.length">
        <h3>{{ 'productDetail.relatedProducts' | translate }}</h3>
        <div class="row row-cols-2 row-cols-md-4 g-3">
          <div class="col" *ngFor="let p of related">
            <a [routerLink]="['/products', p.id]" class="text-decoration-none text-dark">
              <div class="card h-100">
                <img [src]="p.primaryImageUrl || 'https://via.placeholder.com/300x200?text=No+Image'"
                     class="card-img-top" style="height:150px;object-fit:contain;" [alt]="localizedName(p)">
                <div class="card-body py-2">
                  <div class="fs-6 fw-semibold text-truncate">{{ localizedName(p) }}</div>
                  <div class="text-primary fw-bold">{{ (p.discountPrice ?? p.price) | price }}</div>
                </div>
              </div>
            </a>
          </div>
        </div>
      </div>

      <div class="mt-4">
        <h3>{{ 'productDetail.customerReviews' | translate }}</h3>

        <div *ngIf="auth.isLoggedIn()" class="card mb-3">
          <div class="card-body">
            <h5>{{ 'productDetail.writeReview' | translate }}</h5>
            <div class="mb-2">
              <button *ngFor="let s of [1,2,3,4,5]" type="button"
                      class="btn btn-sm me-1" [class.btn-warning]="s <= newRating"
                      [class.btn-outline-warning]="s > newRating" (click)="newRating = s"
                      [attr.aria-label]="s + '★'">{{ s }}★</button>
            </div>
            <textarea class="form-control mb-2" rows="3" [placeholder]="'productDetail.shareThoughts' | translate"
                      [(ngModel)]="newComment"></textarea>
            <button class="btn btn-primary" [disabled]="newRating === 0 || reviewBusy" (click)="submitReview()">
              {{ reviewBusy ? ('productDetail.submitting' | translate) : ('productDetail.submitReview' | translate) }}
            </button>
          </div>
        </div>

        <div *ngIf="reviews.length === 0" class="text-muted">{{ 'productDetail.noReviews' | translate }}</div>
        <div class="border-bottom py-3" *ngFor="let review of reviews">
          <div class="d-flex justify-content-between">
            <strong>{{ review.userName || ('productDetail.anonymous' | translate) }}</strong>
            <div>
              <small class="text-muted">{{ review.createdAt | localizedDate: 'medium' }}</small>
              <button *ngIf="isOwnReview(review)" class="btn btn-sm btn-outline-danger ms-2"
                      (click)="deleteReview(review)">{{ 'productDetail.deleteReview' | translate }}</button>
            </div>
          </div>
          <div class="text-warning">{{ '★'.repeat(review.rating) }}</div>
          <p class="mb-0 mt-1">{{ review.comment }}</p>
        </div>
      </div>
      </ng-container>
    </div>
  `
})
export class ProductDetailComponent implements OnInit {
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);
  product: Product | null = null;
  reviews: Review[] = [];
  primaryImage: string | null = null;
  newRating = 0;
  newComment = '';
  inWishlist = false;
  stars = [1, 2, 3, 4, 5];
  selectedImage: string | null = null;
  activeVariants: any[] = [];
  sizes: string[] = [];
  colors: string[] = [];
  selectedSize = '';
  selectedColor = '';
  selectedVariant: any = null;
  related: any[] = [];
  boughtTogether: any[] = [];
  loading = true;
  loadError = false;
  cartBusy = false;
  wishlistBusy = false;
  reviewBusy = false;

  constructor(
    private route: ActivatedRoute,
    private productService: ProductService,
    private cartService: CartService,
    private reviewService: ReviewService,
    private wishlistService: WishlistService,
    private recommendationService: RecommendationService,
    public auth: AuthService,
    private toastr: ToastrService
  ) {}

  get isArabic(): boolean {
    return this.languageService.currentLanguage() === 'ar';
  }

  get productName(): string {
    if (!this.product) return '';
    return this.isArabic ? (this.product.nameAR || this.product.nameEN || '') : (this.product.nameEN || this.product.nameAR || '');
  }

  get productDescription(): string {
    if (!this.product) return '';
    return this.isArabic
      ? (this.product.descriptionAR || this.product.descriptionEN || '')
      : (this.product.descriptionEN || this.product.descriptionAR || '');
  }

  localizedName(p: { nameAR?: string; nameEN?: string }): string {
    return this.isArabic ? (p.nameAR || p.nameEN || '') : (p.nameEN || p.nameAR || '');
  }

  ngOnInit() {
    this.loadProduct();
  }

  selectImage(image: ProductImage): void {
    this.selectedImage = image.imageUrl || null;
  }

  loadProduct() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading = true;
    this.loadError = false;
    this.productService.getById(id).subscribe({
      next: product => {
        this.product = product;
        this.loading = false;
        const primary = product.images?.find((i: ProductImage) => i.isPrimary);
        this.primaryImage = primary?.imageUrl || product.images?.[0]?.imageUrl || null;
        this.selectedImage = this.primaryImage;
        this.initVariants();
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
    this.loadReviews(id);
    this.loadDiscovery(id);
    if (this.auth.isLoggedIn()) {
      this.checkWishlist();
    }
  }

  loadDiscovery(productId: string) {
    this.recommendationService.getBoughtTogether(productId, 8).subscribe({
      next: items => this.boughtTogether = items || [],
      error: () => {}
    });
    this.recommendationService.getRelated(productId, 8).subscribe({
      next: items => this.related = items || [],
      error: () => {}
    });
  }

  variantLabel(v: any): string {
    return v.attributeValues || v.sku || '';
  }

  initVariants() {
    this.activeVariants = (this.product?.variants || []).filter((v: any) => v.isActive);
    this.sizes = [...new Set(this.activeVariants.map((v: any) => v.size).filter((s: string) => !!s))];
    this.selectedSize = this.sizes[0] ?? '';
    this.refreshColors();
  }

  refreshColors() {
    this.colors = this.sizes.length
      ? [...new Set(this.activeVariants.filter((v: any) => v.size === this.selectedSize).map((v: any) => v.color))]
      : [...new Set(this.activeVariants.map((v: any) => v.color))];
    if (!this.colors.includes(this.selectedColor)) {
      this.selectedColor = this.colors[0] ?? '';
    }
    this.updateSelectedVariant();
  }

  onSizeChange() {
    this.refreshColors();
  }

  updateSelectedVariant() {
    this.selectedVariant = this.activeVariants.find((v: any) =>
      v.size === this.selectedSize && v.color === this.selectedColor) || null;
  }

  get selectedVariantStock(): number {
    return this.selectedVariant?.stockQuantity ?? 0;
  }

  get displayPrice(): number {
    if (this.activeVariants.length) {
      return this.selectedVariant?.price ?? this.product?.price ?? 0;
    }
    return this.product?.discountPrice ?? this.product?.price ?? 0;
  }

  checkWishlist() {
    this.wishlistService.getWishlist().subscribe({
      next: items => {
        this.inWishlist = (items || []).some(i => i.productId === this.product?.id);
      },
      error: () => { this.inWishlist = false; }
    });
  }

  loadReviews(productId: string) {
    this.reviewService.getProductReviews(productId).subscribe({
      next: reviews => this.reviews = reviews || [],
      error: () => this.reviews = []
    });
  }

  addToCart() {
    if (this.cartBusy || !this.product) return;
    if (this.activeVariants.length && !this.selectedVariant) {
      this.toastr.warning(this.translate.instant('productDetail.selectVariant'), this.translate.instant('toast.warning'));
      return;
    }
    this.cartBusy = true;
    this.cartService.addToCart(this.product.id, 1, this.selectedVariant?.id).subscribe({
      next: () => {
        this.cartBusy = false;
        this.toastr.success(this.translate.instant('toast.addedToCart'), this.translate.instant('common.success'));
      },
      error: (err) => {
        this.cartBusy = false;
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
      }
    });
  }

  toggleWishlist() {
    if (this.wishlistBusy || !this.product) return;
    this.wishlistBusy = true;
    if (this.inWishlist) {
      this.wishlistService.removeFromWishlist(this.product.id).subscribe({
        next: () => {
          this.inWishlist = false;
          this.wishlistBusy = false;
          this.toastr.success(this.translate.instant('toast.removedFromWishlist'), this.translate.instant('common.success'));
        },
        error: (err) => {
          this.wishlistBusy = false;
          this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
        }
      });
    } else {
      this.wishlistService.addToWishlist(this.product.id).subscribe({
        next: () => {
          this.inWishlist = true;
          this.wishlistBusy = false;
          this.toastr.success(this.translate.instant('toast.addedToWishlist'), this.translate.instant('common.success'));
        },
        error: (err) => {
          this.wishlistBusy = false;
          this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'));
        }
      });
    }
  }

  submitReview() {
    if (this.reviewBusy || !this.product) return;
    if (this.newRating === 0) {
      this.toastr.warning(this.translate.instant('productDetail.ratingRequired'), this.translate.instant('toast.warning'));
      return;
    }
    const productId = this.product.id;
    this.reviewBusy = true;
    this.reviewService.createReview(productId, this.newRating, this.newComment).subscribe({
      next: () => {
        this.reviewBusy = false;
        this.toastr.success(this.translate.instant('toast.reviewSubmitted'), this.translate.instant('common.success'));
        this.newRating = 0;
        this.newComment = '';
        this.loadReviews(productId);
      },
      error: (err) => {
        this.reviewBusy = false;
        this.toastr.error(err?.error?.message || err?.message || this.translate.instant('productDetail.reviewFailed'), this.translate.instant('common.error'));
      }
    });
  }

  isOwnReview(review: Review): boolean {
    const uid = this.auth.currentUserId;
    return !!uid && review.userId === uid;
  }

  deleteReview(review: Review) {
    this.reviewService.deleteReview(review.id).subscribe({
      next: () => {
        this.toastr.success(this.translate.instant('toast.reviewDeleted'), this.translate.instant('common.success'));
        this.loadReviews(this.product!.id);
      },
      error: (err) => this.toastr.error(err?.error?.message || err?.message || this.translate.instant('toast.error'), this.translate.instant('common.error'))
    });
  }
}