import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { ManagerTimesheet } from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

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
    PageFeedbackComponent
  ],
  templateUrl: './timesheets.component.html',
  styleUrl: './timesheets.component.css',
  providers: [PageStateService]
})
export class ManagerTimesheetsComponent {
  private readonly managerService = inject(ManagerService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<ManagerTimesheet[]>([]);
  readonly selectedTimesheet = signal<ManagerTimesheet | null>(null);
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
    this.managerService.getSubmittedTimesheets().subscribe({
      next: timesheets => {
        this.rows.set(timesheets);
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

  private toDateKey(value: Date | string): string {
    const date = new Date(value);
    return `${date.getUTCFullYear()}-${date.getUTCMonth()}-${date.getUTCDate()}`;
  }
}
