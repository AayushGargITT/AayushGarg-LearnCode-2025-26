export const RESOURCE_ACTIVITY_TAGS = [
  'Backend API Development',
  'Microservices / Architecture',
  'Database Design & Queries',
  'WebSocket / Real-time Features',
  'Frontend Development',
  'Code Review / Mentoring',
  'Bug Fixing',
  'DevOps / Deployment',
  'Testing & QA',
  'Documentation',
  'Other'
] as const;

export interface ResourceAllocation {
  id: string;
  projectId: string;
  projectName: string;
  utilisationPercent: number;
  fromDate: string;
  toDate: string;
  status: 'Active' | 'Upcoming' | 'Ended';
}

export interface ResourceAllocations {
  totalCurrentUtilisationPercent: number;
  allocations: ResourceAllocation[];
}

export interface TimesheetWeekAllocation {
  projectId: string;
  projectName: string;
  allocationPercent: number;
  maxAllowedHours: number;
}

export interface TimesheetWeek {
  weekStartDate: string;
  maxWeeklyHours: number;
  allocations: TimesheetWeekAllocation[];
}

export interface SubmitResourceTimesheetEntry {
  projectId: string;
  hours: number;
  activityTags: string[];
}

export interface SubmitResourceTimesheet {
  weekStartDate: string;
  entries: SubmitResourceTimesheetEntry[];
}

export interface ResourceTimesheetSummary {
  weekStartDate: string;
  totalHours: number;
  status: 'Submitted' | 'Missed';
}

export interface ResourceTimesheetEntry {
  projectId: string;
  projectName: string;
  hours: number;
  activityTags: string[];
}

export interface ResourceTimesheetDetail extends ResourceTimesheetSummary {
  entries: ResourceTimesheetEntry[];
}
