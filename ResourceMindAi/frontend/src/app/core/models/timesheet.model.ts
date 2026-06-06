export enum TimesheetStatus {
  DRAFT = 'Draft',
  SUBMITTED = 'Submitted',
  APPROVED = 'Approved',
  REJECTED = 'Rejected',
  MISSED = 'MISSED'
}

export interface ActivityTag {
  id: string;
  timesheetId: string;
  tagName: string;
}

export interface Timesheet {
  id: string;
  employeeId: string;
  employeeName?: string;
  projectId: string;
  projectName?: string;
  weekStartDate: Date | string;
  hoursLogged: number;
  status: TimesheetStatus | string;
  submittedAt: Date;
}

export interface TimesheetDTO extends Timesheet {
  tags: string[];
}
