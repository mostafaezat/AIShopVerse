import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { BrandService } from '../../core/services/brand.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="products-container">
      <h2>Products</h2>
      <div class="actions">
        <input [(ngModel)]="searchTerm" placeholder="Search..." (keyup.enter)="loadProducts()">
        <button (click)="loadProducts()">Search</button>
        <button (click)="openNew()">+ Add Product</button>
      </div>

      <table>
        <thead><tr><th>Image</th><th>Name</th><th>SKU</th><th>Price</th><th>Stock</th><th>Category</th><th>Brand</th><th>Active</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="9">Loading products...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="9">Unable to load products. <button (click)="loadProducts()">Retry</button></td></tr>
          <tr *ngFor="let product of products">
            <td><img [src]="product.primaryImageUrl || 'https://via.placeholder.com/50?text=No+Image'" style="width:50px;height:50px;object-fit:contain;"></td>
            <td>{{ product.nameEN }}</td>
            <td>{{ product.sku }}</td>
            <td>
              <span *ngIf="product.discountPrice" style="text-decoration:line-through;color:#999;">{{ product.price | currency }}</span>
              <strong>{{ (product.discountPrice ?? product.price) | currency }}</strong>
            </td>
            <td>{{ product.stockQuantity }}</td>
            <td>{{ product.categoryName }}</td>
            <td>{{ product.brandName || '-' }}</td>
            <td>{{ product.isActive ? 'Yes' : 'No' }}</td>
            <td>
              <button (click)="openEdit(product.id)">Edit</button>
              <button (click)="openStock(product)">Stock</button>
              <button (click)="deleteProduct(product.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="!loading && !loadError && products.length === 0">No products found.</div>
    </div>

    <div *ngIf="showForm" class="modal">
      <h3>{{ editingId ? 'Edit Product' : 'Add Product' }}</h3>
      <div>
        <div><label>Name (EN) *</label><input [(ngModel)]="form.nameEN" required></div>
        <div><label>Name (AR) *</label><input [(ngModel)]="form.nameAR" required></div>
        <div><label>SKU *</label><input [(ngModel)]="form.sku" required></div>
        <div><label>Category *</label>
          <select [(ngModel)]="form.categoryId" required>
            <option value="">Select...</option>
            <option *ngFor="let cat of categories" [value]="cat.id">{{ cat.nameEN }}</option>
          </select>
        </div>
        <div><label>Price *</label><input type="number" [(ngModel)]="form.price" required></div>
        <div><label>Discount Price</label><input type="number" [(ngModel)]="form.discountPrice"></div>
        <div><label>Stock Quantity *</label><input type="number" [(ngModel)]="form.stockQuantity" required></div>
        <div><label>Low Stock Threshold</label><input type="number" [(ngModel)]="form.lowStockThreshold" placeholder="(blank = global)"></div>
        <div>
          <label>Variants (size / color / SKU / price / stock)</label>
          <div *ngFor="let v of form.variants; let i = index" style="display:flex;gap:6px;margin-bottom:6px;align-items:center;">
            <input placeholder="Size" [(ngModel)]="v.size" style="width:70px;">
            <input placeholder="Color" [(ngModel)]="v.color" style="width:90px;">
            <input placeholder="SKU" [(ngModel)]="v.sku" style="width:110px;">
            <input type="number" placeholder="Price" [(ngModel)]="v.price" style="width:90px;">
            <input type="number" placeholder="Stock" [(ngModel)]="v.stockQuantity" style="width:80px;">
            <button type="button" (click)="removeVariant(i)">x</button>
          </div>
          <button type="button" (click)="addVariant()">+ Add Variant</button>
        </div>
        <div><label>Brand</label>
          <select [(ngModel)]="form.brandId">
            <option value="">None</option>
            <option *ngFor="let brand of brands" [value]="brand.id">{{ brand.nameEN }}</option>
          </select>
        </div>
        <div><label>Description (EN)</label><textarea rows="2" [(ngModel)]="form.descriptionEN"></textarea></div>
        <div><label>Description (AR)</label><textarea rows="2" [(ngModel)]="form.descriptionAR"></textarea></div>
        <div>
          <label>Images</label>
          <input type="file" multiple accept="image/*" (change)="onFilesSelected($event)">
          <small>First image becomes primary.</small>
          <div *ngFor="let url of form.imageUrls; let i = index">
            <img [src]="url" style="width:40px;height:40px;object-fit:contain;vertical-align:middle;">
            <span>{{ url }}</span>
            <button type="button" (click)="removeImage(i)">x</button>
          </div>
        </div>
        <div><label><input type="checkbox" [(ngModel)]="form.isActive"> Active (visible in storefront)</label></div>
      </div>
      <div>
        <button (click)="saveProduct()" [disabled]="uploading || saving">
          {{ uploading ? 'Uploading...' : (saving ? 'Saving...' : 'Save') }}
        </button>
        <button (click)="showForm = false" [disabled]="saving">Cancel</button>
      </div>
    </div>

    <div *ngIf="stockProduct" class="modal">
      <h3>Adjust Stock — {{ stockProduct.nameEN }}</h3>
      <p>Current stock: {{ stockProduct.stockQuantity }}</p>
      <div><label>Quantity change (+/-)</label><input type="number" [(ngModel)]="stockDelta"></div>
      <div><label>Reason</label><input [(ngModel)]="stockReason" placeholder="e.g. restock"></div>
      <div>
        <button (click)="applyStock()" [disabled]="adjusting">{{ adjusting ? 'Applying...' : 'Apply' }}</button>
        <button (click)="stockProduct = null" [disabled]="adjusting">Cancel</button>
      </div>
    </div>
  `
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  categories: any[] = [];
  brands: any[] = [];
  searchTerm = '';
  loading = false;
  loadError = false;

  showForm = false;
  editingId: string | null = null;
  uploading = false;
  saving = false;
  form: any = this.emptyForm();

  stockProduct: any = null;
  stockDelta = 0;
  stockReason = '';
  adjusting = false;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private brandService: BrandService
  ) {}

  ngOnInit() {
    this.loadProducts();
    this.categoryService.getAll().subscribe(res => this.categories = res.data || []);
    this.brandService.getAll().subscribe(res => this.brands = res.data || []);
  }

  emptyForm() {
    return {
      nameEN: '', nameAR: '', sku: '', price: 0, discountPrice: null,
      stockQuantity: 0, categoryId: '', brandId: '', descriptionEN: '',
      descriptionAR: '', imageUrls: [] as string[], isActive: true, lowStockThreshold: null,
      variants: [] as any[]
    };
  }

  addVariant() {
    this.form.variants.push({ sku: '', price: 0, stockQuantity: 0, size: '', color: '', isActive: true });
  }

  removeVariant(i: number) {
    this.form.variants.splice(i, 1);
  }

  loadProducts() {
    this.loading = true;
    this.loadError = false;
    this.productService.getAll(1, 50, this.searchTerm).subscribe({
      next: res => {
        this.products = res.data?.items || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.products = [];
      }
    });
  }

  openNew() {
    this.editingId = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEdit(id: string) {
    this.productService.getById(id).subscribe(res => {
      const p = res.data;
      if (!p) return;
      this.editingId = id;
      this.form = {
        nameEN: p.nameEN || '', nameAR: p.nameAR || '', sku: p.sku || '',
        price: p.price || 0, discountPrice: p.discountPrice ?? null,
        stockQuantity: p.stockQuantity || 0, categoryId: p.categoryId || '',
        brandId: p.brandId || '', descriptionEN: p.descriptionEN || '',
        descriptionAR: p.descriptionAR || '',
        imageUrls: (p.images || []).map((i: any) => i.imageUrl),
        isActive: p.isActive !== false,
        lowStockThreshold: p.lowStockThreshold ?? null,
        variants: (p.variants || []).filter((v: any) => v.isActive).map((v: any) => ({
          sku: v.sku || '', price: v.price || 0, stockQuantity: v.stockQuantity || 0,
          size: v.size || '', color: v.color || '', isActive: true
        }))
      };
      this.showForm = true;
    });
  }

  onFilesSelected(event: any) {
    const files: FileList = event.target.files;
    if (!files || !files.length) return;
    this.uploading = true;
    const arr = Array.from(files);
    let done = 0;
    arr.forEach(file => {
      this.productService.uploadImage(file).subscribe(res => {
        const url = res.data;
        if (url) this.form.imageUrls.push(url);
        done++;
        if (done === arr.length) { this.uploading = false; alert('Images uploaded'); }
      }, () => {
        done++;
        if (done === arr.length) this.uploading = false;
      });
    });
  }

  removeImage(i: number) {
    this.form.imageUrls.splice(i, 1);
  }

  saveProduct() {
    if (this.uploading || this.saving) return;
    const nameEN = (this.form.nameEN || '').trim();
    const nameAR = (this.form.nameAR || '').trim();
    const sku = (this.form.sku || '').trim();
    if (!nameEN) { alert('English name is required.'); return; }
    if (!nameAR) { alert('Arabic name is required.'); return; }
    if (!sku) { alert('SKU is required.'); return; }
    if (!this.form.categoryId) { alert('Please select a category.'); return; }
    if (this.form.price == null || Number(this.form.price) <= 0) { alert('Price must be greater than 0.'); return; }
    if (this.form.discountPrice != null && Number(this.form.discountPrice) > 0 && Number(this.form.discountPrice) >= Number(this.form.price)) {
      alert('Discount price must be lower than the regular price.'); return;
    }
    const payload = {
      nameAR, nameEN, sku,
      price: this.form.price, discountPrice: this.form.discountPrice || null,
      stockQuantity: this.form.stockQuantity,
      descriptionAR: this.form.descriptionAR, descriptionEN: this.form.descriptionEN,
      categoryId: this.form.categoryId, brandId: this.form.brandId || null,
      imageUrls: this.form.imageUrls, isActive: this.form.isActive,
      lowStockThreshold: this.form.lowStockThreshold === '' || this.form.lowStockThreshold == null ? null : Number(this.form.lowStockThreshold),
      variants: (this.form.variants || []).map((v: any) => ({
        sku: (v.sku || '').trim(), price: Number(v.price) || 0,
        stockQuantity: Number(v.stockQuantity) || 0, size: (v.size || '').trim(),
        color: (v.color || '').trim(), isActive: true
      }))
    };

    this.saving = true;
    if (this.editingId) {
      this.productService.update({ ...payload, id: this.editingId }).subscribe({
        next: () => this.finishSave('Product updated'),
        error: (err: any) => {
          this.saving = false;
          alert(err?.error?.message || 'Failed to update product');
        }
      });
    } else {
      this.productService.add(payload).subscribe({
        next: () => this.finishSave('Product added'),
        error: (err: any) => {
          this.saving = false;
          alert(err?.error?.message || 'Failed to add product');
        }
      });
    }
  }

  finishSave(msg: string) {
    this.saving = false;
    alert(msg);
    this.showForm = false;
    this.editingId = null;
    this.loadProducts();
  }

  openStock(product: any) {
    this.stockProduct = product;
    this.stockDelta = 0;
    this.stockReason = '';
  }

  applyStock() {
    if (this.adjusting) return;
    if (!this.stockProduct) return;
    if (!this.stockDelta) { alert('Enter a non-zero quantity change.'); return; }
    this.adjusting = true;
    this.productService.adjustStock(this.stockProduct.id, this.stockDelta, this.stockReason).subscribe({
      next: res => {
        this.adjusting = false;
        alert('Stock updated to ' + res.data);
        this.stockProduct = null;
        this.loadProducts();
      },
      error: (err: any) => {
        this.adjusting = false;
        alert(err?.error?.message || 'Failed to adjust stock');
      }
    });
  }

  deleteProduct(id: string) {
    if (!confirm('Delete this product?')) return;
    this.productService.delete(id).subscribe({
      next: () => { alert('Product deleted.'); this.loadProducts(); },
      error: (err: any) => alert(err?.error?.message || 'Failed to delete product')
    });
  }
}
