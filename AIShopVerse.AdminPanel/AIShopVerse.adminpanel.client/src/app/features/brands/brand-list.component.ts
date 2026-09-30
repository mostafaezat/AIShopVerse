import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BrandService } from '../../core/services/brand.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-brand-list',
  standalone: true,
  imports: [CommonModule, LoadingComponent, EmptyStateComponent],
  templateUrl: './brand-list.component.html',
  styleUrl: './brand-list.component.scss'
})
export class BrandListComponent implements OnInit {
  brands: any[] = [];
  loading = true;
  loadError = false;
  placeholderLogo = 'https://via.placeholder.com/40?text=Logo';

  constructor(
    private brandService: BrandService,
    private router: Router,
    private sweetAlert: SweetAlertService
  ) {}

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

  onImageError(event: Event) {
    (event.target as HTMLImageElement).src = this.placeholderLogo;
  }

  navigateToAdd() {
    this.router.navigate(['/brands/add']);
  }

  navigateToEdit(id: string) {
    this.router.navigate(['/brands/edit', id]);
  }

  async confirmDelete(brand: any) {
    const confirmed = await this.sweetAlert.confirmDelete(`brand "${brand.nameEN}"`);
    if (!confirmed) return;

    this.brandService.delete(brand.id).subscribe({
      next: () => {
        this.sweetAlert.toastSuccess('Brand deleted.');
        this.loadBrands();
      },
      error: (err: any) => this.sweetAlert.toastError(err?.error?.message || 'Failed to delete brand')
    });
  }
}