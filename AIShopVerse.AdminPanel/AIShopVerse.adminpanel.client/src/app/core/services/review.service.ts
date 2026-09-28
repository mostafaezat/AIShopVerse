import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  constructor(private http: HttpClient) {}

  getAll(page: number = 1, pageSize: number = 20, approved?: boolean | null): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}review/GetAll`, { page, pageSize, approved });
  }

  approve(id: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}review/Approve`, { reviewId: id });
  }

  reject(id: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}review/Reject`, { reviewId: id });
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${environment.apiEndpoint}review/Delete/${id}`);
  }
}
