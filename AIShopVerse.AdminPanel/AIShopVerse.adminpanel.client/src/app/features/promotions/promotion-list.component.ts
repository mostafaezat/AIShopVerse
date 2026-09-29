import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PromotionService } from '../../core/services/promotion.service';

@Component({
  selector: 'app-promotion-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="promotions-container">
      <h2>Promotions & Coupons</h2>
      <button (click)="showForm = true">Add Coupon</button>
      <table>
        <thead><tr><th>Code</th><th>Type</th><th>Value</th><th>Min Order</th><th>Uses</th><th>Valid From</th><th>Valid To</th><th>Active</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="9">Loading coupons...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="9">Unable to load coupons. <button (click)="loadCoupons()">Retry</button></td></tr>
          <tr *ngIf="!loading && !loadError && coupons.length === 0"><td colspan="9">No coupons found.</td></tr>
          <tr *ngFor="let coupon of coupons">
            <td>{{ coupon.code }}</td>
            <td>{{ coupon.discountType === 0 ? 'Percentage' : 'Fixed' }}</td>
            <td>{{ coupon.discountValue }}</td>
            <td>{{ coupon.minOrderValue || '-' }}</td>
            <td>{{ coupon.usedCount }}/{{ coupon.maxUses || '∞' }}</td>
            <td>{{ coupon.validFrom | date }}</td>
            <td>{{ coupon.validTo | date }}</td>
            <td>{{ coupon.isActive ? 'Yes' : 'No' }}</td>
            <td><button (click)="toggleCoupon(coupon)" [disabled]="togglingId === coupon.id">
                  {{ togglingId === coupon.id ? '...' : (coupon.isActive ? 'Deactivate' : 'Activate') }}
                </button>
                <button (click)="deleteCoupon(coupon.id)">Delete</button></td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="showForm" class="modal">
        <h3>Add Coupon</h3>
        <form (ngSubmit)="saveCoupon()">
          <div><label>Code</label><input [(ngModel)]="newCoupon.code" name="code" required></div>
          <div><label>Description</label><input [(ngModel)]="newCoupon.description" name="description"></div>
          <div><label>Type</label>
            <select [(ngModel)]="newCoupon.discountType" name="discountType">
              <option value="0">Percentage</option>
              <option value="1">Fixed</option>
            </select>
          </div>
          <div><label>Value</label><input type="number" [(ngModel)]="newCoupon.discountValue" name="discountValue" required></div>
          <div><label>Min Order</label><input type="number" [(ngModel)]="newCoupon.minOrderValue" name="minOrderValue"></div>
          <div><label>Max Uses</label><input type="number" [(ngModel)]="newCoupon.maxUses" name="maxUses"></div>
          <div><label>Valid From</label><input type="date" [(ngModel)]="newCoupon.validFrom" name="validFrom" required></div>
          <div><label>Valid To</label><input type="date" [(ngModel)]="newCoupon.validTo" name="validTo" required></div>
          <button type="submit" [disabled]="saving">{{ saving ? 'Saving...' : 'Save' }}</button>
          <button type="button" (click)="showForm = false" [disabled]="saving">Cancel</button>
        </form>
      </div>
    </div>
  `
})
export class PromotionListComponent implements OnInit {
  coupons: any[] = [];
  showForm = false;
  loading = true;
  loadError = false;
  saving = false;
  togglingId: string | null = null;
  newCoupon: any = { code: '', description: '', discountType: 0, discountValue: 0, minOrderValue: 0, maxUses: 0, validFrom: '', validTo: '' };
  constructor(private promotionService: PromotionService) {}
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
  saveCoupon() {
    if (this.saving) return;
    if (!(this.newCoupon.code || '').trim()) { alert('Coupon code is required.'); return; }
    if (this.newCoupon.discountValue == null || Number(this.newCoupon.discountValue) <= 0) { alert('Discount value must be greater than 0.'); return; }
    if (Number(this.newCoupon.discountType) === 0 && Number(this.newCoupon.discountValue) > 100) { alert('Percentage discount cannot exceed 100.'); return; }
    if (!this.newCoupon.validFrom || !this.newCoupon.validTo) { alert('Valid-from and valid-to dates are required.'); return; }
    if (new Date(this.newCoupon.validTo) < new Date(this.newCoupon.validFrom)) { alert('Valid-to date cannot be before valid-from date.'); return; }
    if (this.newCoupon.maxUses != null && Number(this.newCoupon.maxUses) < 0) { alert('Max uses cannot be negative.'); return; }
    const coupon = { ...this.newCoupon, discountType: parseInt(this.newCoupon.discountType) };
    this.saving = true;
    this.promotionService.add(coupon).subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.newCoupon = { code: '', description: '', discountType: 0, discountValue: 0, minOrderValue: 0, maxUses: 0, validFrom: '', validTo: '' };
        alert('Coupon added.');
        this.loadCoupons();
      },
      error: (err: any) => { this.saving = false; alert(err?.error?.message || 'Failed to add coupon'); }
    });
  }
  deleteCoupon(id: string) {
    if (!confirm('Delete?')) return;
    this.promotionService.delete(id).subscribe({
      next: () => { alert('Coupon deleted.'); this.loadCoupons(); },
      error: (err: any) => alert(err?.error?.message || 'Failed to delete coupon')
    });
  }
  toggleCoupon(coupon: any) {
    if (this.togglingId === coupon.id) return;
    this.togglingId = coupon.id;
    this.promotionService.toggle(coupon.id).subscribe({
      next: () => {
        this.togglingId = null;
        coupon.isActive = !coupon.isActive;
      },
      error: (err: any) => {
        this.togglingId = null;
        alert(err?.error?.message || 'Failed to update coupon');
      }
    });
  }
}
