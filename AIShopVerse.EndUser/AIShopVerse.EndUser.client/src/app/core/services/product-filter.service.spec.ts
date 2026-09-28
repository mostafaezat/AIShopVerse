import { TestBed } from '@angular/core/testing';
import { ProductFilterService } from './product-filter.service';

describe('ProductFilterService', () => {
  let service: ProductFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProductFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have default filter state', () => {
    const state = service.getFilter();
    expect(state.page).toBe(1);
    expect(state.pageSize).toBe(24);
  });

  it('should update a filter and reset page to 1', () => {
    service.updatePage(3);
    expect(service.getFilter().page).toBe(3);

    service.updateFilter({ searchTerm: 'phones', sortBy: 'price_asc' });
    const state = service.getFilter();
    expect(state.searchTerm).toBe('phones');
    expect(state.sortBy).toBe('price_asc');
    expect(state.page).toBe(1);
  });

  it('should merge partial updates without dropping existing keys', () => {
    service.updateFilter({ categoryId: 'cat-1' });
    service.updateFilter({ brandId: 'brand-1' });
    const state = service.getFilter();
    expect(state.categoryId).toBe('cat-1');
    expect(state.brandId).toBe('brand-1');
  });

  it('should update page independently', () => {
    service.updatePage(2);
    expect(service.getFilter().page).toBe(2);
  });

  it('should emit the latest state through filter$', () => {
    let latest: any;
    service.filter$.subscribe(s => latest = s);
    service.updateFilter({ inStockOnly: true });
    expect(latest.inStockOnly).toBe(true);
  });
});
