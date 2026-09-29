import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../core/services/category.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="categories-container">
      <h2>Categories</h2>
      <button (click)="openNew()">+ Add Category</button>
      <table>
        <thead><tr><th>Name (EN)</th><th>Name (AR)</th><th>Parent</th><th>Description</th><th>Order</th><th>Active</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="7">Loading categories...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="7">Unable to load categories. <button (click)="loadCategories()">Retry</button></td></tr>
          <tr *ngFor="let cat of categories">
            <td>{{ cat.nameEN }}</td>
            <td>{{ cat.nameAR }}</td>
            <td>{{ parentName(cat.parentId) || '-' }}</td>
            <td>{{ cat.description }}</td>
            <td>{{ cat.displayOrder }}</td>
            <td>{{ cat.isActive ? 'Yes' : 'No' }}</td>
            <td>
              <button (click)="openEdit(cat)">Edit</button>
              <button (click)="deleteCategory(cat.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="!loading && !loadError && categories.length === 0">No categories found.</div>
    </div>

    <div *ngIf="showForm" class="modal">
      <h3>{{ form.id ? 'Edit Category' : 'Add Category' }}</h3>
      <div>
        <div><label>Name (EN) *</label><input [(ngModel)]="form.nameEN"></div>
        <div><label>Name (AR) *</label><input [(ngModel)]="form.nameAR"></div>
        <div><label>Description</label><textarea rows="2" [(ngModel)]="form.description"></textarea></div>
        <div><label>Image URL</label><input [(ngModel)]="form.imageUrl"></div>
        <div><label>Parent Category</label>
          <select [(ngModel)]="form.parentId">
            <option value="">None (top level)</option>
            <option *ngFor="let cat of categories" [value]="cat.id" [disabled]="cat.id === form.id">{{ cat.nameEN }}</option>
          </select>
        </div>
        <div><label>Display Order</label><input type="number" [(ngModel)]="form.displayOrder"></div>
        <div *ngIf="form.id"><label><input type="checkbox" [(ngModel)]="form.isActive"> Active</label></div>
      </div>
      <div>
        <button [disabled]="!form.nameEN || saving" (click)="saveCategory()">{{ saving ? 'Saving...' : 'Save' }}</button>
        <button (click)="showForm = false" [disabled]="saving">Cancel</button>
      </div>
    </div>
  `
})
export class CategoryListComponent implements OnInit {
  categories: any[] = [];
  showForm = false;
  form: any = {};
  loading = true;
  loadError = false;
  saving = false;

  constructor(private categoryService: CategoryService) {}

  ngOnInit() { this.loadCategories(); }

  loadCategories() {
    this.loading = true;
    this.loadError = false;
    this.categoryService.getAll().subscribe({
      next: res => {
        this.categories = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.categories = [];
      }
    });
  }

  parentName(id?: string): string | null {
    if (!id) return null;
    const p = this.categories.find(c => c.id === id);
    return p ? p.nameEN : null;
  }

  openNew() {
    this.form = { nameEN: '', nameAR: '', description: '', imageUrl: '', parentId: '', displayOrder: 0, isActive: true };
    this.showForm = true;
  }

  openEdit(cat: any) {
    this.form = {
      id: cat.id, nameEN: cat.nameEN, nameAR: cat.nameAR, description: cat.description || '',
      imageUrl: cat.imageUrl || '', parentId: cat.parentId || '', displayOrder: cat.displayOrder || 0,
      isActive: cat.isActive
    };
    this.showForm = true;
  }

  saveCategory() {
    if (this.saving) return;
    const nameEN = (this.form.nameEN || '').trim();
    const nameAR = (this.form.nameAR || '').trim();
    if (!nameEN) { alert('English name is required.'); return; }
    if (!nameAR) { alert('Arabic name is required.'); return; }
    const payload = {
      nameEN, nameAR, description: this.form.description || null,
      imageUrl: this.form.imageUrl || null, parentId: this.form.parentId || null,
      displayOrder: this.form.displayOrder || 0, isActive: this.form.isActive
    };
    const call = this.form.id
      ? this.categoryService.update({ ...payload, id: this.form.id })
      : this.categoryService.add(payload);

    this.saving = true;
    call.subscribe({
      next: () => { this.saving = false; alert('Saved'); this.showForm = false; this.loadCategories(); },
      error: (err: any) => { this.saving = false; alert(err?.error?.message || 'Failed to save'); }
    });
  }

  deleteCategory(id: string) {
    if (!confirm('Delete this category?')) return;
    this.categoryService.delete(id).subscribe({
      next: () => { alert('Category deleted.'); this.loadCategories(); },
      error: (err: any) => alert(err?.error?.message || 'Failed to delete category')
    });
  }
}
