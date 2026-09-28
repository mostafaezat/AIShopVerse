import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category } from '../models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<Category[]> {
    return this.http.post<any>(`${environment.apiEndpoint}category/GetAll`, {})
      .pipe(map(r => r.data));
  }

  add(category: any): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}category/Add`, category)
      .pipe(map(r => r.data));
  }

  update(category: any): Observable<any> {
    return this.http.put<any>(`${environment.apiEndpoint}category/Update`, category)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiEndpoint}category/Delete/${id}`)
      .pipe(map(r => r.data));
  }
}
