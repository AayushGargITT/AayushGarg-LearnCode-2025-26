import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { Role, User } from '../models/user.model';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = 'https://localhost:44374/api/v1';
  private readonly storageKey = 'resourceMindUser';

  currentUser = signal<User | null>(this.loadUser());

  login(username: string, password: string): Observable<{ user: User }> {
    return this.http.post<{ user: User }>(`${this.apiUrl}/auth/login`, { username, password }).pipe(
      tap((res) => this.setCurrentUser(res.user))
    );
  }

  changePassword(currentPassword: string, newPassword: string, confirmPassword: string): Observable<{ user: User }> {
    const user = this.currentUser();
    if (!user) {
      throw new Error('No user is logged in.');
    }

    return this.http.post<{ user: User }>(`${this.apiUrl}/auth/change-password`, {
      userId: user.id,
      currentPassword,
      newPassword,
      confirmPassword
    }).pipe(
      tap((res) => this.setCurrentUser(res.user))
    );
  }

  logout(): void {
    this.http.post<void>(`${this.apiUrl}/auth/logout`, {}).subscribe();
    sessionStorage.removeItem(this.storageKey);
    this.router.navigate(['/login'])
  }

  setCurrentUser(user: User): void {
    this.currentUser.set(user);
    sessionStorage.setItem(this.storageKey, JSON.stringify(user));
  }

  defaultRouteForRole(role: Role | string): string {
    const normalized = String(role).toLowerCase();
    if (normalized === 'admin') return '/admin/dashboard';
    if (normalized === 'manager') return '/manager/resources';
    return '/employee/allocations';
  }

  private loadUser(): User | null {
    const raw = sessionStorage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as User;
    } catch {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
