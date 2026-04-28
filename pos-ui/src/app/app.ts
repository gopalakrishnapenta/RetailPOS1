import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { SidebarComponent } from './core/components/sidebar/sidebar.component';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { SignalrService } from './core/services/signalr.service';

import { AiChatbotComponent } from './shared/components/ai-chatbot/ai-chatbot.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, CommonModule, AiChatbotComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  showSidebar = false;

  constructor(private router: Router, private signalr: SignalrService) {
    // Start SignalR connection
    if (localStorage.getItem('token')) {
      this.signalr.startConnection();
    }
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      const noSidebarRoutes = ['/login', '/signup', '/pending-approval', '/otp-verification', '/'];
      const urlPath = event.urlAfterRedirects.split('?')[0];
      this.showSidebar = !noSidebarRoutes.includes(urlPath);
    });
  }
}
