import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { ProductListComponent } from './product-list.component';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { BrandService } from '../../core/services/brand.service';
import { ProductFilterService } from '../../core/services/product-filter.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { AuthService } from '../../core/services/auth.service';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let productServiceStub: any;

  const mockResult = {
    items: [
      { id: 'p1', nameEN: 'Phone A', price: 100, stockQuantity: 5, isActive: true, primaryImageUrl: 'img/a' },
      { id: 'p2', nameEN: 'Phone B', price: 200, discountPrice: 150, stockQuantity: 0, isActive: true, primaryImageUrl: 'img/b' }
    ],
    totalItems: 2,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
    page: 1,
    pageSize: 24
  };

  beforeEach(async () => {
    productServiceStub = jasmine.createSpyObj('ProductService', ['getFiltered']);
    productServiceStub.getFiltered.and.returnValue(of(mockResult));

    await TestBed.configureTestingModule({
      imports: [ProductListComponent, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: ProductService, useValue: productServiceStub },
        { provide: CategoryService, useValue: { getCategoryTree: () => of([]), getFiltered: () => of([]) } },
        { provide: BrandService, useValue: { getAll: () => of([]), getFiltered: () => of([]) } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } },
        { provide: AuthService, useValue: { isLoggedIn: () => false } },
        { provide: WishlistService, useValue: { getWishlist: () => of([]), addToWishlist: () => of({}), removeFromWishlist: () => of({}) } },
        ProductFilterService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load and render products from the filter service', () => {
    expect(component.products.length).toBe(2);
    expect(productServiceStub.getFiltered).toHaveBeenCalled();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Phone A');
    expect(compiled.textContent).toContain('Phone B');
  });

  it('should call the API with the default filter on init', () => {
    const arg = productServiceStub.getFiltered.calls.mostRecent().args[0];
    expect(arg.page).toBe(1);
    expect(arg.pageSize).toBe(24);
  });

  it('should update filter through the shared service on sort change', () => {
    const filterService = TestBed.inject(ProductFilterService);
    spyOn(filterService, 'updateFilter').and.callThrough();
    component.sortBy = 'price_asc';
    component.onFilterChange();
    expect(filterService.getFilter().sortBy).toBe('price_asc');
  });

  it('should clear filters back to defaults', () => {
    const filterService = TestBed.inject(ProductFilterService);
    component.searchTerm = 'test';
    component.sortBy = 'rating';
    component.minRating = 4;
    component.clearFilters();
    const state = filterService.getFilter();
    expect(state.searchTerm).toBeUndefined();
    expect(state.sortBy).toBe('newest');
    expect(state.minRating).toBeUndefined();
  });

  it('should not render pagination when there is a single page', () => {
    const compiled = fixture.nativeElement;
    expect(component.pages.length).toBe(0);
  });
});
