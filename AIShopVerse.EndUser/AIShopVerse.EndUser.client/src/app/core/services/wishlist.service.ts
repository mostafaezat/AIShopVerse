import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WishlistItem } from '../models';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  constructor(private http: HttpClient) {}

  getWishlist(): Observable<WishlistItem[]> {
    return this.http.get<any>(`${environment.apiEndpoint}wishlist`)
      .pipe(map(r => r.data));
  }

  addToWishlist(productId: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}wishlist/Add`, { productId })
      .pipe(map(r => r.data));
  }

  removeFromWishlist(productId: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}wishlist/Remove`, { productId })
      .pipe(map(r => r.data));
  }
}
