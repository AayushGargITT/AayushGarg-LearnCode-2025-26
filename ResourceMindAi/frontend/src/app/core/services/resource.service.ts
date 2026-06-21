import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ResourceAllocations,
  ResourceTimesheetDetail,
  ResourceTimesheetSummary,
  SubmitResourceTimesheet,
  TimesheetWeek
} from '../models/resource-self.model';

@Injectable({ providedIn: 'root' })
export class ResourceService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/resource';

  getAllocations(): Observable<ResourceAllocations> {
    return this.http.get<ResourceAllocations>(`${this.apiUrl}/allocations`);
  }

  getTimesheetWeek(weekStartDate?: string): Observable<TimesheetWeek> {
    const params = weekStartDate
      ? new HttpParams().set('weekStartDate', weekStartDate)
      : undefined;
    return this.http.get<TimesheetWeek>(`${this.apiUrl}/timesheets/week`, { params });
  }

  submitTimesheet(request: SubmitResourceTimesheet): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/timesheets`, request);
  }

  getTimesheetHistory(): Observable<ResourceTimesheetSummary[]> {
    return this.http.get<ResourceTimesheetSummary[]>(`${this.apiUrl}/timesheets`);
  }

  getTimesheetDetail(weekStartDate: string): Observable<ResourceTimesheetDetail> {
    return this.http.get<ResourceTimesheetDetail>(
      `${this.apiUrl}/timesheets/${weekStartDate.slice(0, 10)}`
    );
  }
}
