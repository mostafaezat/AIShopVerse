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

      <div *ngIf="items.length === 0" class="text-center py-5">
        <h5>Your wishlist is empty.</h5>
        <a routerLink="/products" class="btn btn-primary mt-3">Browse Products</a>
      </div>

      <div class="row g-3" *ngIf="items.length > 0">
        <div class="col-md-4 col-lg-3" *ngFor="let item of items">
          <div class="card h-100 shadow-sm">
            <img [src]="item.productImageUrl || 'https://via.placeholder.com/200x200?text=No+Image'"
                 class="card-img-top" style="height:180px;object-fit:cover;" [alt]="item.productName">
            <div class="card-body d-flex flex-column">
              <h6 class="card-title">{{ item.productName }}</h6>
              <p class="text-primary fw-bold mb-2">{{ item.price | currency }}</p>
              <div class="mt-auto d-flex gap-2">
                <a [routerLink]="['/products', item.productId]" class="btn btn-sm btn-outline-primary flex-grow-1">View</a>
                <button class="btn btn-sm btn-outline-danger" (click)="remove(item)">Remove</button>
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

  constructor(private wishlistService: WishlistService, private toastr: ToastrService) {}

  ngOnInit() {
    this.loadWishlist();
  }

  loadWishlist() {
    this.wishlistService.getWishlist().subscribe(items => {
      this.items = items;
    });
  }

  remove(item: WishlistItem) {
    this.wishlistService.removeFromWishlist(item.productId).subscribe({
      next: () => {
        this.toastr.success('Removed from wishlist', 'Success');
        this.loadWishlist();
      },
      error: () => this.toastr.error('Failed to remove item', 'Error')
    });
  }
}
