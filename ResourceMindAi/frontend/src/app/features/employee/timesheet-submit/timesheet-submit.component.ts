import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  EMPLOYEE_ACTIVITY_TAGS,
  SubmitEmployeeTimesheetEntry,
  TimesheetWeek
} from '../../../core/models/employee-self.model';
import { EmployeeService } from '../../../core/services/employee.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { toDateOnlyString } from '../../../shared/utils/date-only.util';

@Component({
  selector: 'app-employee-timesheet-submit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DateInputsModule,
    DropDownsModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    PageFeedbackComponent
  ],
  templateUrl: './timesheet-submit.component.html',
  styleUrl: './timesheet-submit.component.css',
  providers: [PageStateService]
})
export class EmployeeTimesheetSubmitComponent {
  private readonly employeeService = inject(EmployeeService);
  private readonly formBuilder = inject(FormBuilder);
  readonly pageState = inject(PageStateService);

  readonly activityTags = [...EMPLOYEE_ACTIVITY_TAGS];
  readonly week = signal<TimesheetWeek | null>(null);

  readonly form = this.formBuilder.group({
    weekStartDate: [this.getCurrentMonday(), Validators.required],
    entries: this.formBuilder.array([])
  });

  get entries(): FormArray {
    return this.form.controls.entries;
  }

  constructor() {
    this.loadWeek();
  }

  loadWeek(clearFeedback = true): void {
    const selectedDate = this.form.controls.weekStartDate.value;
    if (!selectedDate) {
      return;
    }

    if (clearFeedback) {
      this.pageState.clearFeedback();
    }
    this.pageState.startLoading();
    this.employeeService.getTimesheetWeek(toDateOnlyString(selectedDate)).subscribe({
      next: week => {
        this.week.set(week);
        this.rebuildEntries(week);
        this.pageState.stopLoading();
      },
      error: err => {
        this.week.set(null);
        this.entries.clear();
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load allocations for this week.');
      }
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.pageState.isSubmitting()) {
      return;
    }

    const weekStartDate = this.form.controls.weekStartDate.value;
    if (!weekStartDate) {
      return;
    }

    const entries = (this.entries.getRawValue() as SubmitEmployeeTimesheetEntry[])
      .filter(entry => Number(entry.hours) > 0);
    if (entries.length === 0) {
      this.pageState.setError('Enter hours for at least one allocated project.');
      return;
    }

    if (entries.some(entry => entry.activityTags.length === 0)) {
      this.pageState.setError('Select at least one activity tag for each project with logged hours.');
      return;
    }

    this.pageState.clearFeedback();
    this.pageState.startSubmitting();
    this.employeeService.submitTimesheet({
      weekStartDate: toDateOnlyString(weekStartDate),
      entries
    }).subscribe({
      next: () => {
        this.pageState.stopSubmitting();
        this.pageState.setSuccess('Timesheet submitted successfully.');
        this.loadWeek(false);
      },
      error: err => {
        this.pageState.stopSubmitting();
        this.pageState.setError(err.error?.message ?? 'Unable to submit the timesheet.');
      }
    });
  }

  totalHours(): number {
    return this.entries.controls.reduce(
      (total, control) => total + Number(control.get('hours')?.value ?? 0),
      0);
  }

  private rebuildEntries(week: TimesheetWeek): void {
    this.entries.clear();
    for (const allocation of week.allocations) {
      this.entries.push(this.formBuilder.group({
        projectId: [allocation.projectId, Validators.required],
        hours: [
          0,
          [Validators.required, Validators.min(0), Validators.max(allocation.maxAllowedHours)]
        ],
        activityTags: [[]]
      }));
    }
  }

  private getCurrentMonday(): Date {
    const today = new Date();
    const daysSinceMonday = (today.getDay() + 6) % 7;
    return new Date(today.getFullYear(), today.getMonth(), today.getDate() - daysSinceMonday);
  }
}
