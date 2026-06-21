import { Routes } from '@angular/router';
import { ManagerResourcesComponent } from './resources/resources.component';
import { ManagerAllocateComponent } from './allocate/allocate.component';
import { ManagerProjectsComponent } from './projects/projects.component';
import { ManagerTimesheetsComponent } from './timesheets/timesheets.component';
import { ManagerTeamBuilderComponent } from './team-builder/team-builder.component';

export const managerRoutes: Routes = [
  { path: 'resources', component: ManagerResourcesComponent },
  { path: 'allocate', component: ManagerAllocateComponent },
  { path: 'team-builder', component: ManagerTeamBuilderComponent },
  { path: 'projects', component: ManagerProjectsComponent },
  { path: 'timesheets', component: ManagerTimesheetsComponent },
  { path: '', redirectTo: 'resources', pathMatch: 'full' }
];
