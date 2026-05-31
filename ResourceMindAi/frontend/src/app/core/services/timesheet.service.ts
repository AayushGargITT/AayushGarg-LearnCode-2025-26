import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Timesheet, TimesheetDTO, TimesheetStatus } from '../models/timesheet.model';

@Injectable({ providedIn: 'root' })
export class TimesheetService {
  private timesheets: TimesheetDTO[] = [
    { id: 'T-401', employeeId: 'E-2001', employeeName: 'Elena Patel', projectId: 'P-101', projectName: 'Atlas Payments', weekStartDate: new Date(), hoursLogged: 40, status: TimesheetStatus.SUBMITTED, tags: ['Backend API', 'Bug Fixes'], submittedAt: new Date() },
  ];

  getTimesheetsByEmployee(employeeId: string): Observable<TimesheetDTO[]> {
    return of(this.timesheets.filter(t => t.employeeId === employeeId)).pipe(delay(300));
  }
}
