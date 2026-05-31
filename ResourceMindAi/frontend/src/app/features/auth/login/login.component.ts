import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { AuthShellComponent } from '../../../shared/components/auth-shell/auth-shell.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, AuthShellComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  show = signal(false);
  state = signal<'idle' | 'error' | 'deactivated'>('idle');
  
  authService = inject(AuthService);
  router = inject(Router);
  fb = inject(FormBuilder);

  loginForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  toggleShow() {
    this.show.set(!this.show());
  }

  setState(s: 'idle' | 'error' | 'deactivated') {
    this.state.set(s);
  }

  onSubmit() {
    if (this.loginForm.invalid) return;
    
    // Simulate login
    const { username, password } = this.loginForm.value;
    if (this.state() === 'error' || username === 'error') {
      this.state.set('error');
      return;
    }
    
    if (this.state() === 'deactivated') {
      return;
    }
    
    this.authService.login(username!, password!).subscribe({
      next: (res) => {
        // Redirect based on role in a real app, hardcode to admin dashboard for mock
        this.router.navigate(['/admin/dashboard']);
      },
      error: () => this.state.set('error')
    });
  }
}
