import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateManagerAllocationRequest,
  FindResourceRequest,
  ManagerAllocation,
  ManagerProject,
  ManagerProjectDetail,
  ProjectRiskSummary,
  ManagerResource,
  ManagerResourceDashboard,
  ManagerTimesheet,
  ResourceMatchResponse
} from '../models/manager.model';

@Injectable({ providedIn: 'root' })
export class ManagerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/manager';

  getResourceDashboard(): Observable<ManagerResourceDashboard> {
    return this.http.get<ManagerResourceDashboard>(`${this.apiUrl}/resources`);
  }

  getResourceDetail(employeeId: string): Observable<ManagerResource> {
    return this.http.get<ManagerResource>(`${this.apiUrl}/resources/${employeeId}`);
  }

  getProjects(): Observable<ManagerProject[]> {
    return this.http.get<ManagerProject[]>(`${this.apiUrl}/projects`);
  }

  getProjectDetail(projectId: string): Observable<ManagerProjectDetail> {
    return this.http.get<ManagerProjectDetail>(`${this.apiUrl}/projects/${projectId}`);
  }

  generateProjectRiskSummary(projectId: string): Observable<ProjectRiskSummary> {
    return this.http.post<ProjectRiskSummary>(
      `${this.apiUrl}/projects/${projectId}/risk-summary`,
      {}
    );
  }

  getSubmittedTimesheets(): Observable<ManagerTimesheet[]> {
    return this.http.get<ManagerTimesheet[]>(`${this.apiUrl}/timesheets`);
  }

  findResources(request: FindResourceRequest): Observable<ResourceMatchResponse> {
    return this.http.post<ResourceMatchResponse>(`${this.apiUrl}/resources/find`, request);
  }

  allocate(request: CreateManagerAllocationRequest): Observable<ManagerAllocation> {
    return this.http.post<ManagerAllocation>(`${this.apiUrl}/allocations`, request);
  }

  endAllocation(allocationId: string): Observable<ManagerAllocation> {
    return this.http.patch<ManagerAllocation>(`${this.apiUrl}/allocations/${allocationId}/end`, {});
  }
}
