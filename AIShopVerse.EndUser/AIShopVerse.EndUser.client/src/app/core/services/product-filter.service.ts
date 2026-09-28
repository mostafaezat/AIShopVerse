import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ProductFilterState {
  categoryId?: string;
  brandId?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  inStockOnly?: boolean;
  searchTerm?: string;
  sortBy?: 'newest' | 'price_asc' | 'price_desc' | 'rating';
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ProductFilterService {
  private filterSubject = new BehaviorSubject<ProductFilterState>({ page: 1, pageSize: 24 });
  filter$ = this.filterSubject.asObservable();

  updateFilter(partial: Partial<ProductFilterState>) {
    this.filterSubject.next({ ...this.filterSubject.value, ...partial, page: 1 });
  }

  updatePage(page: number) {
    const current = this.filterSubject.value;
    this.filterSubject.next({ ...current, page });
  }

  getFilter(): ProductFilterState {
    return this.filterSubject.value;
  }
}
