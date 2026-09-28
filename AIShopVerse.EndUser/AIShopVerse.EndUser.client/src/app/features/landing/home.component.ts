import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ProductService } from '../../core/services/product.service';
import { RecommendationService, RecommendedForYou } from '../../core/services/recommendation.service';
import { Product } from '../../core/models';
import { ProductSliderComponent } from '../../shared/product-slider.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, ProductSliderComponent],
  template: `
    <div class="hero-section">
      <div class="hero-content">
        <h1 class="hero-title">Welcome to AIShopVerse</h1>
        <p class="hero-subtitle">Discover amazing products tailored to you.</p>
        <a routerLink="/products" class="hero-btn">Browse Products</a>
      </div>
    </div>

    <div class="container py-4">
      <app-product-slider
        *ngIf="bestSellers.length"
        [products]="bestSellers"
        title="Best Sellers"
        [autoplayMs]="5000">
      </app-product-slider>

      <app-product-slider
        *ngIf="newArrivals.length"
        [products]="newArrivals"
        title="New Arrivals"
        [autoplayMs]="5000">
      </app-product-slider>

      <app-product-slider
        *ngIf="promotions.length"
        [products]="promotions"
        title="Promotions"
        [autoplayMs]="5000">
      </app-product-slider>

      <app-product-slider
        *ngIf="auth.isLoggedIn() && recommended && recommended.products.length"
        [products]="recommended.products"
        [title]="recommended.mode === 'Popular' ? 'Popular Right Now' : 'Recommended for You'"
        [autoplayMs]="5000">
      </app-product-slider>

      <p *ngIf="!auth.isLoggedIn()" class="text-center text-muted mt-4">
        <a routerLink="/login">Log in</a> to get personalized recommendations.
      </p>
    </div>
  `,
  styles: [`
    .hero-section {
      background: linear-gradient(135deg, #0d6efd 0%, #6610f2 100%);
      color: #fff; text-align: center; padding: 4rem 1rem;
    }
    .hero-title { font-size: 2.5rem; font-weight: 800; margin-bottom: 0.5rem; }
    .hero-subtitle { font-size: 1.1rem; opacity: 0.9; margin-bottom: 1.5rem; }
    .hero-btn {
      display: inline-block; padding: 12px 32px; background: #fff; color: #0d6efd;
      border-radius: 8px; text-decoration: none; font-weight: 600; transition: transform 0.2s;
    }
    .hero-btn:hover { transform: translateY(-2px); }

    @media (max-width: 576px) {
      .hero-section { padding: 2.5rem 1rem; }
      .hero-title { font-size: 1.75rem; }
      .hero-subtitle { font-size: 0.95rem; }
    }
  `]
})
export class HomeComponent implements OnInit {
  bestSellers: Product[] = [];
  newArrivals: Product[] = [];
  promotions: Product[] = [];
  recommended: RecommendedForYou | null = null;

  constructor(
    public auth: AuthService,
    private productService: ProductService,
    private recommendationService: RecommendationService
  ) {}

  ngOnInit() {
    this.productService.getBestSellers(12).subscribe({
      next: res => this.bestSellers = res || [],
      error: () => this.bestSellers = []
    });

    this.productService.getNewArrivals(12).subscribe({
      next: res => this.newArrivals = res || [],
      error: () => this.newArrivals = []
    });

    this.productService.getPromotions(12).subscribe({
      next: res => this.promotions = res || [],
      error: () => this.promotions = []
    });

    if (this.auth.isLoggedIn()) {
      this.recommendationService.getForMe(12).subscribe({
        next: res => this.recommended = res || null,
        error: () => this.recommended = null
      });
    }
  }
}
