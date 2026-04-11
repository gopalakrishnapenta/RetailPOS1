import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit {
  userRole: string = '';
  userEmail: string = '';
  userName: string = '';
  theme: 'light' | 'dark' = 'light';

  constructor(private router: Router) {}

  ngOnInit() {
    this.loadTheme();
    this.loadUser();
    // Re-load user info whenever navigation happens to ensure sidebar stays in sync
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.loadUser();
    });
  }

  loadUser() {
    try {
      const userJson = localStorage.getItem('user');
      const user = userJson ? JSON.parse(userJson) : {};
      
      // Support both camelCase and PascalCase from backend
      this.userRole = user.role || user.Role || '';
      this.userEmail = user.email || user.Email || '';
      
      if (this.userEmail) {
        this.userName = this.userEmail.split('@')[0];
        // Capitalize first letter of username for better UI
        this.userName = this.userName.charAt(0).toUpperCase() + this.userName.slice(1);
      } else {
        this.userName = 'User';
      }
    } catch (e) {
      console.error('Error parsing user data from localStorage', e);
      this.userRole = '';
      this.userName = 'User';
    }
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }

  loadTheme() {
    const saved = localStorage.getItem('theme');
    if (saved === 'dark' || saved === 'light') {
      this.theme = saved;
    }
    document.documentElement.setAttribute('data-theme', this.theme);
  }

  toggleTheme() {
    this.theme = this.theme === 'dark' ? 'light' : 'dark';
    localStorage.setItem('theme', this.theme);
    document.documentElement.setAttribute('data-theme', this.theme);
  }

  hasAccess(roles: string[]): boolean {
    return roles.includes(this.userRole);
  }
}
