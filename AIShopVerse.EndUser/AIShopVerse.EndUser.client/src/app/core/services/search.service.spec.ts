import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SearchService } from './search.service';
import { environment } from '../../../environments/environment';

describe('SearchService', () => {
  let service: SearchService;
  let httpMock: HttpTestingController;
  const api = environment.apiEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SearchService]
    });
    service = TestBed.inject(SearchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('suggest posts searchTerm and count and returns data', () => {
    const items = [{ id: 's1', nameEN: 'Phone', price: 100 }];
    service.suggest('pho', 8).subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].id).toBe('s1');
    });
    const req = httpMock.expectOne(`${api}search/Suggest`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ searchTerm: 'pho', count: 8 });
    req.flush({ data: items });
  });

  it('suggest defaults count to 8 when not provided', () => {
    service.suggest('abc').subscribe();
    const req = httpMock.expectOne(`${api}search/Suggest`);
    expect(req.request.body).toEqual({ searchTerm: 'abc', count: 8 });
    req.flush({ data: null });
  });
});
