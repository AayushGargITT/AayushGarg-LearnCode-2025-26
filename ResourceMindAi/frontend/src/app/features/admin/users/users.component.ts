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
import { AddEmployeeRequest, CreateUserRequest, Role, User } from '../../../core/models/user.model';
import { UserService } from '../../../core/services/user.service';

type UserAction = 'resetPassword' | 'toggleStatus' | 'addEmployee';

interface UserActionItem {
  text: string;
  action: UserAction;
  disabled?: boolean;
}

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
  isLoading = signal(false);
  isCreating = signal(false);
  actionUserId = signal<string | null>(null);
  resetPasswordUser = signal<User | null>(null);
  employeeDialogUser = signal<User | null>(null);
  isAddingEmployee = signal(false);
  createError = signal<string | null>(null);
  actionError = signal<string | null>(null);
  employeeError = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  roles = [Role.ADMIN, Role.MANAGER, Role.EMPLOYEE];

  createForm = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', Validators.required],
    role: [Role.EMPLOYEE, Validators.required],
  });

  employeeForm = this.fb.group({
    designation: ['', Validators.required],
    department: ['', Validators.required],
  });

  constructor() {
    this.loadUsers();
  }

  openDrawer() {
    this.successMessage.set(null);
    this.actionError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer() {
    this.drawerOpen.set(false);
    this.createError.set(null);
    this.createForm.reset({ role: Role.EMPLOYEE });
  }

  loadUsers() {
    this.isLoading.set(true);
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.rows.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.createError.set('Unable to load users.');
      }
    });
  }

  createUser() {
    this.createError.set(null);
    this.successMessage.set(null);

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isCreating.set(true);

    this.userService.createUser(this.createForm.getRawValue() as CreateUserRequest).subscribe({
      next: () => {
        this.isCreating.set(false);
        this.closeDrawer();
        this.successMessage.set('User created successfully.');
        this.loadUsers();
      },
      error: (err) => {
        this.isCreating.set(false);
        this.createError.set(err.error?.message ?? 'Unable to create user.');
      }
    });
  }

  actionsFor(user: User): UserActionItem[] {
    return [
      { text: 'Reset Password', action: 'resetPassword' },
      { text: user.isActive ? 'Deactivate User' : 'Activate User', action: 'toggleStatus' },
      {
        text: 'Add as Employee',
        action: 'addEmployee',
        disabled: user.role === Role.ADMIN || !!user.employeeId
      }
    ];
  }

  onUserAction(user: User, event: UserActionItem | { item?: UserActionItem }): void {
    const item = (event as { item?: UserActionItem }).item ?? event as UserActionItem;

    if (!item) {
      return;
    }

    if (item.disabled) {
      return;
    }

    this.actionError.set(null);
    this.successMessage.set(null);

    if (item.action === 'resetPassword') {
      this.openResetPasswordDialog(user);
      return;
    }

    if (item.action === 'toggleStatus') {
      this.toggleStatus(user);
      return;
    }

    this.openEmployeeDialog(user);
  }

  openResetPasswordDialog(user: User): void {
    this.resetPasswordUser.set(user);
  }

  closeResetPasswordDialog(): void {
    this.resetPasswordUser.set(null);
  }

  confirmResetPassword(): void {
    const user = this.resetPasswordUser();
    if (!user) {
      return;
    }

    this.actionUserId.set(user.id);
    this.userService.resetPassword(user.id).subscribe({
      next: (updatedUser) => {
        this.updateRow(updatedUser);
        this.actionUserId.set(null);
        this.closeResetPasswordDialog();
        this.successMessage.set('Password reset successfully.');
      },
      error: (err) => {
        this.actionUserId.set(null);
        this.actionError.set(err.error?.message ?? 'Unable to reset password.');
      }
    });
  }

  toggleStatus(user: User): void {
    this.actionUserId.set(user.id);
    this.userService.toggleStatus(user.id).subscribe({
      next: (updatedUser) => {
        this.updateRow(updatedUser);
        this.actionUserId.set(null);
        this.successMessage.set(updatedUser.isActive ? 'User activated successfully.' : 'User deactivated successfully.');
      },
      error: (err) => {
        this.actionUserId.set(null);
        this.actionError.set(err.error?.message ?? 'Unable to update user status.');
      }
    });
  }

  openEmployeeDialog(user: User): void {
    this.employeeError.set(null);
    this.employeeForm.reset();
    this.employeeDialogUser.set(user);
  }

  closeEmployeeDialog(): void {
    this.employeeDialogUser.set(null);
    this.employeeError.set(null);
    this.isAddingEmployee.set(false);
    this.employeeForm.reset();
  }

  submitEmployee(): void {
    const user = this.employeeDialogUser();
    if (!user) {
      return;
    }

    this.employeeError.set(null);

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    this.isAddingEmployee.set(true);
    this.userService.addEmployee(user.id, this.employeeForm.getRawValue() as AddEmployeeRequest).subscribe({
      next: (updatedUser) => {
        this.updateRow(updatedUser);
        this.closeEmployeeDialog();
        this.successMessage.set('Employee profile created successfully.');
      },
      error: (err) => {
        this.isAddingEmployee.set(false);
        this.employeeError.set(err.error?.message ?? 'Unable to add user as employee.');
      }
    });
  }

  private updateRow(updatedUser: User): void {
    this.rows.update((users) => users.map((user) => user.id === updatedUser.id ? updatedUser : user));
  }
}
