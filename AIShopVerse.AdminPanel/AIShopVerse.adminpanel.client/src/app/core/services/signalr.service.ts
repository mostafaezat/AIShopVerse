import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, distinctUntilChanged } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface NewOrderPayload { orderId: string; orderNumber: string; status: string; total: number; }
export interface LowStockPayload { productId: string; nameEN: string; sku: string; stockQuantity: number; threshold: number; }
export interface InfoPayload { title: string; message?: string; type: number; }

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private _connection: HubConnection | null = null;
  private _newOrderSubject = new BehaviorSubject<NewOrderPayload | null>(null);
  private _lowStockSubject = new BehaviorSubject<LowStockPayload | null>(null);
  private _orderStatusSubject = new BehaviorSubject<any | null>(null);
  private _infoSubject = new BehaviorSubject<InfoPayload | null>(null);

  newOrder$ = this._newOrderSubject.asObservable();
  lowStock$ = this._lowStockSubject.asObservable();
  orderStatus$ = this._orderStatusSubject.asObservable();
  info$ = this._infoSubject.asObservable();

  constructor(private authService: AuthService) {
    this.authService.currentUser$
      .pipe(distinctUntilChanged((a, b) => (!!a) === (!!b)))
      .subscribe(loggedIn => {
        if (loggedIn) this.start();
        else this.stop();
      });
  }

  start(): void {
    if (this._connection && this._connection.state === 'Connected') return;

    const token = this.authService.getToken();
    let url = `${environment.signalRUrl}notificationHub`;
    if (token) url += (url.includes('?') ? '&' : '?') + `access_token=${encodeURIComponent(token)}`;

    this._connection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this._connection.on('NewOrder', (payload: NewOrderPayload) => this._newOrderSubject.next(payload));
    this._connection.on('LowStockAlert', (payload: LowStockPayload) => this._lowStockSubject.next(payload));
    this._connection.on('OrderStatusUpdate', (payload: any) => this._orderStatusSubject.next(payload));
    this._connection.on('Info', (payload: InfoPayload) => this._infoSubject.next(payload));

    this._connection.start().catch(() => {});
  }

  stop(): void {
    if (this._connection) {
      this._connection.stop().catch(() => {});
      this._connection = null;
    }
  }
}
