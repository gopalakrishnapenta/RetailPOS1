import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.css']
})
export class LandingComponent {
  features = [
    {
      title: 'Smart Billing',
      description: 'Ultra-fast checkout experience with offline support and integrated payments.',
      icon: '⚡'
    },
    {
      title: 'Real-time Inventory',
      description: 'Track stock across multiple stores with AI-driven low stock alerts.',
      icon: '📦'
    },
    {
      title: 'Advanced Analytics',
      description: 'Gain deep insights into your business performance with interactive dashboards.',
      icon: '📊'
    },
    {
      title: 'Enterprise RBAC',
      description: 'Granular permissions and role-based access for your entire staff.',
      icon: '🛡️'
    }
  ];

  stats = [
    { value: '10k+', label: 'Active Stores' },
    { value: '99.9%', label: 'Uptime' },
    { value: '24/7', label: 'Support' },
    { value: '50M+', label: 'Transactions' }
  ];
}
