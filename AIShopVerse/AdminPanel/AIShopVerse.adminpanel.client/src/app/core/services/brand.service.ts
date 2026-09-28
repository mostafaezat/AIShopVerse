import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Brand } from '../models';

@Injectable({ providedIn: 'root' })
export class BrandService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Brand[]> {
    return this.http.post<any>(`${environment.apiEndpoint}brand/GetAll`, {})
      .pipe(map(r => r.data));
  }

  add(brand: any): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}brand/Add`, brand)
      .pipe(map(r => r.data));
  }

  update(brand: any): Observable<any> {
    return this.http.put<any>(`${environment.apiEndpoint}brand/Update`, brand)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiEndpoint}brand/Delete/${id}`)
      .pipe(map(r => r.data));
  }
}
