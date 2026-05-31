import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Allocation, AllocationDTO } from '../models/allocation.model';

@Injectable({ providedIn: 'root' })
export class AllocationService {
  private allocations: AllocationDTO[] = [
    { id: 'A-301', employeeId: 'E-2001', employeeName: 'Elena Patel', employeeDesignation: 'Senior Backend Engineer', projectId: 'P-101', projectName: 'Atlas Payments', projectManager: 'Marcus Lee', utilisationPercent: 100, fromDate: new Date(), toDate: new Date(), isActive: true, createdAt: new Date() },
  ];

  getAllAllocations(): Observable<AllocationDTO[]> {
    return of(this.allocations).pipe(delay(300));
  }
}
