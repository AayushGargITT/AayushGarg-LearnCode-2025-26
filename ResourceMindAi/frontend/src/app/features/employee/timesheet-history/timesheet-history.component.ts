import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import {
  EmployeeTimesheetDetail,
  EmployeeTimesheetSummary
} from '../../../core/models/employee-self.model';
import { EmployeeService } from '../../../core/services/employee.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-employee-timesheet-history',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    DialogModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    PageFeedbackComponent,
    StatusBadgeComponent
  ],
  templateUrl: './timesheet-history.component.html',
  providers: [PageStateService]
})
export class EmployeeTimesheetHistoryComponent {
  private readonly employeeService = inject(EmployeeService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<EmployeeTimesheetSummary[]>([]);
  readonly selectedWeek = signal<EmployeeTimesheetDetail | null>(null);
  readonly isDetailLoading = signal(false);

  constructor() {
    this.loadHistory();
  }

  loadHistory(): void {
    this.pageState.startLoading();
    this.employeeService.getTimesheetHistory().subscribe({
      next: timesheets => {
        this.rows.set(timesheets);
        this.pageState.stopLoading();
      },
      error: err => {
        this.rows.set([]);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load timesheet history.');
      }
    });
  }

  openDetail(timesheet: EmployeeTimesheetSummary): void {
    this.pageState.clearFeedback();
    this.isDetailLoading.set(true);
    this.employeeService.getTimesheetDetail(timesheet.weekStartDate).subscribe({
      next: detail => {
        this.selectedWeek.set(detail);
        this.isDetailLoading.set(false);
      },
      error: err => {
        this.isDetailLoading.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to load timesheet details.');
      }
    });
  }

  closeDetail(): void {
    this.selectedWeek.set(null);
  }
}
