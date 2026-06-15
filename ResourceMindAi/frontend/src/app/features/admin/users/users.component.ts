import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import {
  CreateUserRequest,
  ManagerDeactivationDetails,
  Role,
  User
} from '../../../core/models/user.model';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { CreateUserDialogComponent } from './components/create-user-dialog/create-user-dialog.component';
import { DeactivationBlockedDialogComponent } from './components/deactivation-blocked-dialog/deactivation-blocked-dialog.component';

type UserAction = 'resetPassword' | 'deactivate' | 'reactivate';
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
    DeactivationBlockedDialogComponent
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css',
  providers: [PageStateService]
})
export class AdminUsersComponent {
  private readonly adminUserService = inject(AdminUserService);
  readonly pageState = inject(PageStateService);

  drawerOpen = signal(false);
  rows = signal<User[]>([]);
  isCreating = signal(false);
  resetPasswordUser = signal<User | null>(null);
  deactivateUser = signal<User | null>(null);
  deactivationBlockers = signal<ManagerDeactivationDetails | null>(null);
  createError = signal<string | null>(null);

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
    this.adminUserService.getAllUsers().subscribe({
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

    this.adminUserService.createUser(request).subscribe({
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
    const actions: UserActionItem[] = [
      { text: 'Reset Password', action: 'resetPassword' },
      user.isActive
        ? { text: 'Deactivate User', action: 'deactivate' }
        : { text: 'Reactivate User', action: 'reactivate' }
    ];

    return actions;
  }

  onUserAction(user: User, item: UserActionItem): void {
    this.pageState.clearFeedback();

    if (item.action === 'resetPassword') {
      this.openResetPasswordDialog(user);
      return;
    }

    if (item.action === 'deactivate') {
      this.openDeactivateDialog(user);
      return;
    }

    if (item.action === 'reactivate') {
      this.reactivate(user);
    }
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
    this.adminUserService.resetPassword(user.id).subscribe({
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

  openDeactivateDialog(user: User): void {
    this.deactivateUser.set(user);
  }

  closeDeactivateDialog(): void {
    this.deactivateUser.set(null);
  }

  deactivationMessage(user: User): string {
    if (user.role === Role.RESOURCE) {
      return `Deactivate ${user.fullName}? Their active allocations will be ended as of today, their manager assignment will be removed, and historical records will be preserved.`;
    }

    if (user.role === Role.MANAGER) {
      return `Deactivate ${user.fullName}? This will proceed only if no active or planned projects and no active resources are assigned to this manager.`;
    }

    return `Deactivate ${user.fullName}?`;
  }

  confirmDeactivate(): void {
    const user = this.deactivateUser();
    if (!user) {
      return;
    }

    this.pageState.startAction(user.id);
    this.adminUserService.deactivateUser(user.id).subscribe({
      next: result => {
        this.pageState.stopAction();
        this.closeDeactivateDialog();
        this.pageState.setSuccess(result.message);
        this.loadUsers();
      },
      error: (err) => {
        this.pageState.stopAction();
        this.closeDeactivateDialog();

        const details = err.error?.details as ManagerDeactivationDetails | undefined;
        if (details && (details.projects?.length || details.employees?.length)) {
          this.deactivationBlockers.set(details);
          return;
        }

        this.pageState.setError(err.error?.message ?? 'Unable to deactivate user.');
      }
    });
  }

  reactivate(user: User): void {
    this.pageState.startAction(user.id);
    this.adminUserService.reactivateUser(user.id).subscribe({
      next: () => {
        this.pageState.stopAction();
        this.pageState.setSuccess('User reactivated successfully.');
        this.loadUsers();
      },
      error: err => {
        this.pageState.stopAction();
        this.pageState.setError(err.error?.message ?? 'Unable to reactivate user.');
      }
    });
  }

  closeDeactivationBlockers(): void {
    this.deactivationBlockers.set(null);
  }
}
