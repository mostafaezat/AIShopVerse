import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BrandService } from '../../../core/services/brand.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-brand-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingComponent],
  templateUrl: './brand-form.component.html',
  styleUrl: './brand-form.component.scss'
})
export class BrandFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  brandId: string | null = null;
  saving = false;
  submitted = false;
  logoPreview: string | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private brandService: BrandService,
    private sweetAlert: SweetAlertService
  ) {
    this.form = this.fb.group({
      nameEN: ['', [Validators.required, Validators.maxLength(100)]],
      nameAR: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      logoUrl: ['', Validators.pattern('^https?://.+')],
      isActive: [true]
    });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEdit = true;
        this.brandId = id;
        this.loadBrand(id);
      }
    });

    this.form.get('logoUrl')?.valueChanges.subscribe(url => {
      if (url && this.isValidUrl(url)) {
        this.logoPreview = url;
      } else {
        this.logoPreview = null;
      }
    });
  }

  private isValidUrl(url: string): boolean {
    try {
      new URL(url);
      return url.startsWith('http');
    } catch {
      return false;
    }
  }

  loadBrand(id: string) {
    this.brandService.getById(id).subscribe({
      next: res => {
        const brand = res.data;
        this.form.patchValue({
          nameEN: brand.nameEN,
          nameAR: brand.nameAR,
          description: brand.description || '',
          logoUrl: brand.logoUrl || '',
          isActive: brand.isActive
        });
        if (brand.logoUrl) {
          this.logoPreview = brand.logoUrl;
        }
      },
      error: (err: any) => {
        this.sweetAlert.toastError(err?.error?.message || 'Failed to load brand');
        this.router.navigate(['/brands']);
      }
    });
  }

  get f() { return this.form.controls; }

  onSubmit() {
    this.submitted = true;
    if (this.form.invalid) return;

    const payload = {
      nameEN: this.f['nameEN'].value.trim(),
      nameAR: this.f['nameAR'].value.trim(),
      description: this.f['description'].value?.trim() || null,
      logoUrl: this.f['logoUrl'].value?.trim() || null,
      isActive: this.f['isActive'].value
    };

    this.saving = true;
    const call = this.isEdit && this.brandId
      ? this.brandService.update({ ...payload, id: this.brandId })
      : this.brandService.add(payload);

    call.subscribe({
      next: () => {
        this.saving = false;
        this.sweetAlert.toastSuccess(this.isEdit ? 'Brand updated.' : 'Brand created.');
        this.router.navigate(['/brands']);
      },
      error: (err: any) => {
        this.saving = false;
        this.sweetAlert.toastError(err?.error?.message || (this.isEdit ? 'Failed to update brand' : 'Failed to create brand'));
      }
    });
  }
}