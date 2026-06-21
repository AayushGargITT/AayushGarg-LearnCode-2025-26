import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateMilestoneRequest,
  CreateProjectRequest,
  Milestone,
  Project,
  ProjectManagerUpdateResult,
  UpdateProjectManagerRequest,
  UpdateMilestoneRequest
} from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class AdminProjectService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/project';

  getAllProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(this.apiUrl);
  }

  createProject(request: CreateProjectRequest): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, request);
  }

  getProjectMilestones(projectId: string): Observable<Milestone[]> {
    return this.http.get<Milestone[]>(`${this.apiUrl}/${projectId}/milestones`);
  }

  addMilestone(projectId: string, request: CreateMilestoneRequest): Observable<Milestone> {
    return this.http.post<Milestone>(`${this.apiUrl}/${projectId}/milestones`, request);
  }

  updateMilestone(
    projectId: string,
    milestoneId: string,
    request: UpdateMilestoneRequest
  ): Observable<Milestone> {
    return this.http.put<Milestone>(
      `${this.apiUrl}/${projectId}/milestones/${milestoneId}`,
      request
    );
  }

  updateManager(
    projectId: string,
    request: UpdateProjectManagerRequest
  ): Observable<ProjectManagerUpdateResult> {
    return this.http.patch<ProjectManagerUpdateResult>(
      `${this.apiUrl}/${projectId}/manager`,
      request
    );
  }
}
