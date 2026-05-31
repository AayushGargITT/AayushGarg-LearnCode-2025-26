import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GridModule } from '@progress/kendo-angular-grid';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-employee-timesheet-history',
  standalone: true,
  imports: [CommonModule, GridModule, AppLayoutComponent, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './timesheet-history.component.html',
  styleUrl: './timesheet-history.component.css'
})
export class EmployeeTimesheetHistoryComponent {
  rows = [
    { week: "Oct 14, 2024", project: "Atlas Payments", hours: 40, status: "SUBMITTED" },
    { week: "Oct 07, 2024", project: "Atlas Payments", hours: 40, status: "SUBMITTED" },
    { week: "Sep 30, 2024", project: "Atlas Payments", hours: 38, status: "SUBMITTED" },
    { week: "Sep 23, 2024", project: "Atlas Payments", hours: 40, status: "SUBMITTED" },
  ];
}
