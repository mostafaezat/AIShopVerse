import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CategoryTreeDto {
  id: string;
  nameEN?: string;
  nameAR?: string;
  imageUrl?: string;
  children: CategoryTreeDto[];
}

export interface CategoryWithCount {
  id: string;
  nameAR: string;
  nameEN: string;
  imageUrl?: string;
  parentId?: string;
  displayOrder: number;
  productCount: number;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private http: HttpClient) {}

  getCategoryTree(): Observable<CategoryTreeDto[]> {
    return this.http.get<any>(`${environment.apiEndpoint}category/Tree`)
      .pipe(map(r => r.data));
  }

  getFiltered(brandId?: string): Observable<CategoryWithCount[]> {
    let params = new HttpParams();
    if (brandId) params = params.set('brandId', brandId);
    return this.http.get<any>(`${environment.apiEndpoint}category/Filtered`, { params })
      .pipe(map(r => r.data));
  }
}
