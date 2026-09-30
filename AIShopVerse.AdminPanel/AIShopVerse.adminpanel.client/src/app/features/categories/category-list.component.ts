import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CategoryService } from '../../core/services/category.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, LoadingComponent, EmptyStateComponent],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.scss'
})
export class CategoryListComponent implements OnInit {
  categories: any[] = [];
  loading = true;
  loadError = false;

  constructor(
    private categoryService: CategoryService,
    private router: Router,
    private sweetAlert: SweetAlertService
  ) {}

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

  getParentName(id?: string): string {
    if (!id) return '—';
    const parent = this.categories.find(c => c.id === id);
    return parent ? parent.nameEN : '—';
  }

  navigateToAdd() {
    this.router.navigate(['/categories/add']);
  }

  navigateToEdit(id: string) {
    this.router.navigate(['/categories/edit', id]);
  }

  async confirmDelete(cat: any) {
    const confirmed = await this.sweetAlert.confirmDelete(`category "${cat.nameEN}"`);
    if (!confirmed) return;

    this.categoryService.delete(cat.id).subscribe({
      next: () => {
        this.sweetAlert.toastSuccess('Category deleted.');
        this.loadCategories();
      },
      error: (err: any) => this.sweetAlert.toastError(err?.error?.message || 'Failed to delete category')
    });
  }
}