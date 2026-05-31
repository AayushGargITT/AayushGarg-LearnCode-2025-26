import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private users: User[] = [
    { id: 'U-1001', fullName: 'Asha Rao', email: 'asha@acme.io', username: 'asha.rao', role: 'Admin', isActive: true, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
    { id: 'U-1002', fullName: 'Marcus Lee', email: 'marcus@acme.io', username: 'marcus.lee', role: 'Manager', isActive: true, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
    { id: 'U-1003', fullName: 'Priya Shah', email: 'priya@acme.io', username: 'priya.shah', role: 'Manager', isActive: true, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
    { id: 'U-1004', fullName: 'Elena Patel', email: 'elena@acme.io', username: 'elena.patel', role: 'Employee', isActive: true, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
    { id: 'U-1005', fullName: 'Jonas Weber', email: 'jonas@acme.io', username: 'jonas.weber', role: 'Employee', isActive: false, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
    { id: 'U-1006', fullName: 'Maya Chen', email: 'maya@acme.io', username: 'maya.chen', role: 'Employee', isActive: true, forcePasswordChange: false, createdAt: new Date(), updatedAt: new Date() },
  ];

  getAllUsers(): Observable<User[]> {
    return of(this.users).pipe(delay(300));
  }

  createUser(user: any): Observable<User> {
    return of(user).pipe(delay(300));
  }
}
