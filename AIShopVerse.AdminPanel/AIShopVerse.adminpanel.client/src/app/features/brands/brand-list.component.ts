import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BrandService } from '../../core/services/brand.service';

@Component({
  selector: 'app-brand-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="brands-container">
      <h2>Brands</h2>
      <button (click)="openNew()">+ Add Brand</button>
      <table>
        <thead><tr><th>Logo</th><th>Name (EN)</th><th>Name (AR)</th><th>Description</th><th>Active</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="6">Loading brands...</td></tr>
          <tr *ngIf="!loading && loadError"><td colspan="6">Unable to load brands. <button (click)="loadBrands()">Retry</button></td></tr>
          <tr *ngFor="let brand of brands">
            <td><img [src]="brand.logoUrl || 'https://via.placeholder.com/40?text=Logo'" style="width:40px;height:40px;object-fit:contain;"></td>
            <td>{{ brand.nameEN }}</td>
            <td>{{ brand.nameAR }}</td>
            <td>{{ brand.description }}</td>
            <td>{{ brand.isActive ? 'Yes' : 'No' }}</td>
            <td>
              <button (click)="openEdit(brand)">Edit</button>
              <button (click)="deleteBrand(brand.id)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="!loading && !loadError && brands.length === 0">No brands found.</div>
    </div>

    <div *ngIf="showForm" class="modal">
      <h3>{{ form.id ? 'Edit Brand' : 'Add Brand' }}</h3>
      <div>
        <div><label>Name (EN) *</label><input [(ngModel)]="form.nameEN"></div>
        <div><label>Name (AR) *</label><input [(ngModel)]="form.nameAR"></div>
        <div><label>Description</label><textarea rows="2" [(ngModel)]="form.description"></textarea></div>
        <div><label>Logo URL</label><input [(ngModel)]="form.logoUrl"></div>
        <div *ngIf="form.id"><label><input type="checkbox" [(ngModel)]="form.isActive"> Active</label></div>
      </div>
      <div>
        <button [disabled]="!form.nameEN || saving" (click)="saveBrand()">{{ saving ? 'Saving...' : 'Save' }}</button>
        <button (click)="showForm = false" [disabled]="saving">Cancel</button>
      </div>
    </div>
  `
})
export class BrandListComponent implements OnInit {
  brands: any[] = [];
  showForm = false;
  form: any = {};
  loading = true;
  loadError = false;
  saving = false;

  constructor(private brandService: BrandService) {}

  ngOnInit() { this.loadBrands(); }

  loadBrands() {
    this.loading = true;
    this.loadError = false;
    this.brandService.getAll().subscribe({
      next: res => {
        this.brands = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
        this.brands = [];
      }
    });
  }

  openNew() {
    this.form = { nameEN: '', nameAR: '', description: '', logoUrl: '', isActive: true };
    this.showForm = true;
  }

  openEdit(brand: any) {
    this.form = {
      id: brand.id, nameEN: brand.nameEN, nameAR: brand.nameAR,
      description: brand.description || '', logoUrl: brand.logoUrl || '',
      isActive: brand.isActive
    };
    this.showForm = true;
  }

  saveBrand() {
    if (this.saving) return;
    const nameEN = (this.form.nameEN || '').trim();
    const nameAR = (this.form.nameAR || '').trim();
    if (!nameEN) { alert('English name is required.'); return; }
    if (!nameAR) { alert('Arabic name is required.'); return; }
    const payload = {
      nameEN, nameAR,
      description: this.form.description || null, logoUrl: this.form.logoUrl || null,
      isActive: this.form.isActive
    };
    const call = this.form.id
      ? this.brandService.update({ ...payload, id: this.form.id })
      : this.brandService.add(payload);

    this.saving = true;
    call.subscribe({
      next: () => { this.saving = false; alert('Saved'); this.showForm = false; this.loadBrands(); },
      error: (err: any) => { this.saving = false; alert(err?.error?.message || 'Failed to save'); }
    });
  }

  deleteBrand(id: string) {
    if (!confirm('Delete this brand?')) return;
    this.brandService.delete(id).subscribe({
      next: () => { alert('Brand deleted.'); this.loadBrands(); },
      error: (err: any) => alert(err?.error?.message || 'Failed to delete brand')
    });
  }
}
