import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SearchSuggestion {
  id: string;
  nameEN?: string;
  nameAR?: string;
  sku?: string;
  price: number;
  discountPrice?: number;
  imageUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class SearchService {
  constructor(private http: HttpClient) {}

  suggest(term: string, count: number = 8): Observable<SearchSuggestion[]> {
    return this.http.post<any>(`${environment.apiEndpoint}search/Suggest`, { searchTerm: term, count })
      .pipe(map(r => r.data));
  }
}
