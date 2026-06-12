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
  currentStatus: 'Bench' | 'Allocated' | string;
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
  riskSummary: ProjectRiskSummary | null;
}

export interface ProjectRiskSummary {
  overallHealth: 'ON_TRACK' | 'ATTENTION' | 'AT_RISK';
  summary: string;
  riskPoints: ProjectRiskPoint[];
  recommendedActions: string[];
  suggestedSkills: string[];
  generatedAt: Date | string;
}

export interface ProjectRiskPoint {
  severity: 'LOW' | 'MEDIUM' | 'HIGH';
  title: string;
  description: string;
}

export interface ManagerTimesheet {
  id: string;
  employeeId: string;
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
  availablePercent: number;
  isUnderCurrentManager: boolean;
  reasons: string[];
  aiRank: number | null;
  aiReason: string;
  strengths: string[];
  concerns: string[];
}

export interface ResourceMatchResponse {
  intent: ResourceIntent;
  matches: ResourceMatch[];
}

export interface BuildTeamRequest {
  projectId: string;
  requirement: string;
}

export interface TeamBuilderMember {
  employee: ManagerResource;
  suggestedRole: string;
  reason: string;
  matchedSkills: string[];
}

export interface TeamBuilderUnavailableMember {
  employee: ManagerResource;
  matchedRole: string;
  reason: string;
  matchedSkills: string[];
}

export interface TeamBuilderResponse {
  intent: ResourceIntent;
  teamSummary: string;
  members: TeamBuilderMember[];
  unavailableMatches: TeamBuilderUnavailableMember[];
  missingSkills: string[];
}
