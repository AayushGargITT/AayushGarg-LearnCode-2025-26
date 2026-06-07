import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map } from 'rxjs';
import { AdminDashboard } from '../models/admin-dashboard.model';
import { EmployeeStatus } from '../models/employee.model';
import { HealthStatus, ProjectStatus } from '../models/project.model';
import { AdminEmployeeService } from './admin-employee.service';
import { AdminProjectService } from './admin-project.service';
import { AdminUserService } from './admin-user.service';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly adminUserService = inject(AdminUserService);
  private readonly adminEmployeeService = inject(AdminEmployeeService);
  private readonly adminProjectService = inject(AdminProjectService);

  getDashboard(): Observable<AdminDashboard> {
    return forkJoin({
      users: this.adminUserService.getAllUsers(),
      employees: this.adminEmployeeService.getAllEmployees(),
      projects: this.adminProjectService.getAllProjects()
    }).pipe(
      map(({ users, employees, projects }) => ({
        totalUsers: users.length,
        activeProjects: projects.filter(project => project.status === ProjectStatus.ACTIVE).length,
        employeesOnBench: employees.filter(
          employee => employee.isActive && employee.allocationStatus === EmployeeStatus.BENCH
        ).length,
        atRiskProjects: projects.filter(
          project => project.healthStatus === HealthStatus.RED
        ).length,
        recentProjects: [...projects]
          .sort(
            (left, right) =>
              new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime()
          )
          .slice(0, 5)
      }))
    );
  }
}
