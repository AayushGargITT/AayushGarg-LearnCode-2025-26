import { HealthStatus, MilestoneStatus, ProjectStatus } from './project.model';
import { TimesheetStatus } from './timesheet.model';

export interface ManagerAllocation {
  id: string;
  employeeId: string;
  employeeName: string;
  projectId: string;
  projectName: string;
  utilisationPercent: number;
  fromDate: Date | string;
  toDate: Date | string | null;
  isActive: boolean;
}

export interface ManagerResource {
  id: string;
  userId: string;
  fullName: string;
  department: string;
  designation: string;
  currentStatus: 'Bench' | 'Partial' | 'Full' | string;
  allocationPercent: number;
  skills: string[];
  activeAllocations: ManagerAllocation[];
  recentActivityTags: string[];
}

export interface ManagerResourceDashboard {
  onBench: ManagerResource[];
  activeEmployees: ManagerResource[];
}

export interface ManagerProject {
  id: string;
  name: string;
  description: string;
  startDate: Date | string;
  endDate: Date | string | null;
  status: ProjectStatus | string;
  healthStatus: HealthStatus | string;
  teamSize: number;
}

export interface ManagerMilestone {
  id: string;
  title: string;
  dueDate: Date | string;
  status: MilestoneStatus | string;
}

export interface ManagerProjectDetail extends ManagerProject {
  milestones: ManagerMilestone[];
  allocatedResources: ManagerAllocation[];
  riskFlags: string[];
  riskSummary: string[];
}

export interface ManagerTimesheet {
  id: string;
  employeeName: string;
  projectName: string;
  weekStartDate: Date | string;
  hoursLogged: number;
  status: TimesheetStatus | string;
  tags: string[];
}

export interface CreateManagerAllocationRequest {
  projectId: string;
  employeeId: string;
  utilisationPercent: number;
  fromDate: Date | string;
  toDate: Date | string | null;
}

export interface FindResourceRequest {
  projectId: string;
  requirement: string;
}

export interface ResourceIntent {
  requiredRole: string | null;
  requiredSkills: string[];
  experienceHint: string | null;
  availabilityRequirement: number | null;
  fromDate: string | null;
  toDate: string | null;
  prioritySignals: string[];
  softConstraints: string[];
  exclusionConstraints: string[];
}

export interface ResourceMatch {
  employee: ManagerResource;
  score: number;
  reasons: string[];
}

export interface ResourceMatchResponse {
  intent: ResourceIntent;
  matches: ResourceMatch[];
}
