import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import {
  ResourceTimesheetDetail,
  ResourceTimesheetSummary
} from '../../../core/models/resource-self.model';
import { ResourceService } from '../../../core/services/resource.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { formatDateOnly } from '../../../shared/utils/date-only.util';

@Component({
  selector: 'app-resource-timesheet-history',
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
export class ResourceTimesheetHistoryComponent {
  private readonly resourceService = inject(ResourceService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<ResourceTimesheetSummary[]>([]);
  readonly selectedWeek = signal<ResourceTimesheetDetail | null>(null);
  readonly isDetailLoading = signal(false);

  constructor() {
    this.loadHistory();
  }

  loadHistory(): void {
    this.pageState.startLoading();
    this.resourceService.getTimesheetHistory().subscribe({
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

  openDetail(timesheet: ResourceTimesheetSummary): void {
    this.pageState.clearFeedback();
    this.isDetailLoading.set(true);
    this.resourceService.getTimesheetDetail(timesheet.weekStartDate).subscribe({
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

  formatWeekStart(value: Date | string): string {
    return formatDateOnly(value);
  }
}
