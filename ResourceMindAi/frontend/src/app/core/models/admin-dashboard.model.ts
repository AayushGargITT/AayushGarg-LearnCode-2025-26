import { Project } from './project.model';

export interface AdminDashboard {
  totalUsers: number;
  activeProjects: number;
  employeesOnBench: number;
  atRiskProjects: number;
  recentProjects: Project[];
}
