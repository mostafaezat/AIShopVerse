import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { BrandService } from '../../core/services/brand.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingComponent, EmptyStateComponent, ModalComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  categories: any[] = [];
  brands: any[] = [];
  searchTerm = '';
  loading = false;
  loadError = false;

  // Stock modal
  @ViewChild('stockModal') stockModal?: any;
  stockProduct: any = null;
  stockDelta = 0;
  stockReason = '';
  adjusting = false;
  placeholderImage = 'https://via.placeholder.com/50?text=No+Image';

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private brandService: BrandService,
    private router: Router,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() {
    this.loadProducts();
    this.categoryService.getAll().subscribe(res => this.categories = res.data || []);
    this.brandService.getAll().subscribe(res => this.brands = res.data || []);
  }

  getStockBadgeClass(stock: number): string {
    if (stock === 0) return 'bg-danger';
    if (stock < 10) return 'bg-warning text-dark';
    return 'bg-success';
  }

  onImageError(event: Event) {
    (event.target as HTMLImageElement).src = this.placeholderImage;
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

  navigateToAdd() {
    this.router.navigate(['/products/add']);
  }

  navigateToEdit(id: string) {
    this.router.navigate(['/products/edit', id]);
  }

  async confirmDelete(product: any) {
    const confirmed = await this.sweetAlert.confirmDelete(`product "${product.nameEN}"`);
    if (!confirmed) return;

    this.productService.delete(product.id).subscribe({
      next: () => {
        this.sweetAlert.toastSuccess('Product deleted.');
        this.loadProducts();
      },
      error: (err: any) => this.sweetAlert.toastError(err?.error?.message || 'Failed to delete product')
    });
  }

  openStockModal(product: any) {
    this.stockProduct = product;
    this.stockDelta = 0;
    this.stockReason = '';
    this.stockModal?.show();
  }

  closeStockModal() {
    this.stockModal?.hide();
    this.stockProduct = null;
  }

  applyStock() {
    if (this.adjusting || !this.stockProduct || !this.stockDelta) return;

    this.adjusting = true;
    this.productService.adjustStock(this.stockProduct.id, this.stockDelta, this.stockReason).subscribe({
      next: res => {
        this.adjusting = false;
        this.sweetAlert.toastSuccess('Stock updated to ' + res.data);
        this.closeStockModal();
        this.loadProducts();
      },
      error: (err: any) => {
        this.adjusting = false;
        this.sweetAlert.toastError(err?.error?.message || 'Failed to adjust stock');
      }
    });
  }
}