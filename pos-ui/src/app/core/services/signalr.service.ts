import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  private notificationSubject = new Subject<any>();

  constructor() {}

  /**
   * Initialize and start the SignalR connection
   */
  startConnection() {
    const token = localStorage.getItem('token');
    if (!token) {
       console.warn('SignalR: No token found, skipping connection.');
       return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5176/notificationHub', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR: Connection started successfully');
        this.joinStoreGroup();
        this.registerHandlers();
      })
      .catch(err => console.error('SignalR: Error while starting connection: ' + err));
  }

  /**
   * Listener for incoming notifications
   */
  get notifications$(): Observable<any> {
    return this.notificationSubject.asObservable();
  }

  private registerHandlers() {
    this.hubConnection?.on('ReceiveNotification', (user, message) => {
      console.log(`SignalR: Received [${user}]: ${message}`);
      this.notificationSubject.next({ user, message });
    });
  }

  private joinStoreGroup() {
    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      const storeId = user.storeId || user.StoreId;
      if (storeId && this.hubConnection?.state === signalR.HubConnectionState.Connected) {
        console.log(`SignalR: Joining group Store_${storeId}`);
        this.hubConnection.invoke('JoinStoreGroup', storeId)
          .catch(err => console.error('SignalR: JoinStoreGroup error', err));
      }
    } catch (e) {
      console.warn('SignalR: Parse user failed', e);
    }
  }

  stopConnection() {
    this.hubConnection?.stop()
      .then(() => console.log('SignalR: Connection stopped'))
      .catch(err => console.error('SignalR: Error while stopping connection: ' + err));
  }
}
