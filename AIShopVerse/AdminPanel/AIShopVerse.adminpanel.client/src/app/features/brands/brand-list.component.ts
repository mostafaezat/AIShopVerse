import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BrandService } from '../../core/services/brand.service';
import { Brand } from '../../core/models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-brand-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Brands</h2>
      <button class="btn btn-primary" (click)="toggleForm()">
        {{ showForm ? 'Cancel' : 'Add Brand' }}
      </button>
    </div>

    <div *ngIf="showForm" class="card mb-4">
      <div class="card-body">
        <h5>{{ editingBrand ? 'Edit Brand' : 'Add Brand' }}</h5>
        <form (ngSubmit)="saveBrand()">
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (AR)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameAR" name="nameAR">
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (EN)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameEN" name="nameEN" required>
            </div>
            <div class="col-md-9 mb-3">
              <label class="form-label">Description</label>
              <input type="text" class="form-control" [(ngModel)]="formData.description" name="description">
            </div>
            <div class="col-md-3 mb-3 d-flex align-items-end">
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
            <th>Name (EN)</th>
            <th>Name (AR)</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let brand of brands">
            <td>{{ brand.nameEN }}</td>
            <td>{{ brand.nameAR }}</td>
            <td>
              <span class="badge" [class.bg-success]="brand.isActive" [class.bg-secondary]="!brand.isActive">
                {{ brand.isActive ? 'Yes' : 'No' }}
              </span>
            </td>
            <td>
              <button class="btn btn-sm btn-warning me-1" (click)="editBrand(brand)">Edit</button>
              <button class="btn btn-sm btn-danger" (click)="deleteBrand(brand.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class BrandListComponent implements OnInit {
  brands: Brand[] = [];
  showForm = false;
  editingBrand: Brand | null = null;
  formData: any = {};

  constructor(private brandService: BrandService) {}

  ngOnInit(): void {
    this.loadBrands();
  }

  loadBrands(): void {
    this.brandService.getAll().subscribe(b => this.brands = b);
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.editingBrand = null;
      this.formData = {};
    }
  }

  editBrand(brand: Brand): void {
    this.editingBrand = brand;
    this.formData = { ...brand };
    this.showForm = true;
  }

  saveBrand(): void {
    if (this.editingBrand) {
      this.brandService.update(this.formData).subscribe(() => {
        this.loadBrands();
        this.toggleForm();
      });
    } else {
      this.brandService.add(this.formData).subscribe(() => {
        this.loadBrands();
        this.toggleForm();
      });
    }
  }

  deleteBrand(id: string): void {
    Swal.fire({
      title: 'Are you sure?',
      text: 'This brand will be deleted.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Delete'
    }).then((result) => {
      if (result.isConfirmed) {
        this.brandService.delete(id).subscribe(() => {
          this.loadBrands();
          Swal.fire('Deleted!', 'Brand has been deleted.', 'success');
        });
      }
    });
  }
}
