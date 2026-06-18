import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  CreateEmployeeSkillRequest,
  Employee,
  EmployeeManagerUpdatePreview,
  EmployeeSkill,
  EmployeeStatus,
  UpdateEmployeeManagerRequest,
  UpdateEmployeeSkillProficiencyRequest
} from '../../../core/models/employee.model';
import { Role, User } from '../../../core/models/user.model';
import { AdminEmployeeService } from '../../../core/services/admin-employee.service';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { AssignManagerDialogComponent } from './components/assign-manager-dialog/assign-manager-dialog.component';
import { EmployeeManagerConfirmDialogComponent } from './components/employee-manager-confirm-dialog/employee-manager-confirm-dialog.component';
import { EmployeeSkillsDialogComponent } from './components/employee-skills-dialog/employee-skills-dialog.component';

type EmployeeAction = 'manageSkills' | 'updateManager';
type EmployeeActionItem = RowActionItem<EmployeeAction>;

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    PageFeedbackComponent,
    RowActionMenuComponent,
    AssignManagerDialogComponent,
    EmployeeManagerConfirmDialogComponent,
    EmployeeSkillsDialogComponent
  ],
  templateUrl: './employees.component.html',
  providers: [PageStateService]
})
export class AdminEmployeesComponent {
  private readonly adminEmployeeService = inject(AdminEmployeeService);
  private readonly adminUserService = inject(AdminUserService);
  readonly pageState = inject(PageStateService);
  readonly Role = Role;

  readonly filter = signal<string>('All');
  readonly searchTerm = signal<string>('');
  readonly rows = signal<Employee[]>([]);
  readonly selectedEmployee = signal<Employee | null>(null);
  readonly selectedEmployeeSkills = signal<EmployeeSkill[]>([]);
  readonly isSkillLoading = signal(false);
  readonly isSkillSaving = signal(false);
  readonly skillError = signal<string | null>(null);
  readonly selectedManagerEmployee = signal<Employee | null>(null);
  readonly activeManagers = signal<User[]>([]);
  readonly isManagerLoading = signal(false);
  readonly isManagerSaving = signal(false);
  readonly managerError = signal<string | null>(null);
  readonly managerUpdatePreview = signal<EmployeeManagerUpdatePreview | null>(null);
  readonly pendingManagerUpdate = signal<UpdateEmployeeManagerRequest | null>(null);

  readonly availableManagers = computed(() => {
    const currentManagerId = this.selectedManagerEmployee()?.managerId;
    return this.activeManagers().filter(manager => manager.id !== currentManagerId);
  });

  readonly filteredRows = computed(() => {
    const filter = this.filter();
    const searchTerm = this.searchTerm().trim().toLowerCase();

    return this.rows().filter(row => {
      const matchesFilter =
        filter === 'All'
        || (filter === 'Bench' && row.allocationStatus === EmployeeStatus.BENCH)
        || (filter === 'Allocated' && row.allocationStatus === EmployeeStatus.ALLOCATED);

      const matchesSearch =
        !searchTerm
        || row.fullName.toLowerCase().includes(searchTerm)
        || row.department.toLowerCase().includes(searchTerm)
        || row.designation.toLowerCase().includes(searchTerm);

      return matchesFilter && matchesSearch;
    });
  });

  constructor() {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.pageState.startLoading();
    this.adminEmployeeService.getAllEmployees().subscribe({
      next: employees => {
        this.rows.set(employees);
        this.pageState.stopLoading();
      },
      error: err => {
        this.rows.set([]);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load employees.');
      }
    });
  }

  setFilter(filter: string): void {
    this.filter.set(filter);
  }

  setSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  canManageSkills(employee: Employee): boolean {
    return employee.role === Role.RESOURCE;
  }

  canUpdateManager(employee: Employee): boolean {
    return employee.role === Role.RESOURCE
      && employee.isActive;
  }

  managerDisplay(employee: Employee): string {
    if (employee.role === Role.MANAGER) {
      return 'Self';
    }

    if (employee.role !== Role.RESOURCE) {
      return 'Not applicable';
    }

    return employee.managerName || 'Unassigned';
  }

  actionsFor(employee: Employee): EmployeeActionItem[] {
    const actions: EmployeeActionItem[] = [];

    if (this.canManageSkills(employee)) {
      actions.push({ text: 'Manage Skills', action: 'manageSkills' });
    }

    if (this.canUpdateManager(employee)) {
      actions.push({
        text: employee.managerId ? 'Update Manager' : 'Assign Manager',
        action: 'updateManager'
      });
    }

    return actions;
  }

  onEmployeeAction(employee: Employee, item: EmployeeActionItem): void {
    if (item.action === 'manageSkills') {
      this.openSkills(employee);
      return;
    }

    if (item.action === 'updateManager') {
      this.openManagerDialog(employee);
    }
  }

  openSkills(employee: Employee): void {
    if (!this.canManageSkills(employee)) {
      return;
    }

    this.selectedEmployee.set(employee);
    this.skillError.set(null);
    this.loadSkills(employee.id);
  }

  closeSkills(): void {
    this.selectedEmployee.set(null);
    this.selectedEmployeeSkills.set([]);
    this.skillError.set(null);
    this.isSkillLoading.set(false);
    this.isSkillSaving.set(false);
  }

  addSkill(request: CreateEmployeeSkillRequest): void {
    const employee = this.selectedEmployee();
    if (!employee) {
      return;
    }

    this.skillError.set(null);
    this.isSkillSaving.set(true);

    this.adminEmployeeService.addEmployeeSkill(employee.id, request).subscribe({
      next: () => {
        this.pageState.setSuccess('Skill added successfully.');
        this.loadSkills(employee.id);
      },
      error: err => {
        this.isSkillSaving.set(false);
        this.skillError.set(err.error?.message ?? 'Unable to add skill.');
      }
    });
  }

  updateSkillProficiency(event: { skillId: string; request: UpdateEmployeeSkillProficiencyRequest }): void {
    const employee = this.selectedEmployee();
    if (!employee) {
      return;
    }

    this.skillError.set(null);
    this.isSkillSaving.set(true);

    this.adminEmployeeService.updateEmployeeSkillProficiency(employee.id, event.skillId, event.request).subscribe({
      next: () => {
        this.pageState.setSuccess('Skill proficiency updated successfully.');
        this.loadSkills(employee.id);
      },
      error: err => {
        this.isSkillSaving.set(false);
        this.skillError.set(err.error?.message ?? 'Unable to update skill proficiency.');
      }
    });
  }

  openManagerDialog(employee: Employee): void {
    if (!this.canUpdateManager(employee)) {
      return;
    }

    this.selectedManagerEmployee.set(employee);
    this.managerError.set(null);
    this.managerUpdatePreview.set(null);
    this.pendingManagerUpdate.set(null);
    this.loadManagers();
  }

  closeManagerDialog(): void {
    this.selectedManagerEmployee.set(null);
    this.activeManagers.set([]);
    this.managerError.set(null);
    this.isManagerLoading.set(false);
    this.isManagerSaving.set(false);
    this.managerUpdatePreview.set(null);
    this.pendingManagerUpdate.set(null);
  }

  prepareManagerUpdate(request: UpdateEmployeeManagerRequest): void {
    const employee = this.selectedManagerEmployee();
    if (!employee) {
      return;
    }

    this.managerError.set(null);

    if (!employee.managerId) {
      this.saveManagerUpdate(employee, request);
      return;
    }

    this.isManagerSaving.set(true);

    this.adminEmployeeService
      .getManagerUpdatePreview(employee.id, request.newManagerId)
      .subscribe({
      next: preview => {
        this.isManagerSaving.set(false);
        this.pendingManagerUpdate.set(request);
        this.managerUpdatePreview.set(preview);
      },
      error: err => {
        this.isManagerSaving.set(false);
        this.managerError.set(err.error?.message ?? 'Unable to prepare manager update.');
      }
    });
  }

  confirmManagerUpdate(): void {
    const employee = this.selectedManagerEmployee();
    const request = this.pendingManagerUpdate();
    if (!employee || !request) {
      return;
    }

    this.saveManagerUpdate(employee, request);
  }

  private saveManagerUpdate(
    employee: Employee,
    request: UpdateEmployeeManagerRequest
  ): void {
    this.isManagerSaving.set(true);
    this.adminEmployeeService.updateManager(employee.id, request).subscribe({
      next: result => {
        this.closeManagerDialog();
        this.pageState.setSuccess(result.message);
        this.loadEmployees();
      },
      error: err => {
        this.isManagerSaving.set(false);
        this.managerError.set(err.error?.message ?? 'Unable to update manager.');
      }
    });
  }

  private loadSkills(employeeId: string): void {
    this.isSkillLoading.set(true);

    this.adminEmployeeService.getEmployeeSkills(employeeId).subscribe({
      next: skills => {
        this.selectedEmployeeSkills.set(skills);
        this.isSkillLoading.set(false);
        this.isSkillSaving.set(false);
      },
      error: err => {
        this.selectedEmployeeSkills.set([]);
        this.isSkillLoading.set(false);
        this.isSkillSaving.set(false);
        this.skillError.set(err.error?.message ?? 'Unable to load employee skills.');
      }
    });
  }

  private loadManagers(): void {
    this.isManagerLoading.set(true);

    this.adminUserService.getActiveManagers().subscribe({
      next: managers => {
        this.activeManagers.set(managers);
        this.isManagerLoading.set(false);
      },
      error: err => {
        this.activeManagers.set([]);
        this.isManagerLoading.set(false);
        this.managerError.set(err.error?.message ?? 'Unable to load managers.');
      }
    });
  }
}
