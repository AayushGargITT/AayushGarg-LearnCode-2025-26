export const EMPLOYEE_ACTIVITY_TAGS = [
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

export interface EmployeeAllocation {
  id: string;
  projectId: string;
  projectName: string;
  utilisationPercent: number;
  fromDate: string;
  toDate: string;
  status: 'Active' | 'Upcoming' | 'Ended';
}

export interface EmployeeAllocations {
  totalCurrentUtilisationPercent: number;
  allocations: EmployeeAllocation[];
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

export interface SubmitEmployeeTimesheetEntry {
  projectId: string;
  hours: number;
  activityTags: string[];
}

export interface SubmitEmployeeTimesheet {
  weekStartDate: string;
  entries: SubmitEmployeeTimesheetEntry[];
}

export interface EmployeeTimesheetSummary {
  weekStartDate: string;
  totalHours: number;
  status: 'Submitted' | 'Missed';
}

export interface EmployeeTimesheetEntry {
  projectId: string;
  projectName: string;
  hours: number;
  activityTags: string[];
}

export interface EmployeeTimesheetDetail extends EmployeeTimesheetSummary {
  entries: EmployeeTimesheetEntry[];
}
