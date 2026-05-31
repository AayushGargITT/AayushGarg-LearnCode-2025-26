import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Employee, EmployeeDetailDTO, EmployeeStatus } from '../models/employee.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private employees: Employee[] = [
    { id: 'E-2001', userId: 'U-1004', fullName: 'Elena Patel', email: 'elena@acme.io', department: 'Engineering', designation: 'Senior Backend Engineer', status: EmployeeStatus.ALLOCATED, isActive: true, createdAt: new Date(), updatedAt: new Date() },
    { id: 'E-2002', userId: 'U-1005', fullName: 'Jonas Weber', email: 'jonas@acme.io', department: 'Engineering', designation: 'Frontend Engineer', status: EmployeeStatus.BENCH, isActive: true, createdAt: new Date(), updatedAt: new Date() },
    { id: 'E-2003', userId: 'U-1006', fullName: 'Maya Chen', email: 'maya@acme.io', department: 'QA', designation: 'QA Lead', status: EmployeeStatus.ALLOCATED, isActive: true, createdAt: new Date(), updatedAt: new Date() },
    { id: 'E-2004', userId: 'U-1007', fullName: 'Diego Alvarez', email: 'diego@acme.io', department: 'DevOps', designation: 'Platform Engineer', status: EmployeeStatus.BENCH, isActive: true, createdAt: new Date(), updatedAt: new Date() },
    { id: 'E-2005', userId: 'U-1008', fullName: 'Ada Okonkwo', email: 'ada@acme.io', department: 'Engineering', designation: 'Staff Engineer', status: EmployeeStatus.ALLOCATED, isActive: true, createdAt: new Date(), updatedAt: new Date() },
    { id: 'E-2006', userId: 'U-1009', fullName: 'Tomás Silva', email: 'tomas@acme.io', department: 'Engineering', designation: 'Backend Engineer', status: EmployeeStatus.BENCH, isActive: false, createdAt: new Date(), updatedAt: new Date() },
  ];

  getAllEmployees(): Observable<Employee[]> {
    return of(this.employees).pipe(delay(300));
  }

  getEmployeeById(id: string): Observable<Employee | undefined> {
    return of(this.employees.find(e => e.id === id)).pipe(delay(300));
  }

  getEmployeeDetails(id: string): Observable<EmployeeDetailDTO> {
    const emp = this.employees.find(e => e.id === id);
    if (!emp) throw new Error('Not found');
    
    return of({
      ...emp,
      skills: [],
      activeAllocations: [],
      recentTags: []
    }).pipe(delay(300));
  }
}
