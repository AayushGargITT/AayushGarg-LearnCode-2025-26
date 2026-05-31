import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Project, ProjectStatus } from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private projects: Project[] = [
    { id: 'P-101', name: 'Atlas Payments', description: 'Payment gateway', startDate: new Date(), endDate: new Date(), status: ProjectStatus.ACTIVE, managerId: 'U-1002', managerName: 'Marcus Lee', health: 'ON_TRACK', createdAt: new Date(), updatedAt: new Date() },
    { id: 'P-102', name: 'Borealis CRM', description: 'CRM migration', startDate: new Date(), endDate: new Date(), status: ProjectStatus.ACTIVE, managerId: 'U-1003', managerName: 'Priya Shah', health: 'NEEDS_ATTENTION', createdAt: new Date(), updatedAt: new Date() },
    { id: 'P-103', name: 'Cobalt Insights', description: 'Data analytics platform', startDate: new Date(), endDate: new Date(), status: ProjectStatus.ON_HOLD, managerId: 'U-1002', managerName: 'Marcus Lee', health: 'AT_RISK', createdAt: new Date(), updatedAt: new Date() },
    { id: 'P-104', name: 'Delta Auth', description: 'SSO provider', startDate: new Date(), endDate: new Date(), status: ProjectStatus.PLANNED, managerId: 'U-1004', managerName: 'Sara Kim', health: 'ON_TRACK', createdAt: new Date(), updatedAt: new Date() },
  ];

  getAllProjects(): Observable<Project[]> {
    return of(this.projects).pipe(delay(300));
  }

  getProjectsByManager(managerId: string): Observable<Project[]> {
    return of(this.projects.filter(p => p.managerId === managerId)).pipe(delay(300));
  }
}
