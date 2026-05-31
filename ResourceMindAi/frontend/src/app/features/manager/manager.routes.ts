import { Routes } from '@angular/router';
import { ManagerResourcesComponent } from './resources/resources.component';
import { ManagerAllocateComponent } from './allocate/allocate.component';
import { ManagerProjectsComponent } from './projects/projects.component';
import { ManagerTimesheetsComponent } from './timesheets/timesheets.component';
import { ManagerAiComponent } from './ai/ai.component';

export const managerRoutes: Routes = [
  { path: 'resources', component: ManagerResourcesComponent },
  { path: 'allocate', component: ManagerAllocateComponent },
  { path: 'projects', component: ManagerProjectsComponent },
  { path: 'timesheets', component: ManagerTimesheetsComponent },
  { path: 'ai', component: ManagerAiComponent },
  { path: '', redirectTo: 'resources', pathMatch: 'full' }
];
