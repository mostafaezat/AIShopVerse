import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReviewService } from '../../core/services/review.service';

@Component({
  selector: 'app-review-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="reviews-container">
      <h2>Review Moderation</h2>

      <div class="filters">
        <label>Status:</label>
        <select [(ngModel)]="approvedFilter" (ngModelChange)="reload()">
          <option [ngValue]="null">All</option>
          <option [ngValue]="true">Approved</option>
          <option [ngValue]="false">Pending</option>
        </select>
        <button (click)="reload()">Refresh</button>
      </div>

      <table>
        <thead>
          <tr><th>User</th><th>Product</th><th>Rating</th><th>Comment</th><th>Status</th><th>Date</th><th>Actions</th></tr>
        </thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="7">Loading reviews...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="7">Unable to load reviews. <button (click)="reload()">Retry</button></td></tr>
          <tr *ngFor="let r of reviews">
            <td>{{ r.userName }}</td>
            <td>{{ r.productName }}</td>
            <td>{{ r.rating }}</td>
            <td>{{ r.comment || '-' }}</td>
            <td>{{ r.isApproved ? 'Approved' : 'Pending' }}</td>
            <td>{{ r.createdAt | date: 'short' }}</td>
            <td>
              <button *ngIf="!r.isApproved" (click)="approve(r)" [disabled]="actionId === r.id">
                {{ actionId === r.id ? '...' : 'Approve' }}
              </button>
              <button *ngIf="!r.isApproved" (click)="reject(r)" [disabled]="actionId === r.id">Reject</button>
              <button (click)="deleteReview(r)" [disabled]="actionId === r.id">Delete</button>
            </td>
          </tr>
          <tr *ngIf="!loading && !loadError && !reviews.length">
            <td colspan="7">No reviews found.</td>
          </tr>
        </tbody>
      </table>

      <div class="pagination" *ngIf="result && result.totalPages > 1">
        <button [disabled]="!result.hasPrevious" (click)="changePage(page - 1)">Previous</button>
        <span>{{ page }} / {{ result.totalPages }}</span>
        <button [disabled]="!result.hasNext" (click)="changePage(page + 1)">Next</button>
      </div>
    </div>
  `
})
export class ReviewListComponent implements OnInit {
  reviews: any[] = [];
  result: any = null;
  page = 1;
  pageSize = 20;
  approvedFilter: boolean | null = null;
  loading = true;
  loadError = false;
  actionId: string | null = null;

  constructor(private reviewService: ReviewService) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.loadError = false;
    this.reviewService.getAll(this.page, this.pageSize, this.approvedFilter).subscribe({
      next: res => {
        this.result = res.data;
        this.reviews = res.data?.items || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.reviews = [];
      }
    });
  }

  reload() {
    this.page = 1;
    this.load();
  }

  changePage(p: number) {
    this.page = p;
    this.load();
  }

  approve(r: any) {
    if (this.actionId) return;
    this.actionId = r.id;
    this.reviewService.approve(r.id).subscribe({
      next: () => { this.actionId = null; alert('Review approved.'); this.load(); },
      error: (err: any) => { this.actionId = null; alert(err?.error?.message || 'Failed to approve review'); }
    });
  }

  reject(r: any) {
    if (this.actionId) return;
    if (!confirm('Reject this review?')) return;
    this.actionId = r.id;
    this.reviewService.reject(r.id).subscribe({
      next: () => { this.actionId = null; alert('Review rejected.'); this.load(); },
      error: (err: any) => { this.actionId = null; alert(err?.error?.message || 'Failed to reject review'); }
    });
  }

  deleteReview(r: any) {
    if (this.actionId) return;
    if (!confirm('Delete this review?')) return;
    this.actionId = r.id;
    this.reviewService.delete(r.id).subscribe({
      next: () => { this.actionId = null; alert('Review deleted.'); this.load(); },
      error: (err: any) => { this.actionId = null; alert(err?.error?.message || 'Failed to delete review'); }
    });
  }
}
