import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  private baseUrl = 'http://localhost:5000/gateway/notificationHub';

  constructor(private auth: AuthService, private notification: NotificationService) { }

  public startConnection(): void {
    if (this.hubConnection) return;

    const token = this.auth.getToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.baseUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('🚀 SignalR: Connected to NotificationHub');
        this.registerHandlers();
      })
      .catch(err => console.error('❌ SignalR: Error while starting connection: ' + err));
  }

  private registerHandlers(): void {
    this.hubConnection?.on('ReceiveNotification', (user: string, message: string) => {
      console.log(`🔔 Notification Received: [${user}] ${message}`);
      
      // Determine type based on message content for better UI
      let type: 'success' | 'error' | 'warning' | 'info' = 'info';
      if (message.includes('LOW STOCK')) type = 'warning';
      if (message.includes('ERROR') || message.includes('FAILED')) type = 'error';
      if (message.includes('RECEIVED') || message.includes('SUCCESS')) type = 'success';

      this.notification.show('System Alert', message, type);
    });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop().then(() => {
        this.hubConnection = null;
      });
    }
  }
}
