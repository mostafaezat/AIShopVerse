import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReviewService } from './review.service';
import { environment } from '../../../environments/environment';

describe('ReviewService', () => {
  let service: ReviewService;
  let httpMock: HttpTestingController;
  const api = environment.apiEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReviewService]
    });
    service = TestBed.inject(ReviewService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll posts to GetAll with pagination', () => {
    service.getAll(2, 10, false).subscribe();
    const req = httpMock.expectOne(`${api}review/GetAll`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ page: 2, pageSize: 10, approved: false });
    req.flush({ data: { items: [] } });
  });

  it('approve posts ReviewId to Approve', () => {
    service.approve('r1').subscribe();
    const req = httpMock.expectOne(`${api}review/Approve`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reviewId: 'r1' });
    req.flush({ data: 'ok' });
  });

  it('reject posts ReviewId to Reject', () => {
    service.reject('r1').subscribe();
    const req = httpMock.expectOne(`${api}review/Reject`);
    expect(req.request.body).toEqual({ reviewId: 'r1' });
    req.flush({ data: 'ok' });
  });

  it('delete removes review by id', () => {
    service.delete('r1').subscribe();
    const req = httpMock.expectOne(`${api}review/Delete/r1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ data: 'ok' });
  });
});
