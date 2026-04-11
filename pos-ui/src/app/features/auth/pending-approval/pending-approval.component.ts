import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-pending-approval',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pending-approval.component.html',
  styleUrls: ['../login/login.component.css', './pending-approval.component.css']
})
export class PendingApprovalComponent implements OnInit {
  userEmail = '';

  constructor(private router: Router) {}

  ngOnInit() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    this.userEmail = user.email || 'your-email@gmail.com';
    
    // Safety check: if they have a role, redirect them out of here
    if (user.role && user.role !== 'PENDING_STAFF') {
      this.router.navigate(['/pos/billing']);
    }
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}
