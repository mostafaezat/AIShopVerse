import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Coupon } from '../models';

@Injectable({ providedIn: 'root' })
export class PromotionService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Coupon[]> {
    return this.http.post<any>(`${environment.apiEndpoint}promotion/GetAll`, {})
      .pipe(map(r => r.data));
  }

  add(coupon: any): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}promotion/Add`, coupon)
      .pipe(map(r => r.data));
  }

  update(coupon: any): Observable<any> {
    return this.http.put<any>(`${environment.apiEndpoint}promotion/Update`, coupon)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiEndpoint}promotion/Delete/${id}`)
      .pipe(map(r => r.data));
  }
}
