import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Review } from '../models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  constructor(private http: HttpClient) {}

  getProductReviews(productId: string): Observable<Review[]> {
    return this.http.get<any>(`${environment.apiEndpoint}review/product/${productId}`)
      .pipe(map(r => r.data));
  }

  createReview(productId: string, rating: number, comment?: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}review`, { productId, rating, comment })
      .pipe(map(r => r.data));
  }

  deleteReview(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiEndpoint}review/${id}`)
      .pipe(map(r => r.data));
  }
}
