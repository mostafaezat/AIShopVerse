import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PromotionService } from '../../core/services/promotion.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-promotion-list',
  standalone: true,
  imports: [CommonModule, LoadingComponent, EmptyStateComponent],
  templateUrl: './promotion-list.component.html',
  styleUrl: './promotion-list.component.scss'
})
export class PromotionListComponent implements OnInit {
  coupons: any[] = [];
  loading = true;
  loadError = false;
  togglingId: string | null = null;

  constructor(
    private promotionService: PromotionService,
    private router: Router,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() { this.loadCoupons(); }

  loadCoupons() {
    this.loading = true;
    this.loadError = false;
    this.promotionService.getAll().subscribe({
      next: res => {
        this.coupons = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.coupons = [];
      }
    });
  }

  navigateToAdd() {
    this.router.navigate(['/promotions/add']);
  }

  navigateToEdit(id: string) {
    this.router.navigate(['/promotions/edit', id]);
  }

  async toggleCoupon(coupon: any) {
    if (this.togglingId === coupon.id) return;
    this.togglingId = coupon.id;

    this.promotionService.toggle(coupon.id).subscribe({
      next: () => {
        this.togglingId = null;
        coupon.isActive = !coupon.isActive;
        this.sweetAlert.toastSuccess(coupon.isActive ? 'Coupon activated.' : 'Coupon deactivated.');
      },
      error: (err: any) => {
        this.togglingId = null;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to update coupon');
      }
    });
  }

  async confirmDelete(coupon: any) {
    const confirmed = await this.sweetAlert.confirmDelete(`coupon "${coupon.code}"`);
    if (!confirmed) return;

    this.promotionService.delete(coupon.id).subscribe({
      next: () => {
        this.sweetAlert.toastSuccess('Coupon deleted.');
        this.loadCoupons();
      },
      error: (err: any) => this.sweetAlert.toastError(err?.error?.message || 'Failed to delete coupon')
    });
  }
}