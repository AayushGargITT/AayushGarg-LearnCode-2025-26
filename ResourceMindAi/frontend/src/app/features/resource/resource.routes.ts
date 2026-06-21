import { Routes } from '@angular/router';
import { ResourceAllocationsComponent } from './allocations/allocations.component';
import { ResourceTimesheetSubmitComponent } from './timesheet-submit/timesheet-submit.component';
import { ResourceTimesheetHistoryComponent } from './timesheet-history/timesheet-history.component';

export const resourceRoutes: Routes = [
  { path: 'allocations', component: ResourceAllocationsComponent },
  { path: 'timesheets/submit', component: ResourceTimesheetSubmitComponent },
  { path: 'timesheets/history', component: ResourceTimesheetHistoryComponent },
  { path: '', redirectTo: 'allocations', pathMatch: 'full' }
];
