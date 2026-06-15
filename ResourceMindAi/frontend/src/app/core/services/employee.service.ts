import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  EmployeeAllocations,
  EmployeeTimesheetDetail,
  EmployeeTimesheetSummary,
  SubmitEmployeeTimesheet,
  TimesheetWeek
} from '../models/employee-self.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/resource';

  getAllocations(): Observable<EmployeeAllocations> {
    return this.http.get<EmployeeAllocations>(`${this.apiUrl}/allocations`);
  }

  getTimesheetWeek(weekStartDate?: string): Observable<TimesheetWeek> {
    const params = weekStartDate
      ? new HttpParams().set('weekStartDate', weekStartDate)
      : undefined;
    return this.http.get<TimesheetWeek>(`${this.apiUrl}/timesheets/week`, { params });
  }

  submitTimesheet(request: SubmitEmployeeTimesheet): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/timesheets`, request);
  }

  getTimesheetHistory(): Observable<EmployeeTimesheetSummary[]> {
    return this.http.get<EmployeeTimesheetSummary[]>(`${this.apiUrl}/timesheets`);
  }

  getTimesheetDetail(weekStartDate: string): Observable<EmployeeTimesheetDetail> {
    return this.http.get<EmployeeTimesheetDetail>(
      `${this.apiUrl}/timesheets/${weekStartDate.slice(0, 10)}`
    );
  }
}
