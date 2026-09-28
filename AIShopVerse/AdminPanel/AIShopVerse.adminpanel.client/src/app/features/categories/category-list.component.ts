import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Categories</h2>
      <button class="btn btn-primary" (click)="toggleForm()">
        {{ showForm ? 'Cancel' : 'Add Category' }}
      </button>
    </div>

    <div *ngIf="showForm" class="card mb-4">
      <div class="card-body">
        <h5>{{ editingCategory ? 'Edit Category' : 'Add Category' }}</h5>
        <form (ngSubmit)="saveCategory()">
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (AR)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameAR" name="nameAR">
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Name (EN)</label>
              <input type="text" class="form-control" [(ngModel)]="formData.nameEN" name="nameEN" required>
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Description</label>
              <input type="text" class="form-control" [(ngModel)]="formData.description" name="description">
            </div>
            <div class="col-md-3 mb-3">
              <label class="form-label">Display Order</label>
              <input type="number" class="form-control" [(ngModel)]="formData.displayOrder" name="displayOrder" required>
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
            <th>Display Order</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let category of categories">
            <td>{{ category.nameEN }}</td>
            <td>{{ category.nameAR }}</td>
            <td>{{ category.displayOrder }}</td>
            <td>
              <span class="badge" [class.bg-success]="category.isActive" [class.bg-secondary]="!category.isActive">
                {{ category.isActive ? 'Yes' : 'No' }}
              </span>
            </td>
            <td>
              <button class="btn btn-sm btn-warning me-1" (click)="editCategory(category)">Edit</button>
              <button class="btn btn-sm btn-danger" (click)="deleteCategory(category.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class CategoryListComponent implements OnInit {
  categories: Category[] = [];
  showForm = false;
  editingCategory: Category | null = null;
  formData: any = {};

  constructor(private categoryService: CategoryService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe(c => this.categories = c);
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.editingCategory = null;
      this.formData = {};
    }
  }

  editCategory(category: Category): void {
    this.editingCategory = category;
    this.formData = { ...category };
    this.showForm = true;
  }

  saveCategory(): void {
    if (this.editingCategory) {
      this.categoryService.update(this.formData).subscribe(() => {
        this.loadCategories();
        this.toggleForm();
      });
    } else {
      this.categoryService.add(this.formData).subscribe(() => {
        this.loadCategories();
        this.toggleForm();
      });
    }
  }

  deleteCategory(id: string): void {
    Swal.fire({
      title: 'Are you sure?',
      text: 'This category will be deleted.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Delete'
    }).then((result) => {
      if (result.isConfirmed) {
        this.categoryService.delete(id).subscribe(() => {
          this.loadCategories();
          Swal.fire('Deleted!', 'Category has been deleted.', 'success');
        });
      }
    });
  }
}
