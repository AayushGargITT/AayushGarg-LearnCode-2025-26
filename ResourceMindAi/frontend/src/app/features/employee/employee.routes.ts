import { Routes } from '@angular/router';
import { EmployeeAllocationsComponent } from './allocations/allocations.component';
import { EmployeeTimesheetSubmitComponent } from './timesheet-submit/timesheet-submit.component';
import { EmployeeTimesheetHistoryComponent } from './timesheet-history/timesheet-history.component';

export const employeeRoutes: Routes = [
  { path: 'allocations', component: EmployeeAllocationsComponent },
  { path: 'timesheets/submit', component: EmployeeTimesheetSubmitComponent },
  { path: 'timesheets/history', component: EmployeeTimesheetHistoryComponent },
  { path: '', redirectTo: 'allocations', pathMatch: 'full' }
];
