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
          <tr *ngFor="let r of reviews">
            <td>{{ r.userName }}</td>
            <td>{{ r.productName }}</td>
            <td>{{ r.rating }}</td>
            <td>{{ r.comment || '-' }}</td>
            <td>{{ r.isApproved ? 'Approved' : 'Pending' }}</td>
            <td>{{ r.createdAt | date: 'short' }}</td>
            <td>
              <button *ngIf="!r.isApproved" (click)="approve(r)">Approve</button>
              <button *ngIf="!r.isApproved" (click)="reject(r)">Reject</button>
              <button (click)="deleteReview(r)">Delete</button>
            </td>
          </tr>
          <tr *ngIf="!reviews.length">
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

  constructor(private reviewService: ReviewService) {}

  ngOnInit() { this.load(); }

  load() {
    this.reviewService.getAll(this.page, this.pageSize, this.approvedFilter).subscribe(res => {
      this.result = res.data;
      this.reviews = res.data?.items || [];
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
    this.reviewService.approve(r.id).subscribe(() => this.load());
  }

  reject(r: any) {
    this.reviewService.reject(r.id).subscribe(() => this.load());
  }

  deleteReview(r: any) {
    if (confirm('Delete this review?')) this.reviewService.delete(r.id).subscribe(() => this.load());
  }
}
