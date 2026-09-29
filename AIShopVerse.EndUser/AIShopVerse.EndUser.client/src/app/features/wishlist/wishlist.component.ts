import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { WishlistService } from '../../core/services/wishlist.service';
import { WishlistItem } from '../../core/models';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container py-4">
      <h2 class="mb-4">My Wishlist</h2>

      <div *ngIf="loading" class="text-center py-5 text-muted">Loading your wishlist...</div>

      <div *ngIf="!loading && loadError" class="text-center py-5">
        <div class="alert alert-warning mx-auto" style="max-width:480px;">
          Unable to load your wishlist. Please try again.
          <div class="mt-2"><button class="btn btn-outline-secondary btn-sm" (click)="loadWishlist()">Retry</button></div>
        </div>
      </div>

      <div *ngIf="items.length === 0 && !loading && !loadError" class="text-center py-5">
        <h5>Your wishlist is empty.</h5>
        <a routerLink="/products" class="btn btn-primary mt-3">Browse Products</a>
      </div>

      <div class="row g-3" *ngIf="items.length > 0 && !loading && !loadError">
        <div class="col-md-4 col-lg-3" *ngFor="let item of items">
          <div class="card h-100 shadow-sm">
            <img [src]="item.productImageUrl || 'https://via.placeholder.com/200x200?text=No+Image'"
                 class="card-img-top" style="height:180px;object-fit:cover;" [alt]="item.productName">
            <div class="card-body d-flex flex-column">
              <h6 class="card-title">{{ item.productName }}</h6>
              <p class="text-primary fw-bold mb-2">{{ item.price | currency }}</p>
              <div class="mt-auto d-flex gap-2">
                <a [routerLink]="['/products', item.productId]" class="btn btn-sm btn-outline-primary flex-grow-1">View</a>
                <button class="btn btn-sm btn-outline-danger" (click)="remove(item)"
                        [disabled]="removingId === item.productId">
                  {{ removingId === item.productId ? 'Removing...' : 'Remove' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class WishlistComponent implements OnInit {
  items: WishlistItem[] = [];
  loading = true;
  loadError = false;
  removingId: string | null = null;

  constructor(private wishlistService: WishlistService, private toastr: ToastrService) {}

  ngOnInit() {
    this.loadWishlist();
  }

  loadWishlist() {
    this.loading = true;
    this.loadError = false;
    this.wishlistService.getWishlist().subscribe({
      next: items => {
        this.items = items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  remove(item: WishlistItem) {
    if (this.removingId === item.productId) return;
    this.removingId = item.productId;
    this.wishlistService.removeFromWishlist(item.productId).subscribe({
      next: () => {
        this.toastr.success('Removed from wishlist', 'Success');
        this.removingId = null;
        this.loadWishlist();
      },
      error: (err) => {
        this.removingId = null;
        this.toastr.error(err?.error?.message || 'Failed to remove item', 'Error');
      }
    });
  }
}
