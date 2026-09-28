import { Component, Input, OnInit, OnDestroy, ElementRef, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Product } from '../core/models';
import { CartService } from '../core/services/cart.service';
import { AuthService } from '../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-product-slider',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <section class="slider-section" *ngIf="products?.length">
      <div class="slider-header">
        <h2 class="slider-title">{{ title }}</h2>
        <div class="slider-controls">
          <button class="slider-btn" (click)="scrollPrev()" [disabled]="isAtStart" aria-label="Previous">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M15 18l-6-6 6-6"/></svg>
          </button>
          <button class="slider-btn" (click)="scrollNext()" [disabled]="isAtEnd" aria-label="Next">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 18l6-6-6-6"/></svg>
          </button>
        </div>
      </div>
      <div class="slider-track-wrapper">
        <div class="slider-track" #track
             (mousedown)="onDragStart($event)"
             (mousemove)="onDragMove($event)"
             (mouseup)="onDragEnd($event)"
             (mouseleave)="onDragEnd($event)"
             (touchstart)="onTouchStart($event)"
             (touchmove)="onTouchMove($event)"
             (touchend)="onTouchEnd($event)"
             (mouseenter)="onMouseEnter()"
             (mouseleave)="onMouseLeave()">
          <div class="slider-card" *ngFor="let product of products">
            <a [routerLink]="['/products', product.id]" class="product-link">
              <div class="product-image-wrap">
                <img [src]="product.primaryImageUrl || 'https://via.placeholder.com/300x220?text=No+Image'"
                     class="product-image" [alt]="product.nameEN" loading="lazy">
                <span class="badge-discount" *ngIf="product.discountPrice && product.discountPrice < product.price">
                  -{{ getDiscountPercent(product) }}%
                </span>
              </div>
              <div class="product-info">
                <h6 class="product-name">{{ product.nameEN }}</h6>
                <div class="product-rating" *ngIf="product.averageRating">
                  <span class="stars">{{ getStars(product.averageRating) }}</span>
                  <span class="review-count">({{ product.reviewCount }})</span>
                </div>
                <div class="product-price">
                  <span class="current-price">{{ (product.discountPrice || product.price) | currency }}</span>
                  <span class="original-price" *ngIf="product.discountPrice && product.discountPrice < product.price">{{ product.price | currency }}</span>
                </div>
              </div>
            </a>
            <button class="add-to-cart-btn"
                    [disabled]="product.stockQuantity === 0"
                    (click)="addToCart(product, $event)">
              {{ product.stockQuantity === 0 ? 'Out of Stock' : 'Add to Cart' }}
            </button>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .slider-section { margin-bottom: 2.5rem; }
    .slider-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .slider-title { font-size: 1.5rem; font-weight: 700; color: #1a1a2e; margin: 0; }
    .slider-controls { display: flex; gap: 0.5rem; }
    .slider-btn {
      width: 36px; height: 36px; border-radius: 50%; border: 1.5px solid #ddd;
      background: #fff; color: #333; cursor: pointer; display: flex;
      align-items: center; justify-content: center; transition: all 0.2s;
    }
    .slider-btn:hover:not(:disabled) { border-color: #0d6efd; color: #0d6efd; background: #f0f7ff; }
    .slider-btn:disabled { opacity: 0.3; cursor: default; }
    .slider-track-wrapper { overflow: hidden; position: relative; }
    .slider-track {
      display: flex; gap: 1rem; overflow-x: auto; scroll-behavior: smooth;
      scroll-snap-type: x mandatory; -webkit-overflow-scrolling: touch;
      scrollbar-width: none; padding: 0.5rem 0;
    }
    .slider-track::-webkit-scrollbar { display: none; }
    .slider-track.dragging { scroll-snap-type: none; cursor: grabbing; }
    .slider-card {
      flex: 0 0 calc(25% - 0.75rem); scroll-snap-align: start; position: relative;
      border: 1px solid #e9ecef; border-radius: 12px; overflow: hidden;
      background: #fff; transition: box-shadow 0.2s;
    }
    .slider-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .product-link { text-decoration: none; color: inherit; display: block; }
    .product-image-wrap {
      position: relative; padding-top: 75%; overflow: hidden; background: #f8f9fa;
    }
    .product-image {
      position: absolute; top: 0; left: 0; width: 100%; height: 100%;
      object-fit: contain; padding: 8px; transition: transform 0.3s;
    }
    .slider-card:hover .product-image { transform: scale(1.05); }
    .badge-discount {
      position: absolute; top: 8px; left: 8px; background: #dc3545; color: #fff;
      font-size: 0.7rem; font-weight: 600; padding: 2px 6px; border-radius: 4px;
    }
    .product-info { padding: 12px 12px 8px; }
    .product-name {
      font-size: 0.85rem; font-weight: 600; color: #1a1a2e; margin: 0 0 4px;
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;
      overflow: hidden; line-height: 1.3;
    }
    .product-rating { display: flex; align-items: center; gap: 4px; margin-bottom: 4px; }
    .stars { color: #f5a623; font-size: 0.8rem; letter-spacing: -1px; }
    .review-count { color: #888; font-size: 0.75rem; }
    .product-price { display: flex; align-items: baseline; gap: 6px; }
    .current-price { font-weight: 700; color: #0d6efd; font-size: 0.95rem; }
    .original-price { text-decoration: line-through; color: #aaa; font-size: 0.8rem; }
    .add-to-cart-btn {
      display: block; width: calc(100% - 24px); margin: 0 12px 12px; padding: 8px;
      border: none; border-radius: 8px; background: #0d6efd; color: #fff;
      font-size: 0.8rem; font-weight: 600; cursor: pointer; transition: background 0.2s;
    }
    .add-to-cart-btn:hover:not(:disabled) { background: #0b5ed7; }
    .add-to-cart-btn:disabled { background: #6c757d; cursor: default; }

    @media (max-width: 1200px) { .slider-card { flex: 0 0 calc(33.333% - 0.67rem); } }
    @media (max-width: 768px) { .slider-card { flex: 0 0 calc(50% - 0.5rem); } }
    @media (max-width: 480px) { .slider-card { flex: 0 0 calc(80% - 0.4rem); } }
  `]
})
export class ProductSliderComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() products: Product[] = [];
  @Input() title: string = '';
  @Input() autoplayMs: number = 0;

  @ViewChild('track') trackRef!: ElementRef<HTMLDivElement>;

  isAtStart = true;
  isAtEnd = false;

  private isDragging = false;
  private startX = 0;
  private scrollLeft = 0;
  private autoplayTimer: any = null;
  private resizeObserver: ResizeObserver | null = null;

  constructor(
    private cartService: CartService,
    public auth: AuthService,
    private toastr: ToastrService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {}

  ngAfterViewInit() {
    if (this.trackRef) {
      this.trackRef.nativeElement.addEventListener('scroll', this.updateScrollState.bind(this));
      this.updateScrollState();
      this.resizeObserver = new ResizeObserver(() => this.updateScrollState());
      this.resizeObserver.observe(this.trackRef.nativeElement);
      if (this.autoplayMs > 0) this.startAutoplay();
    }
  }

  ngOnDestroy() {
    this.stopAutoplay();
    if (this.resizeObserver) this.resizeObserver.disconnect();
  }

  getDiscountPercent(product: Product): number {
    if (!product.discountPrice || !product.price) return 0;
    return Math.round(((product.price - product.discountPrice) / product.price) * 100);
  }

  getStars(rating: number): string {
    const full = Math.floor(rating);
    const half = rating % 1 >= 0.5 ? 1 : 0;
    return '★'.repeat(full) + (half ? '½' : '') + '☆'.repeat(5 - full - half);
  }

  addToCart(product: Product, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    if (product.stockQuantity === 0) return;
    if (product.hasVariants) {
      this.router.navigate(['/products', product.id]);
      return;
    }
    this.cartService.addToCart(product.id, 1).subscribe({
      next: () => this.toastr.success('Added to cart', 'Success'),
      error: () => this.toastr.error('Failed to add to cart', 'Error')
    });
  }

  scrollPrev() {
    if (!this.trackRef) return;
    const cardWidth = this.trackRef.nativeElement.querySelector('.slider-card')?.clientWidth || 250;
    this.trackRef.nativeElement.scrollBy({ left: -cardWidth, behavior: 'smooth' });
  }

  scrollNext() {
    if (!this.trackRef) return;
    const cardWidth = this.trackRef.nativeElement.querySelector('.slider-card')?.clientWidth || 250;
    this.trackRef.nativeElement.scrollBy({ left: cardWidth, behavior: 'smooth' });
  }

  private updateScrollState() {
    if (!this.trackRef) return;
    const el = this.trackRef.nativeElement;
    this.isAtStart = el.scrollLeft <= 5;
    this.isAtEnd = el.scrollLeft >= el.scrollWidth - el.clientWidth - 5;
    this.cdr.detectChanges();
  }

  private startAutoplay() {
    this.stopAutoplay();
    this.autoplayTimer = setInterval(() => {
      if (!this.trackRef) return;
      const el = this.trackRef.nativeElement;
      if (el.scrollLeft >= el.scrollWidth - el.clientWidth - 5) {
        el.scrollTo({ left: 0, behavior: 'smooth' });
      } else {
        this.scrollNext();
      }
    }, this.autoplayMs);
  }

  private stopAutoplay() {
    if (this.autoplayTimer) {
      clearInterval(this.autoplayTimer);
      this.autoplayTimer = null;
    }
  }

  onMouseEnter() { if (this.autoplayMs > 0) this.stopAutoplay(); }
  onMouseLeave() { if (this.autoplayMs > 0) this.startAutoplay(); }

  onDragStart(e: MouseEvent) {
    if (!this.trackRef) return;
    this.isDragging = true;
    this.startX = e.pageX - this.trackRef.nativeElement.offsetLeft;
    this.scrollLeft = this.trackRef.nativeElement.scrollLeft;
    this.trackRef.nativeElement.classList.add('dragging');
  }

  onDragMove(e: MouseEvent) {
    if (!this.isDragging || !this.trackRef) return;
    e.preventDefault();
    const x = e.pageX - this.trackRef.nativeElement.offsetLeft;
    const walk = (x - this.startX) * 1.5;
    this.trackRef.nativeElement.scrollLeft = this.scrollLeft - walk;
  }

  onDragEnd(e: MouseEvent) {
    if (!this.isDragging || !this.trackRef) return;
    this.isDragging = false;
    this.trackRef.nativeElement.classList.remove('dragging');
    this.updateScrollState();
  }

  onTouchStart(e: TouchEvent) {
    if (!this.trackRef) return;
    this.isDragging = true;
    this.startX = e.touches[0].pageX - this.trackRef.nativeElement.offsetLeft;
    this.scrollLeft = this.trackRef.nativeElement.scrollLeft;
  }

  onTouchMove(e: TouchEvent) {
    if (!this.isDragging || !this.trackRef) return;
    const x = e.touches[0].pageX - this.trackRef.nativeElement.offsetLeft;
    const walk = (x - this.startX) * 1.5;
    this.trackRef.nativeElement.scrollLeft = this.scrollLeft - walk;
  }

  onTouchEnd(e: TouchEvent) {
    this.isDragging = false;
    this.updateScrollState();
  }
}
