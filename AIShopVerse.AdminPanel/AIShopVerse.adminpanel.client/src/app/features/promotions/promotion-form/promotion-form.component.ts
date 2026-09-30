import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PromotionService } from '../../../core/services/promotion.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-promotion-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingComponent],
  templateUrl: './promotion-form.component.html',
  styleUrl: './promotion-form.component.scss'
})
export class PromotionFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  couponId: string | null = null;
  saving = false;
  submitted = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private promotionService: PromotionService,
    private sweetAlert: SweetAlertService
  ) {
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      description: ['', Validators.maxLength(500)],
      discountType: [0, Validators.required],
      discountValue: ['', [Validators.required, Validators.min(0.01)]],
      minOrderValue: [0, Validators.min(0)],
      maxUses: [null, Validators.min(1)],
      validFrom: ['', Validators.required],
      validTo: ['', Validators.required],
      isActive: [true]
    }, { validators: [this.dateOrderValidator, this.percentageValidator] });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEdit = true;
        this.couponId = id;
        this.loadCoupon(id);
      }
    });
  }

  get f() { return this.form.controls; }

  private dateOrderValidator(control: AbstractControl): ValidationErrors | null {
    const from = control.get('validFrom')?.value;
    const to = control.get('validTo')?.value;
    if (from && to && new Date(to) < new Date(from)) {
      return { dateOrder: true };
    }
    return null;
  }

  private percentageValidator(control: AbstractControl): ValidationErrors | null {
    const type = control.get('discountType')?.value;
    const value = control.get('discountValue')?.value;
    if (type === 0 && value > 100) {
      return { maxPercentage: true };
    }
    return null;
  }

  loadCoupon(id: string) {
    this.promotionService.getById(id).subscribe({
      next: res => {
        const coupon = res.data;
        this.form.patchValue({
          code: coupon.code,
          description: coupon.description || '',
          discountType: coupon.discountType,
          discountValue: coupon.discountValue,
          minOrderValue: coupon.minOrderValue || 0,
          maxUses: coupon.maxUses || null,
          validFrom: this.formatDateForInput(coupon.validFrom),
          validTo: this.formatDateForInput(coupon.validTo),
          isActive: coupon.isActive
        });
      },
      error: (err: any) => {
        this.sweetAlert.toastError(err?.error?.message || 'Failed to load coupon');
        this.router.navigate(['/promotions']);
      }
    });
  }

  private formatDateForInput(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toISOString().slice(0, 16);
  }

  onSubmit() {
    this.submitted = true;
    this.form.updateValueAndValidity();
    if (this.form.invalid) return;

    const payload = {
      code: this.f['code'].value.trim().toUpperCase(),
      description: this.f['description'].value?.trim() || null,
      discountType: this.f['discountType'].value,
      discountValue: this.f['discountValue'].value,
      minOrderValue: this.f['minOrderValue'].value || 0,
      maxUses: this.f['maxUses'].value || null,
      validFrom: new Date(this.f['validFrom'].value).toISOString(),
      validTo: new Date(this.f['validTo'].value).toISOString(),
      isActive: this.f['isActive'].value
    };

    this.saving = true;
    const call = this.isEdit && this.couponId
      ? this.promotionService.update({ ...payload, id: this.couponId })
      : this.promotionService.add(payload);

    call.subscribe({
      next: () => {
        this.saving = false;
        this.sweetAlert.toastSuccess(this.isEdit ? 'Coupon updated.' : 'Coupon created.');
        this.router.navigate(['/promotions']);
      },
      error: (err: any) => {
        this.saving = false;
        this.sweetAlert.toastError(err?.error?.message || (this.isEdit ? 'Failed to update coupon' : 'Failed to create coupon'));
      }
    });
  }
}