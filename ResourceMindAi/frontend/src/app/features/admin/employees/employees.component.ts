import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

type EmployeeAction = 'toggleStatus';
type EmployeeActionItem = RowActionItem<EmployeeAction>;

interface EmployeeRow {
  id: string;
  name: string;
  dept: string;
  title: string;
  status: string;
  active: 'Active' | 'Inactive';
}

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonsModule,
    DialogModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    ConfirmDialogComponent,
    PageFeedbackComponent,
    RowActionMenuComponent
  ],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.css',
  providers: [PageStateService]
})
export class AdminEmployeesComponent {
  readonly pageState = inject(PageStateService);

  filter = signal<string>('All');
  drawerOpen = signal(false);
  actionEmployee = signal<EmployeeRow | null>(null);

  rows = signal<EmployeeRow[]>([
    { id: 'E-2001', name: 'Elena Patel', dept: 'Engineering', title: 'Senior Backend Engineer', status: 'ALLOCATED', active: 'Active' },
    { id: 'E-2002', name: 'Jonas Weber', dept: 'Engineering', title: 'Frontend Engineer', status: 'BENCH', active: 'Active' },
    { id: 'E-2003', name: 'Maya Chen', dept: 'QA', title: 'QA Lead', status: 'ALLOCATED', active: 'Active' },
    { id: 'E-2004', name: 'Diego Alvarez', dept: 'DevOps', title: 'Platform Engineer', status: 'BENCH', active: 'Active' },
    { id: 'E-2005', name: 'Ada Okonkwo', dept: 'Engineering', title: 'Staff Engineer', status: 'ALLOCATED', active: 'Active' },
    { id: 'E-2006', name: 'Tomas Silva', dept: 'Engineering', title: 'Backend Engineer', status: 'BENCH', active: 'Inactive' },
  ]);

  filteredRows = computed(() => {
    const filter = this.filter();
    return this.rows().filter(row =>
      filter === 'All' || (filter === 'Bench' ? row.status === 'BENCH' : row.status === 'ALLOCATED'));
  });

  setFilter(filter: string): void {
    this.filter.set(filter);
  }

  openDrawer(): void {
    this.pageState.clearFeedback();
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  actionsFor(row: EmployeeRow): EmployeeActionItem[] {
    return [
      { text: row.active === 'Active' ? 'Deactivate Employee' : 'Activate Employee', action: 'toggleStatus' }
    ];
  }

  onEmployeeAction(row: EmployeeRow, item: EmployeeActionItem): void {
    if (item.action !== 'toggleStatus') {
      return;
    }

    this.pageState.clearFeedback();
    this.actionEmployee.set(row);
  }

  closeConfirmDialog(): void {
    this.actionEmployee.set(null);
    this.pageState.stopAction();
  }

  confirmToggleStatus(): void {
    const employee = this.actionEmployee();
    if (!employee) {
      return;
    }

    this.pageState.startAction(employee.id);
    this.rows.update(rows => rows.map(row => row.id === employee.id
      ? { ...row, active: row.active === 'Active' ? 'Inactive' : 'Active' }
      : row));
    this.pageState.setSuccess(`${employee.name} ${employee.active === 'Active' ? 'deactivated' : 'activated'} successfully.`);
    this.closeConfirmDialog();
  }
}
