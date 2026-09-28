import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BrandService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}brand/GetAll`, {});
  }

  add(brand: any): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}brand/Add`, brand);
  }

  update(brand: any): Observable<any> {
    return this.http.put(`${environment.apiEndpoint}brand/Update`, brand);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${environment.apiEndpoint}brand/Delete/${id}`);
  }
}
