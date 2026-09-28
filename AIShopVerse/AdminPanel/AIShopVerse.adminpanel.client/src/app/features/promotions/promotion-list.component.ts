import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PromotionService } from '../../core/services/promotion.service';
import { Coupon, DiscountType } from '../../core/models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-promotion-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Promotions</h2>
      <button class="btn btn-primary" (click)="toggleForm()">
        {{ showForm ? 'Cancel' : 'Add Promotion' }}
      </button>
    </div>

    <div *ngIf="showForm" class="card mb-4">
      <div class="card-body">
        <h5>{{ editingCoupon ? 'Edit Promotion' : 'Add Promotion' }}</h5>
        <form (ngSubmit)="saveCoupon()">
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">Code</label>
              <input type="text" class="form-control" [(ngModel)]="formData.code" name="code" required>
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Description</label>
              <input type="text" class="form-control" [(ngModel)]="formData.description" name="description">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Discount Type</label>
              <select class="form-select" [(ngModel)]="formData.discountType" name="discountType" required>
                <option [value]="0">Percentage</option>
                <option [value]="1">Fixed</option>
              </select>
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Discount Value</label>
              <input type="number" class="form-control" [(ngModel)]="formData.discountValue" name="discountValue" required>
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Min Order Value</label>
              <input type="number" class="form-control" [(ngModel)]="formData.minOrderValue" name="minOrderValue">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Max Uses</label>
              <input type="number" class="form-control" [(ngModel)]="formData.maxUses" name="maxUses">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Valid From</label>
              <input type="date" class="form-control" [(ngModel)]="formData.validFrom" name="validFrom">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Valid To</label>
              <input type="date" class="form-control" [(ngModel)]="formData.validTo" name="validTo">
            </div>
            <div class="col-md-12 mb-3">
              <div class="form-check">
                <input type="checkbox" class="form-check-input" [(ngModel)]="formData.isActive" name="isActive" id="isActive">
                <label class="form-check-label" for="isActive">Active</label>
              </div>
            </div>
          </div>
          <button type="submit" class="btn btn-success me-2">Save</button>
          <button type="button" class="btn btn-secondary" (click)="toggleForm()">Cancel</button>
        </form>
      </div>
    </div>

    <div class="table-responsive">
      <table class="table table-striped table-hover">
        <thead class="table-dark">
          <tr>
            <th>Code</th>
            <th>Discount Type</th>
            <th>Discount Value</th>
            <th>Valid From</th>
            <th>Valid To</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let coupon of coupons">
            <td>{{ coupon.code }}</td>
            <td>{{ coupon.discountType === 0 ? 'Percentage' : 'Fixed' }}</td>
            <td>{{ coupon.discountValue }}</td>
            <td>{{ coupon.validFrom | date:'shortDate' }}</td>
            <td>{{ coupon.validTo | date:'shortDate' }}</td>
            <td>
              <span class="badge" [class.bg-success]="coupon.isActive" [class.bg-secondary]="!coupon.isActive">
                {{ coupon.isActive ? 'Yes' : 'No' }}
              </span>
            </td>
            <td>
              <button class="btn btn-sm btn-warning me-1" (click)="editCoupon(coupon)">Edit</button>
              <button class="btn btn-sm btn-danger" (click)="deleteCoupon(coupon.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class PromotionListComponent implements OnInit {
  coupons: Coupon[] = [];
  showForm = false;
  editingCoupon: Coupon | null = null;
  formData: any = {};

  constructor(private promotionService: PromotionService) {}

  ngOnInit(): void {
    this.loadCoupons();
  }

  loadCoupons(): void {
    this.promotionService.getAll().subscribe(c => this.coupons = c);
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.editingCoupon = null;
      this.formData = {};
    }
  }

  editCoupon(coupon: Coupon): void {
    this.editingCoupon = coupon;
    this.formData = { ...coupon };
    this.showForm = true;
  }

  saveCoupon(): void {
    if (this.editingCoupon) {
      this.promotionService.update(this.formData).subscribe(() => {
        this.loadCoupons();
        this.toggleForm();
      });
    } else {
      this.promotionService.add(this.formData).subscribe(() => {
        this.loadCoupons();
        this.toggleForm();
      });
    }
  }

  deleteCoupon(id: string): void {
    Swal.fire({
      title: 'Are you sure?',
      text: 'This promotion will be deleted.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Delete'
    }).then((result) => {
      if (result.isConfirmed) {
        this.promotionService.delete(id).subscribe(() => {
          this.loadCoupons();
          Swal.fire('Deleted!', 'Promotion has been deleted.', 'success');
        });
      }
    });
  }
}
