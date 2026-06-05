import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AddEmployeeRequest, CreateUserRequest, User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:44374/api/v1/user';

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }

  getActiveManagers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/active-managers`);
  }

  createUser(user: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.apiUrl, user);
  }

  resetPassword(userId: string): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/reset-password/${userId}`, {});
  }

  toggleStatus(userId: string): Observable<User> {
    return this.http.patch<User>(`${this.apiUrl}/toggle-status/${userId}`, {});
  }

  addEmployee(userId: string, request: AddEmployeeRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/add-employee/${userId}`, request);
  }
}
