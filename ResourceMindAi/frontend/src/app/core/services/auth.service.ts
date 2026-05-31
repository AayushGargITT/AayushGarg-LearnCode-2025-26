import { Injectable, signal } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { User, Role } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // A signal to hold the currently logged-in user
  currentUser = signal<User | null>(null);

  constructor() {}

  login(username: string, password: string): Observable<{ user: User, token: string }> {
    // Mock login logic
    if (username === 'error') {
      return throwError(() => new Error('Invalid credentials'));
    }
    
    const mockUser: User = {
      id: 'U-1001',
      fullName: 'Asha Rao',
      email: 'asha@acme.io',
      username: username,
      role: Role.ADMIN,
      isActive: true,
      forcePasswordChange: false,
      createdAt: new Date(),
      updatedAt: new Date()
    };

    // Update the signal
    this.currentUser.set(mockUser);
    
    return of({ user: mockUser, token: 'mock-jwt-token' }).pipe(delay(500));
  }

  logout(): void {
    this.currentUser.set(null);
  }

  signUp(signUpRequest: any): Observable<User> {
    return of(signUpRequest).pipe(delay(500));
  }

  changePassword(newPassword: string): Observable<boolean> {
    const user = this.currentUser();
    if (user) {
      this.currentUser.set({ ...user, forcePasswordChange: false });
    }
    return of(true).pipe(delay(500));
  }
}
