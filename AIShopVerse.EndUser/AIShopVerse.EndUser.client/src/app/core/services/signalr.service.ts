import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, distinctUntilChanged } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface OrderStatusUpdate {
  orderId: string;
  status: string;
}

export interface InfoUpdate {
  title: string;
  message?: string;
  type: number;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private _connection: HubConnection | null = null;
  private _orderStatusSubject = new BehaviorSubject<OrderStatusUpdate | null>(null);
  private _infoSubject = new BehaviorSubject<InfoUpdate | null>(null);
  orderStatus$ = this._orderStatusSubject.asObservable();
  info$ = this._infoSubject.asObservable();

  constructor(private authService: AuthService) {
    this.authService.currentUser$
      .pipe(distinctUntilChanged((a, b) => (!!a) === (!!b)))
      .subscribe(loggedIn => {
        if (loggedIn) {
          this.start();
        } else {
          this.stop();
        }
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

    this._connection.on('OrderStatusUpdate', (payload: OrderStatusUpdate) => {
      this._orderStatusSubject.next(payload);
    });
    this._connection.on('Info', (payload: InfoUpdate) => {
      this._infoSubject.next(payload);
    });

    this._connection.start().catch(() => {});
  }

  stop(): void {
    if (this._connection) {
      this._connection.stop().catch(() => {});
      this._connection = null;
    }
  }
}
