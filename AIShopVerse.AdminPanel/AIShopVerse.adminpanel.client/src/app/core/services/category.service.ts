import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}category/GetAll`, {});
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${environment.apiEndpoint}category/GetById/${id}`);
  }

  add(category: any): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}category/Add`, category);
  }

  update(category: any): Observable<any> {
    return this.http.put(`${environment.apiEndpoint}category/Update`, category);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${environment.apiEndpoint}category/Delete/${id}`);
  }
}