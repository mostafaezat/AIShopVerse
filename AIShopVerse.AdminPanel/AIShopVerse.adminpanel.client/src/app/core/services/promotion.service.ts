import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PromotionService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}promotion/GetAll`, {});
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${environment.apiEndpoint}promotion/GetById/${id}`);
  }

  add(coupon: any): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}promotion/Add`, coupon);
  }

  update(coupon: any): Observable<any> {
    return this.http.put(`${environment.apiEndpoint}promotion/Update`, coupon);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${environment.apiEndpoint}promotion/Delete/${id}`);
  }

  toggle(id: string): Observable<any> {
    return this.http.put(`${environment.apiEndpoint}promotion/Toggle/${id}`, {});
  }
}