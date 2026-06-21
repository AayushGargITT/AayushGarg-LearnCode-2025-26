import { Routes } from '@angular/router';
import { AdminDashboardComponent } from './dashboard/dashboard.component';
import { AdminUsersComponent } from './users/users.component';
import { AdminEmployeesComponent } from './employees/employees.component';
import { AdminEmployeeDetailComponent } from './employee-detail/employee-detail.component';
import { AdminProjectsComponent } from './projects/projects.component';
import { AdminMilestonesComponent } from './milestones/milestones.component';
import { AdminAllocationsComponent } from './allocations/allocations.component';
import { AdminConfigComponent } from './config/config.component';

export const adminRoutes: Routes = [
  { path: 'dashboard', component: AdminDashboardComponent },
  { path: 'users', component: AdminUsersComponent },
  { path: 'employees', component: AdminEmployeesComponent },
  { path: 'employees/:id', component: AdminEmployeeDetailComponent },
  { path: 'projects', component: AdminProjectsComponent },
  { path: 'projects/:id/milestones', component: AdminMilestonesComponent },
  { path: 'allocations', component: AdminAllocationsComponent },
  { path: 'config', component: AdminConfigComponent },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
];
