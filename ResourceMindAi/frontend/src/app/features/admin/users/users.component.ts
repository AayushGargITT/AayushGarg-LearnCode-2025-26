import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { AddEmployeeRequest, CreateUserRequest, Role, User } from '../../../core/models/user.model';
import { UserService } from '../../../core/services/user.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { AddEmployeeDialogComponent } from './components/add-employee-dialog/add-employee-dialog.component';
import { CreateUserDialogComponent } from './components/create-user-dialog/create-user-dialog.component';

type UserAction = 'resetPassword' | 'toggleStatus' | 'addEmployee';
type UserActionItem = RowActionItem<UserAction>;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    ConfirmDialogComponent,
    PageFeedbackComponent,
    RowActionMenuComponent,
    CreateUserDialogComponent,
    AddEmployeeDialogComponent
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css',
  providers: [PageStateService]
})
export class AdminUsersComponent {
  private readonly userService = inject(UserService);
  readonly pageState = inject(PageStateService);

  drawerOpen = signal(false);
  rows = signal<User[]>([]);
  isCreating = signal(false);
  resetPasswordUser = signal<User | null>(null);
  employeeDialogUser = signal<User | null>(null);
  isAddingEmployee = signal(false);
  createError = signal<string | null>(null);
  employeeError = signal<string | null>(null);

  constructor() {
    this.loadUsers();
  }

  openDrawer() {
    this.pageState.clearFeedback();
    this.drawerOpen.set(true);
  }

  closeDrawer() {
    this.drawerOpen.set(false);
    this.createError.set(null);
  }

  loadUsers() {
    this.pageState.startLoading();
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.rows.set(users);
        this.pageState.stopLoading();
      },
      error: () => {
        this.pageState.stopLoading();
        this.createError.set('Unable to load users.');
      }
    });
  }

  createUser(request: CreateUserRequest) {
    this.createError.set(null);
    this.pageState.clearFeedback();

    this.isCreating.set(true);

    this.userService.createUser(request).subscribe({
      next: () => {
        this.isCreating.set(false);
        this.closeDrawer();
        this.pageState.setSuccess('User created successfully.');
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

  onUserAction(user: User, item: UserActionItem): void {
    this.pageState.clearFeedback();

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

    this.pageState.startAction(user.id);
    this.userService.resetPassword(user.id).subscribe({
      next: () => {
        this.pageState.stopAction();
        this.closeResetPasswordDialog();
        this.pageState.setSuccess('Password reset successfully.');
        this.loadUsers();
      },
      error: (err) => {
        this.pageState.stopAction();
        this.pageState.setError(err.error?.message ?? 'Unable to reset password.');
      }
    });
  }

  toggleStatus(user: User): void {
    this.pageState.startAction(user.id);
    this.userService.toggleStatus(user.id).subscribe({
      next: () => {
        this.pageState.stopAction();
        this.pageState.setSuccess(user.isActive ? 'User deactivated successfully.' : 'User activated successfully.');
        this.loadUsers();
      },
      error: (err) => {
        this.pageState.stopAction();
        this.pageState.setError(err.error?.message ?? 'Unable to update user status.');
      }
    });
  }

  openEmployeeDialog(user: User): void {
    this.employeeError.set(null);
    this.employeeDialogUser.set(user);
  }

  closeEmployeeDialog(): void {
    this.employeeDialogUser.set(null);
    this.employeeError.set(null);
    this.isAddingEmployee.set(false);
  }

  submitEmployee(request: AddEmployeeRequest): void {
    const user = this.employeeDialogUser();
    if (!user) {
      return;
    }

    this.employeeError.set(null);

    this.isAddingEmployee.set(true);
    this.userService.addEmployee(user.id, request).subscribe({
      next: () => {
        this.closeEmployeeDialog();
        this.pageState.setSuccess('Employee profile created successfully.');
        this.loadUsers();
      },
      error: (err) => {
        this.isAddingEmployee.set(false);
        this.employeeError.set(err.error?.message ?? 'Unable to add user as employee.');
      }
    });
  }
}
