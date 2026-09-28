import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;
  const api = environment.apiEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationService]
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getUnreadCount returns unread count', () => {
    service.getUnreadCount().subscribe(c => expect(c).toBe(3));
    const req = httpMock.expectOne(`${api}notification/UnreadCount`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: 3 });
  });

  it('markAllRead posts to ReadAll', () => {
    service.markAllRead().subscribe();
    const req = httpMock.expectOne(`${api}notification/ReadAll`);
    expect(req.request.method).toBe('POST');
    req.flush({ data: true });
  });

  it('markRead posts to {id}/Read', () => {
    service.markRead('n1').subscribe();
    const req = httpMock.expectOne(`${api}notification/n1/Read`);
    expect(req.request.method).toBe('POST');
    req.flush({ data: true });
  });

  it('getAll builds query params when filters provided', () => {
    service.getAll(true, 10).subscribe();
    const req = httpMock.expectOne(`${api}notification?onlyUnread=true&take=10`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: [] });
  });
});
