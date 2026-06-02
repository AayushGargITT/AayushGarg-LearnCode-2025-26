import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AvatarComponent } from '../avatar/avatar.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { Role } from '../../../core/models/user.model';

const NAV: Record<Role, { to: string; label: string; icon: string }[]> = {
  [Role.ADMIN]: [
    { to: '/admin/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { to: '/admin/users', label: 'Manage Users', icon: 'group' },
    { to: '/admin/employees', label: 'Manage Employees', icon: 'manage_accounts' },
    { to: '/admin/projects', label: 'Manage Projects', icon: 'account_tree' },
    { to: '/admin/allocations', label: 'All Allocations', icon: 'fact_check' },
    { to: '/admin/config', label: 'System Config', icon: 'settings' },
  ],
  [Role.MANAGER]: [
    { to: '/manager/resources', label: 'Resource Dashboard', icon: 'speed' },
    { to: '/manager/allocate', label: 'Allocate Resource', icon: 'auto_awesome' },
    { to: '/manager/projects', label: 'My Projects', icon: 'work' },
    { to: '/manager/timesheets', label: 'Timesheets', icon: 'schedule' },
    { to: '/manager/ai', label: 'AI Assistant', icon: 'auto_awesome' },
  ],
  [Role.EMPLOYEE]: [
    { to: '/employee/timesheets/submit', label: 'Submit Timesheet', icon: 'post_add' },
    { to: '/employee/allocations', label: 'My Allocations', icon: 'work' },
    { to: '/employee/timesheets/history', label: 'Timesheet History', icon: 'history' },
  ],
};

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, AvatarComponent, StatusBadgeComponent],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.css'
})
export class AppLayoutComponent {
  @Input() title: string = '';

  authService = inject(AuthService);
  router = inject(Router);

  get navItems() {
    const currentUser=this.authService.currentUser()
    if(!currentUser) return []
    return NAV[currentUser.role] || [];
  }

  get user() {
    const current = this.authService.currentUser();
    if(!current) return {current:'', roleLabel:''}
    return {
      name: current.fullName,
      roleLabel: current.role,
    };
  }

  logout() {
    this.authService.logout();
  }
}
