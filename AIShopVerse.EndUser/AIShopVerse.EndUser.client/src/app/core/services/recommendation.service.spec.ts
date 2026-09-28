import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RecommendationService } from './recommendation.service';
import { environment } from '../../../environments/environment';

describe('RecommendationService', () => {
  let service: RecommendationService;
  let httpMock: HttpTestingController;
  const api = environment.apiEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RecommendationService]
    });
    service = TestBed.inject(RecommendationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getForMe posts count and returns data', () => {
    const payload = { mode: 'Personalized', products: [{ id: 'p1', nameEN: 'Phone', price: 100, stockQuantity: 5, isActive: true, images: [], attributes: [], variants: [] }] };
    service.getForMe(12).subscribe(res => {
      expect(res.mode).toBe('Personalized');
      expect(res.products.length).toBe(1);
    });
    const req = httpMock.expectOne(`${api}recommendation/ForMe`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ count: 12 });
    req.flush({ data: payload });
  });

  it('getForMe defaults count to 12 when not provided', () => {
    service.getForMe().subscribe();
    const req = httpMock.expectOne(`${api}recommendation/ForMe`);
    expect(req.request.body).toEqual({ count: 12 });
    req.flush({ data: null });
  });

  it('getRelated posts productId and count and returns data', () => {
    const items = [{ id: 'p2', nameEN: 'Related', price: 20, stockQuantity: 3, isActive: true, images: [], attributes: [], variants: [] }];
    service.getRelated('p1', 8).subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].id).toBe('p2');
    });
    const req = httpMock.expectOne(`${api}discovery/Related`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'p1', count: 8 });
    req.flush({ data: items });
  });

  it('getBoughtTogether posts productId and count and returns data', () => {
    const items = [{ id: 'p3', nameEN: 'Together', price: 30, stockQuantity: 2, isActive: true, images: [], attributes: [], variants: [] }];
    service.getBoughtTogether('p1').subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].id).toBe('p3');
    });
    const req = httpMock.expectOne(`${api}discovery/BoughtTogether`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'p1', count: 8 });
    req.flush({ data: items });
  });
});
