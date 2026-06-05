export enum ProjectStatus {
  PLANNED = 'Planned',
  ACTIVE = 'Active',
  COMPLETED = 'Completed',
  ON_HOLD = 'OnHold',
  CANCELLED = 'Cancelled'
}

export enum HealthStatus {
  GREEN = 'Green',
  AMBER = 'Amber',
  RED = 'Red'
}

export enum MilestoneStatus {
  PENDING = 'Pending',
  IN_PROGRESS = 'InProgress',
  COMPLETED = 'Completed',
  OVERDUE = 'Overdue'
}

export interface Milestone {
  id: string;
  projectId: string;
  title: string;
  dueDate: Date | string;
  status: MilestoneStatus | string;
}

export interface CreateMilestoneRequest {
  title: string;
  dueDate: Date | string;
  status: MilestoneStatus;
}

export interface UpdateMilestoneRequest {
  title: string;
  dueDate: Date | string;
  status: MilestoneStatus;
}

export interface Project {
  id: string;
  name: string;
  description: string;
  startDate: Date | string;
  endDate: Date | string | null;
  status: ProjectStatus | string;
  healthStatus: HealthStatus | string;
  managerId: string;
  managerName: string;
  createdAt: Date | string;
  updatedAt?: Date | string | null;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
  startDate: Date | string;
  endDate: Date | string;
  status: ProjectStatus;
  managerId: string;
}

export interface ProjectDetailDTO extends Project {
  milestones: Milestone[];
  allocations: any[];
}
