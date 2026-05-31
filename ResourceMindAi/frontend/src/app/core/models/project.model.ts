export enum ProjectStatus {
  PLANNED = 'PLANNED',
  ACTIVE = 'ACTIVE',
  ON_HOLD = 'ON_HOLD',
  COMPLETED = 'COMPLETED'
}

export enum MilestoneStatus {
  NOT_STARTED = 'NOT_STARTED',
  IN_PROGRESS = 'IN_PROGRESS',
  DONE = 'DONE'
}

export interface Milestone {
  id: string;
  projectId: string;
  title: string;
  dueDate: Date | string;
  status: MilestoneStatus | string;
}

export interface Project {
  id: string;
  name: string;
  description: string;
  startDate: Date | string;
  endDate: Date | string;
  status: ProjectStatus | string;
  managerId: string;
  managerName?: string;
  health?: 'ON_TRACK' | 'NEEDS_ATTENTION' | 'AT_RISK';
  createdAt: Date;
  updatedAt: Date;
}

export interface ProjectDetailDTO extends Project {
  milestones: Milestone[];
  allocations: any[]; // To be defined with Allocation
}
