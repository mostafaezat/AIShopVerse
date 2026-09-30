import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { BrandService } from '../../../core/services/brand.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingComponent],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  productId: string | null = null;
  saving = false;
  submitted = false;
  categories: any[] = [];
  brands: any[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private categoryService: CategoryService,
    private brandService: BrandService,
    private sweetAlert: SweetAlertService
  ) {
    this.form = this.fb.group({
      nameEN: ['', [Validators.required, Validators.maxLength(200)]],
      nameAR: ['', [Validators.required, Validators.maxLength(200)]],
      sku: ['', [Validators.required, Validators.maxLength(50)]],
      price: ['', [Validators.required, Validators.min(0.01)]],
      discountPrice: [null, Validators.min(0)],
      stockQuantity: ['', [Validators.required, Validators.min(0)]],
      categoryId: ['', Validators.required],
      brandId: [null],
      descriptionEN: ['', Validators.maxLength(2000)],
      descriptionAR: ['', Validators.maxLength(2000)],
      imageUrls: this.fb.array([]),
      isActive: [true],
      lowStockThreshold: [null, Validators.min(0)],
      variants: this.fb.array([])
    }, { validators: [this.discountPriceValidator] });
  }

  ngOnInit() {
    this.categoryService.getAll().subscribe(res => this.categories = res.data || []);
    this.brandService.getAll().subscribe(res => this.brands = res.data || []);

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEdit = true;
        this.productId = id;
        this.loadProduct(id);
      }
    });
  }

  private discountPriceValidator(control: AbstractControl): ValidationErrors | null {
    const price = control.get('price')?.value;
    const discountPrice = control.get('discountPrice')?.value;
    if (discountPrice !== null && discountPrice !== '' && price !== null && price !== '') {
      if (Number(discountPrice) >= Number(price)) {
        return { discountPriceInvalid: true };
      }
    }
    return null;
  }

  get f() { return this.form.controls; }

  get variantsArray() {
    return this.f['variants'] as FormArray;
  }

  get imageUrlsArray() {
    return this.f['imageUrls'] as FormArray;
  }

  loadProduct(id: string) {
    this.productService.getById(id).subscribe({
      next: res => {
        const p = res.data;
        if (!p) {
          this.sweetAlert.toastError('Product not found');
          this.router.navigate(['/products']);
          return;
        }
        this.form.patchValue({
          nameEN: p.nameEN || '',
          nameAR: p.nameAR || '',
          sku: p.sku || '',
          price: p.price || 0,
          discountPrice: p.discountPrice ?? null,
          stockQuantity: p.stockQuantity || 0,
          categoryId: p.categoryId || '',
          brandId: p.brandId || null,
          descriptionEN: p.descriptionEN || '',
          descriptionAR: p.descriptionAR || '',
          isActive: p.isActive !== false,
          lowStockThreshold: p.lowStockThreshold ?? null
        });

        // Set image URLs
        const images = (p.images || []).map((img: any) => img.imageUrl);
        this.setImageUrls(images);

        // Set variants
        const variants = (p.variants || []).filter((v: any) => v.isActive).map((v: any) => ({
          size: v.size || '',
          color: v.color || '',
          sku: v.sku || '',
          price: v.price || 0,
          stockQuantity: v.stockQuantity || 0
        }));
        this.setVariants(variants);
      },
      error: (err: any) => {
        this.sweetAlert.toastError(err?.error?.message || 'Failed to load product');
        this.router.navigate(['/products']);
      }
    });
  }

  setImageUrls(urls: string[]) {
    const urlsArray = this.imageUrlsArray;
    urlsArray.clear();
    urls.forEach(url => urlsArray.push(this.fb.control(url, Validators.pattern('^https?://.+'))));
  }

  setVariants(variants: any[]) {
    const variantsArray = this.variantsArray;
    variantsArray.clear();
    variants.forEach(v => {
      variantsArray.push(this.fb.group({
        size: [v.size || '', Validators.maxLength(20)],
        color: [v.color || '', Validators.maxLength(30)],
        sku: [v.sku || '', Validators.maxLength(50)],
        price: [v.price || 0, Validators.min(0)],
        stockQuantity: [v.stockQuantity || 0, Validators.min(0)]
      }));
    });
  }

  addVariant() {
    this.variantsArray.push(this.fb.group({
      size: ['', Validators.maxLength(20)],
      color: ['', Validators.maxLength(30)],
      sku: ['', Validators.maxLength(50)],
      price: [0, Validators.min(0)],
      stockQuantity: [0, Validators.min(0)]
    }));
  }

  removeVariant(index: number) {
    this.variantsArray.removeAt(index);
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || !input.files.length) return;

    const files = Array.from(input.files);
    const maxFiles = 10 - this.imageUrlsArray.length;
    if (files.length > maxFiles) {
      this.sweetAlert.toastWarning(`Maximum 10 images allowed. Only first ${maxFiles} will be uploaded.`);
      files.splice(maxFiles);
    }

    files.forEach(file => {
      this.productService.uploadImage(file).subscribe({
        next: res => {
          if (res.data) {
            this.imageUrlsArray.push(this.fb.control(res.data, Validators.pattern('^https?://.+')));
          }
        },
        error: (err: any) => {
          this.sweetAlert.toastError(err?.error?.message || 'Failed to upload image');
        }
      });
    });
    (event.target as HTMLInputElement).value = '';
  }

  removeImage(index: number) {
    this.imageUrlsArray.removeAt(index);
  }

  onSubmit() {
    this.submitted = true;
    this.form.updateValueAndValidity();
    if (this.form.invalid) return;

    const payload = {
      nameEN: this.f['nameEN'].value.trim(),
      nameAR: this.f['nameAR'].value.trim(),
      sku: this.f['sku'].value.trim(),
      price: Number(this.f['price'].value),
      discountPrice: this.f['discountPrice'].value ? Number(this.f['discountPrice'].value) : null,
      stockQuantity: Number(this.f['stockQuantity'].value),
      categoryId: this.f['categoryId'].value,
      brandId: this.f['brandId'].value || null,
      descriptionEN: this.f['descriptionEN'].value?.trim() || null,
      descriptionAR: this.f['descriptionAR'].value?.trim() || null,
      imageUrls: this.imageUrlsArray.value,
      isActive: this.f['isActive'].value,
      lowStockThreshold: this.f['lowStockThreshold'].value ?? null,
      variants: this.variantsArray.value.map((v: any) => ({
        size: v.size?.trim() || '',
        color: v.color?.trim() || '',
        sku: v.sku?.trim() || '',
        price: Number(v.price) || 0,
        stockQuantity: Number(v.stockQuantity) || 0,
        isActive: true
      }))
    };

    this.saving = true;
    const call = this.isEdit && this.productId
      ? this.productService.update({ ...payload, id: this.productId })
      : this.productService.add(payload);

    call.subscribe({
      next: () => {
        this.saving = false;
        this.sweetAlert.toastSuccess(this.isEdit ? 'Product updated.' : 'Product created.');
        this.router.navigate(['/products']);
      },
      error: (err: any) => {
        this.saving = false;
        this.sweetAlert.toastError(err?.error?.message || (this.isEdit ? 'Failed to update product' : 'Failed to create product'));
      }
    });
  }
}