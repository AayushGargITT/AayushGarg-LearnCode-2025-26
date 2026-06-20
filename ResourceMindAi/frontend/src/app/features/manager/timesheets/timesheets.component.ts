import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { forkJoin } from 'rxjs';
import {
  FrozenTimesheetSubmission,
  ManagerTimesheet
} from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { formatDateOnly, toDateOnlyString } from '../../../shared/utils/date-only.util';

@Component({
  selector: 'app-manager-timesheets',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    DialogModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    AvatarComponent,
    StatusBadgeComponent,
    PageFeedbackComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './timesheets.component.html',
  providers: [PageStateService]
})
export class ManagerTimesheetsComponent {
  private readonly managerService = inject(ManagerService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<ManagerTimesheet[]>([]);
  readonly frozenSubmissions = signal<FrozenTimesheetSubmission[]>([]);
  readonly selectedTimesheet = signal<ManagerTimesheet | null>(null);
  readonly restoreCandidate = signal<FrozenTimesheetSubmission | null>(null);
  readonly selectedWeekEntries = computed(() => {
    const selected = this.selectedTimesheet();
    if (!selected) {
      return [];
    }

    const selectedWeek = this.toDateKey(selected.weekStartDate);
    return this.rows().filter(timesheet =>
      timesheet.employeeId === selected.employeeId
      && this.toDateKey(timesheet.weekStartDate) === selectedWeek);
  });
  readonly selectedWeekTotal = computed(() =>
    this.selectedWeekEntries().reduce(
      (total, timesheet) => total + timesheet.hoursLogged,
      0
    ));

  constructor() {
    this.loadTimesheets();
  }

  loadTimesheets(): void {
    this.pageState.startLoading();
    forkJoin({
      timesheets: this.managerService.getSubmittedTimesheets(),
      frozen: this.managerService.getFrozenTimesheetSubmissions()
    }).subscribe({
      next: ({ timesheets, frozen }) => {
        this.rows.set(timesheets);
        this.frozenSubmissions.set(frozen);
        this.pageState.stopLoading();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load timesheets.');
      }
    });
  }

  openDetail(timesheet: ManagerTimesheet): void {
    this.selectedTimesheet.set(timesheet);
  }

  closeDetail(): void {
    this.selectedTimesheet.set(null);
  }

  requestRestore(issue: FrozenTimesheetSubmission): void {
    this.restoreCandidate.set(issue);
  }

  cancelRestore(): void {
    this.restoreCandidate.set(null);
  }

  confirmRestore(): void {
    const issue = this.restoreCandidate();
    if (!issue) {
      return;
    }

    this.pageState.startAction(issue.employeeUserId);
    this.managerService.restoreTimesheetSubmissionAccess(
      issue.employeeUserId,
      issue.weekStartDate
    ).subscribe({
      next: () => {
        this.restoreCandidate.set(null);
        this.pageState.stopAction();
        this.pageState.setSuccess('Timesheet submission access restored.');
        this.loadTimesheets();
      },
      error: err => {
        this.restoreCandidate.set(null);
        this.pageState.stopAction();
        this.pageState.setError(
          err.error?.message ?? 'Unable to restore timesheet submission access.'
        );
      }
    });
  }

  formatWeekStart(value: Date | string): string {
    return formatDateOnly(value);
  }

  private toDateKey(value: Date | string): string {
    return toDateOnlyString(value);
  }
}
