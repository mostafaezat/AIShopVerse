import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Brand } from '../models';

export interface BrandWithCount {
  id: string;
  nameAR: string;
  nameEN: string;
  logoUrl?: string;
  productCount: number;
}

@Injectable({ providedIn: 'root' })
export class BrandService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Brand[]> {
    return this.http.get<any>(`${environment.apiEndpoint}brand/All`)
      .pipe(map(r => r.data));
  }

  getFiltered(categoryId?: string): Observable<BrandWithCount[]> {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    return this.http.get<any>(`${environment.apiEndpoint}brand/Filtered`, { params })
      .pipe(map(r => r.data));
  }
}
