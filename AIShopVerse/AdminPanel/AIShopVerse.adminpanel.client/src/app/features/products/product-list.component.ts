import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { BrandService } from '../../core/services/brand.service';
import { Product, Category, Brand } from '../../core/models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Products</h2>
      <button class="btn btn-primary" (click)="toggleForm()">
        {{ showForm ? 'Cancel' : 'Add Product' }}
      </button>
    </div>

    <div *ngIf="showForm" class="card mb-4">
      <div class="card-body">
        <h5>{{ editingProduct ? 'Edit Product' : 'Add Product' }}</h5>
        <form (ngSubmit)="saveProduct()">
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (AR)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameAR" name="nameAR">
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (EN)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameEN" name="nameEN">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">SKU</label>
              <input type="text" class="form-control" [(ngModel)]="formData.sku" name="sku">
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Price</label>
              <input type="number" class="form-control" [(ngModel)]="formData.price" name="price" required>
            </div>
            <div class="col-md-4 mb-3">
              <label class="form-label">Stock Quantity</label>
              <input type="number" class="form-control" [(ngModel)]="formData.stockQuantity" name="stockQuantity" required>
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Category</label>
              <select class="form-select" [(ngModel)]="formData.categoryId" name="categoryId">
                <option value="">Select Category</option>
                <option *ngFor="let c of categories" [value]="c.id">{{ c.nameEN }}</option>
              </select>
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Brand</label>
              <select class="form-select" [(ngModel)]="formData.brandId" name="brandId">
                <option value="">Select Brand</option>
                <option *ngFor="let b of brands" [value]="b.id">{{ b.nameEN }}</option>
              </select>
            </div>
            <div class="col-md-12 mb-3">
              <label class="form-label">Description (EN)</label>
              <textarea class="form-control" [(ngModel)]="formData.descriptionEN" name="descriptionEN" rows="3"></textarea>
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
            <th>Name</th>
            <th>SKU</th>
            <th>Price</th>
            <th>Stock</th>
            <th>Category</th>
            <th>Brand</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let product of products">
            <td>{{ product.nameEN }}</td>
            <td>{{ product.sku }}</td>
            <td>{{ product.price | currency }}</td>
            <td [class.text-danger]="product.stockQuantity < 10">{{ product.stockQuantity }}</td>
            <td>{{ product.categoryName }}</td>
            <td>{{ product.brandName }}</td>
            <td>
              <span class="badge" [class.bg-success]="product.isActive" [class.bg-secondary]="!product.isActive">
                {{ product.isActive ? 'Yes' : 'No' }}
              </span>
            </td>
            <td>
              <button class="btn btn-sm btn-warning me-1" (click)="editProduct(product)">Edit</button>
              <button class="btn btn-sm btn-info me-1" (click)="adjustStock(product)">Adjust Stock</button>
              <button class="btn btn-sm btn-danger" (click)="deleteProduct(product.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class ProductListComponent implements OnInit {
  products: Product[] = [];
  categories: Category[] = [];
  brands: Brand[] = [];
  showForm = false;
  editingProduct: Product | null = null;
  formData: any = {};

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private brandService: BrandService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.categoryService.getAll().subscribe(c => this.categories = c);
    this.brandService.getAll().subscribe(b => this.brands = b);
  }

  loadProducts(): void {
    this.productService.getAll().subscribe(p => this.products = p);
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.editingProduct = null;
      this.formData = {};
    }
  }

  editProduct(product: Product): void {
    this.editingProduct = product;
    this.formData = { ...product };
    this.showForm = true;
  }

  saveProduct(): void {
    if (this.editingProduct) {
      this.productService.update(this.formData).subscribe(() => {
        this.loadProducts();
        this.toggleForm();
      });
    } else {
      this.productService.add(this.formData).subscribe(() => {
        this.loadProducts();
        this.toggleForm();
      });
    }
  }

  deleteProduct(id: string): void {
    Swal.fire({
      title: 'Are you sure?',
      text: 'This product will be deleted.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Delete'
    }).then((result) => {
      if (result.isConfirmed) {
        this.productService.delete(id).subscribe(() => {
          this.loadProducts();
          Swal.fire('Deleted!', 'Product has been deleted.', 'success');
        });
      }
    });
  }

  adjustStock(product: Product): void {
    Swal.fire({
      title: 'Adjust Stock',
      input: 'number',
      inputLabel: `Current stock: ${product.stockQuantity}. Enter quantity to add (negative to reduce):`,
      inputPlaceholder: 'Quantity',
      showCancelButton: true,
      confirmButtonText: 'Adjust'
    }).then((result) => {
      if (result.isConfirmed && result.value !== null) {
        this.productService.adjustStock(product.id, result.value).subscribe(() => {
          this.loadProducts();
          Swal.fire('Updated!', 'Stock has been adjusted.', 'success');
        });
      }
    });
  }
}
