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
          <tr *ngFor="let coupon of coupons">
            <td>{{ coupon.code }}</td>
            <td>{{ coupon.discountType === 0 ? 'Percentage' : 'Fixed' }}</td>
            <td>{{ coupon.discountValue }}</td>
            <td>{{ coupon.minOrderValue || '-' }}</td>
            <td>{{ coupon.usedCount }}/{{ coupon.maxUses || '∞' }}</td>
            <td>{{ coupon.validFrom | date }}</td>
            <td>{{ coupon.validTo | date }}</td>
            <td>{{ coupon.isActive ? 'Yes' : 'No' }}</td>
            <td><button (click)="toggleCoupon(coupon)">{{ coupon.isActive ? 'Deactivate' : 'Activate' }}</button>
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
          <button type="submit">Save</button>
          <button type="button" (click)="showForm = false">Cancel</button>
        </form>
      </div>
    </div>
  `
})
export class PromotionListComponent implements OnInit {
  coupons: any[] = [];
  showForm = false;
  newCoupon: any = { code: '', description: '', discountType: 0, discountValue: 0, minOrderValue: 0, maxUses: 0, validFrom: '', validTo: '' };
  constructor(private promotionService: PromotionService) {}
  ngOnInit() { this.loadCoupons(); }
  loadCoupons() { this.promotionService.getAll().subscribe(res => this.coupons = res.data || []); }
  saveCoupon() {
    const coupon = { ...this.newCoupon, discountType: parseInt(this.newCoupon.discountType) };
    this.promotionService.add(coupon).subscribe(() => { this.showForm = false; this.loadCoupons(); });
  }
  deleteCoupon(id: string) { if (confirm('Delete?')) this.promotionService.delete(id).subscribe(() => this.loadCoupons()); }
  toggleCoupon(coupon: any) {
    this.promotionService.toggle(coupon.id).subscribe(() => {
      coupon.isActive = !coupon.isActive;
    });
  }
}
