import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AdminSidebarComponent } from '../components/sidebar/sidebar.component';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AdminSidebarComponent],
  templateUrl: './categories.component.html'
})
export class CategoriesComponent implements OnInit {
  categories: any[] = [];
  newCat = { name: '', description: '', isActive: true };
  isEditing = false;
  editingId: number | null = null;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.api.getAdminCategories().subscribe(res => this.categories = res);
  }

  editCategory(c: any) {
    this.isEditing = true;
    this.editingId = c.id;
    this.newCat = { name: c.name, description: c.description, isActive: c.isActive };
  }

  cancelEdit() {
    this.isEditing = false;
    this.editingId = null;
    this.newCat = { name: '', description: '', isActive: true };
  }

  toggleStatus(c: any) {
    if (c.isActive) {
      if (confirm('Are you sure you want to disable this category?')) {
        this.api.deleteCategory(c.id).subscribe(() => {
          this.loadCategories();
          alert('Category disabled successfully');
        });
      }
    } else {
      // Enable logic: Use PUT to update the status
      const updatedCat = { ...c, isActive: true };
      this.api.updateCategory(c.id, updatedCat).subscribe(() => {
        this.loadCategories();
        alert('Category enabled successfully');
      });
    }
  }

  onSubmit() {
    if (!this.newCat.name) return;

    if (this.isEditing && this.editingId) {
      this.api.updateCategory(this.editingId, this.newCat).subscribe(() => {
        this.loadCategories();
        this.cancelEdit();
        alert('Category updated successfully');
      });
    } else {
      this.api.addCategory(this.newCat).subscribe(() => {
        this.loadCategories();
        this.newCat = { name: '', description: '', isActive: true };
        alert('Category added successfully');
      });
    }
  }
}
