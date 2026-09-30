import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReviewService } from '../../core/services/review.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-review-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingComponent, EmptyStateComponent],
  templateUrl: './review-list.component.html',
  styleUrl: './review-list.component.scss'
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

  constructor(
    private reviewService: ReviewService,
    private sweetAlert: SweetAlertService
  ) {}

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

  async approve(r: any) {
    if (this.actionId) return;
    this.actionId = r.id;
    this.reviewService.approve(r.id).subscribe({
      next: () => {
        this.actionId = null;
        this.sweetAlert.toastSuccess('Review approved.');
        this.load();
      },
      error: (err: any) => {
        this.actionId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to approve review');
      }
    });
  }

  async reject(r: any) {
    if (this.actionId) return;
    const confirmed = await this.sweetAlert.confirm(
      'Reject Review',
      'Are you sure you want to reject this review?'
    );
    if (!confirmed) return;

    this.actionId = r.id;
    this.reviewService.reject(r.id).subscribe({
      next: () => {
        this.actionId = null;
        this.sweetAlert.toastSuccess('Review rejected.');
        this.load();
      },
      error: (err: any) => {
        this.actionId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to reject review');
      }
    });
  }

  async confirmDelete(r: any) {
    if (this.actionId) return;
    const confirmed = await this.sweetAlert.confirmDelete(`review by ${r.userName}`);
    if (!confirmed) return;

    this.actionId = r.id;
    this.reviewService.delete(r.id).subscribe({
      next: () => {
        this.actionId = null;
        this.sweetAlert.toastSuccess('Review deleted.');
        this.load();
      },
      error: (err: any) => {
        this.actionId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to delete review');
      }
    });
  }
}