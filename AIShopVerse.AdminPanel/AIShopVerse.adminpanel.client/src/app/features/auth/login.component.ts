import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <h2>Admin Login</h2>
      <form (ngSubmit)="onSubmit()" #loginForm="ngForm">
        <div><label>Email</label><input type="email" [(ngModel)]="model.email" name="email" required email
               (ngModelChange)="clearError('email')"></div>
        <div class="field-error" *ngIf="fieldError === 'email'">A valid email is required.</div>
        <div><label>Password</label><input type="password" [(ngModel)]="model.password" name="password" required
               (ngModelChange)="clearError('password')"></div>
        <div class="field-error" *ngIf="fieldError === 'password'">Password is required.</div>
        <button type="submit" [disabled]="loading">{{ loading ? 'Signing in...' : 'Login' }}</button>
        <p *ngIf="error" class="error">{{ error }}</p>
      </form>
    </div>
  `,
  styles: [`.login-container { max-width: 400px; margin: 2rem auto; padding: 2rem; } form div { margin-bottom: 1rem; } label { display: block; margin-bottom: 0.25rem; } input { width: 100%; padding: 0.5rem; } button { background: #007bff; color: white; padding: 0.5rem 1rem; border: none; cursor: pointer; } .error { color: red; } .field-error { color: #dc3545; font-size: 0.8rem; margin-top: -0.6rem; margin-bottom: 0.5rem; }`]
})
export class LoginComponent {
  model = { email: '', password: '' };
  error = '';
  fieldError: 'email' | 'password' | '' = '';
  loading = false;
  private returnUrl: string | null = null;
  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('sessionExpired') === '1') {
      this.error = 'Your session has expired. Please sign in again.';
    }
    const returnUrl = params.get('returnUrl');
    this.returnUrl = returnUrl && returnUrl.startsWith('/') ? returnUrl : null;
  }
  clearError(field: 'email' | 'password') {
    if (this.fieldError === field) this.fieldError = '';
  }
  onSubmit() {
    if (!this.model.email.trim() || !this.model.password) {
      this.fieldError = !this.model.email.trim() ? 'email' : 'password';
      return;
    }
    this.fieldError = '';
    this.loading = true;
    this.auth.login(this.model).subscribe({
      next: (res) => {
        this.loading = false;
        if (res?.isSuccess && this.auth.isLoggedIn()) {
          this.router.navigateByUrl(this.returnUrl || '/dashboard');
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
