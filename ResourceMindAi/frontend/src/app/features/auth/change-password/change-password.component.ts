import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AuthShellComponent } from '../../../shared/components/auth-shell/auth-shell.component';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AuthShellComponent],
  templateUrl: './change-password.component.html'
})
export class ChangePasswordComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  showCurrent = signal(false);
  showNew = signal(false);
  showConfirm = signal(false);
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);

  form = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  passwordStrength(): number {
    const password = this.form.value.newPassword ?? '';
    let score = 0;

    if (password.length >= 8) score += 35;
    if (/[A-Z]/.test(password)) score += 30;
    if (/\d/.test(password)) score += 25;
    if (/[^A-Za-z0-9]/.test(password)) score += 10;

    return Math.min(score, 100);
  }

  get passwordsMatch(): boolean {
    const { newPassword, confirmPassword } = this.form.value;
    return newPassword === confirmPassword;
  }

  onSubmit(): void {
    this.errorMessage.set(null);

    if (this.form.invalid || !this.passwordsMatch) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();
    this.isSubmitting.set(true);

    this.authService.changePassword(currentPassword!, newPassword!, confirmPassword!).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.router.navigate([this.authService.defaultRouteForRole(res.user.role)]);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message ?? 'Unable to update password.');
      }
    });
  }
}
