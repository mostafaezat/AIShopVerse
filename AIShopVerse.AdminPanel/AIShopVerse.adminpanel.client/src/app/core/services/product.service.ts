import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient) {}

  getAll(page: number = 1, pageSize: number = 20, searchTerm?: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}product/GetAll`, { page, pageSize, searchTerm });
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${environment.apiEndpoint}product/${id}`);
  }

  add(product: any): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}product/Add`, product);
  }

  update(product: any): Observable<any> {
    return this.http.put(`${environment.apiEndpoint}product/Update`, product);
  }

  uploadImage(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${environment.apiEndpoint}product/UploadImage`, formData);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${environment.apiEndpoint}product/Delete/${id}`);
  }

  adjustStock(productId: string, quantity: number, reason?: string): Observable<any> {
    return this.http.post(`${environment.apiEndpoint}product/AdjustStock`, { productId, quantity, reason });
  }
}
