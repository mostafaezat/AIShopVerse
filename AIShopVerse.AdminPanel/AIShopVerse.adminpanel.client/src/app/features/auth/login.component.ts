import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <h2>Admin Login</h2>
      <form (ngSubmit)="onSubmit()">
        <div><label>Email</label><input type="email" [(ngModel)]="model.email" name="email" required></div>
        <div><label>Password</label><input type="password" [(ngModel)]="model.password" name="password" required></div>
        <button type="submit" [disabled]="loading">{{ loading ? 'Signing in...' : 'Login' }}</button>
        <p *ngIf="error" class="error">{{ error }}</p>
      </form>
    </div>
  `,
  styles: [`.login-container { max-width: 400px; margin: 2rem auto; padding: 2rem; } form div { margin-bottom: 1rem; } label { display: block; margin-bottom: 0.25rem; } input { width: 100%; padding: 0.5rem; } button { background: #007bff; color: white; padding: 0.5rem 1rem; border: none; cursor: pointer; } .error { color: red; }`]
})
export class LoginComponent {
  model = { email: '', password: '' };
  error = '';
  loading = false;
  constructor(private auth: AuthService, private router: Router) {}
  onSubmit() {
    this.loading = true;
    this.auth.login(this.model).subscribe({
      next: (res) => {
        this.loading = false;
        if (res?.isSuccess && this.auth.isLoggedIn()) {
          this.router.navigateByUrl('/dashboard');
        } else {
          this.error = res?.message || 'Invalid email or password.';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || err?.message || 'Login failed';
      }
    });
  }
}
