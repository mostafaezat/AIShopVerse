import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface NotificationDto {
  id: string;
  title: string;
  message?: string;
  type: number;
  orderId?: string;
  isRead: boolean;
  createdAt: Date;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private http: HttpClient) {}

  getAll(onlyUnread?: boolean, take?: number): Observable<NotificationDto[]> {
    let url = `${environment.apiEndpoint}notification`;
    const params: string[] = [];
    if (onlyUnread) params.push(`onlyUnread=true`);
    if (take) params.push(`take=${take}`);
    if (params.length) url += `?${params.join('&')}`;
    return this.http.get<any>(url).pipe(map(r => r.data));
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<any>(`${environment.apiEndpoint}notification/UnreadCount`)
      .pipe(map(r => r.data));
  }

  markRead(id: string): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}notification/${id}/Read`, {})
      .pipe(map(r => r.data));
  }

  markAllRead(): Observable<any> {
    return this.http.post<any>(`${environment.apiEndpoint}notification/ReadAll`, {})
      .pipe(map(r => r.data));
  }
}
