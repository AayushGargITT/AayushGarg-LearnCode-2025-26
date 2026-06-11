import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateUserRequest,
  DeactivateUserResult,
  User
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AdminUserService {
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

  deactivateUser(userId: string): Observable<DeactivateUserResult> {
    return this.http.patch<DeactivateUserResult>(`${this.apiUrl}/${userId}/deactivate`, {});
  }

  reactivateUser(userId: string): Observable<User> {
    return this.http.patch<User>(`${this.apiUrl}/${userId}/reactivate`, {});
  }
}
