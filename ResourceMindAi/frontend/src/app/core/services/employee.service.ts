import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  CreateEmployeeSkillRequest,
  Employee,
  EmployeeDetailDTO,
  EmployeeSkill,
  UpdateEmployeeSkillProficiencyRequest
} from '../models/employee.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/employee';

  getAllEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }

  getEmployeeById(userId: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/${userId}`);
  }

  getEmployeeDetails(userId: string): Observable<EmployeeDetailDTO> {
    return this.getEmployeeById(userId).pipe(
      map(employee => ({
        ...employee,
        skills: [],
        activeAllocations: [],
        recentTags: []
      }))
    );
  }

  getEmployeeSkills(employeeId: string): Observable<EmployeeSkill[]> {
    return this.http.get<EmployeeSkill[]>(`${this.apiUrl}/${employeeId}/skills`);
  }

  addEmployeeSkill(employeeId: string, request: CreateEmployeeSkillRequest): Observable<EmployeeSkill> {
    return this.http.post<EmployeeSkill>(`${this.apiUrl}/${employeeId}/skills`, request);
  }

  updateEmployeeSkillProficiency(
    employeeId: string,
    skillId: string,
    request: UpdateEmployeeSkillProficiencyRequest
  ): Observable<EmployeeSkill> {
    return this.http.patch<EmployeeSkill>(`${this.apiUrl}/${employeeId}/skills/${skillId}/proficiency`, request);
  }
}
