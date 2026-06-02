import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { Role, User } from '../../../core/models/user.model';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class AdminUsersComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  drawerOpen = signal(false);
  rows = signal<User[]>([]);
  createError = signal<string | null>(null);
  roles = [Role.ADMIN, Role.MANAGER, Role.EMPLOYEE];

  createForm = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: [Role.EMPLOYEE, Validators.required],
  });

  constructor() {
    this.loadUsers();
  }

  openDrawer() {
    this.drawerOpen.set(true);
  }

  closeDrawer() {
    this.drawerOpen.set(false);
    this.createError.set(null);
    this.createForm.reset({ role: Role.EMPLOYEE });
  }

  loadUsers() {
    this.userService.getAllUsers().subscribe((users) => this.rows.set(users));
  }

  createUser() {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.userService.createUser(this.createForm.getRawValue()).subscribe({
      next: () => {
        this.closeDrawer();
        this.loadUsers();
      },
      error: (err) => this.createError.set(err.error?.message ?? 'Unable to create user.')
    });
  }
}
