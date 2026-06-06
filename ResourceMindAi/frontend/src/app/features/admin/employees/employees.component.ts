import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  CreateEmployeeSkillRequest,
  Employee,
  EmployeeSkill,
  EmployeeStatus,
  UpdateEmployeeSkillProficiencyRequest
} from '../../../core/models/employee.model';
import { Role } from '../../../core/models/user.model';
import { AdminEmployeeService } from '../../../core/services/admin-employee.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { EmployeeSkillsDialogComponent } from './components/employee-skills-dialog/employee-skills-dialog.component';

type EmployeeAction = 'manageSkills';
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
    EmployeeSkillsDialogComponent
  ],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.css',
  providers: [PageStateService]
})
export class AdminEmployeesComponent {
  private readonly adminEmployeeService = inject(AdminEmployeeService);
  readonly pageState = inject(PageStateService);

  readonly filter = signal<string>('All');
  readonly searchTerm = signal<string>('');
  readonly rows = signal<Employee[]>([]);
  readonly selectedEmployee = signal<Employee | null>(null);
  readonly selectedEmployeeSkills = signal<EmployeeSkill[]>([]);
  readonly isSkillLoading = signal(false);
  readonly isSkillSaving = signal(false);
  readonly skillError = signal<string | null>(null);

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
    return employee.role === Role.EMPLOYEE;
  }

  actionsFor(employee: Employee): EmployeeActionItem[] {
    if (!this.canManageSkills(employee)) {
      return [];
    }

    return [{ text: 'Manage Skills', action: 'manageSkills' }];
  }

  onEmployeeAction(employee: Employee, item: EmployeeActionItem): void {
    if (item.action === 'manageSkills') {
      this.openSkills(employee);
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
}
