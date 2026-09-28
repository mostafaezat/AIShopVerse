import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-wrapper">
      <div class="auth-card">
        <div class="auth-header">
          <h2>Create Account</h2>
          <p>Join AIShopVerse today</p>
        </div>

        <div class="google-btn-wrap" *ngIf="googleClientId">
          <button class="google-btn" type="button" (click)="registerWithGoogle()" [disabled]="loading">
            <svg width="18" height="18" viewBox="0 0 24 24"><path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 01-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"/><path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/><path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/><path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/></svg>
            Sign up with Google
          </button>
        </div>

        <div class="divider" *ngIf="googleClientId"><span>or</span></div>

        <form (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>Full Name</label>
            <input type="text" class="form-control" [class.is-invalid]="nameTouched && nameError"
                   placeholder="John Doe"
                   [(ngModel)]="model.fullName" name="fullName"
                   (ngModelChange)="clearError('name')"
                   (blur)="nameTouched = true" required>
            <div class="invalid-feedback" *ngIf="nameTouched && nameError">{{ nameError }}</div>
          </div>

          <div class="form-group">
            <label>Email</label>
            <input type="email" class="form-control" [class.is-invalid]="emailTouched && emailError"
                   placeholder="you@example.com"
                   [(ngModel)]="model.email" name="email" email
                   (ngModelChange)="clearError('email')"
                   (blur)="emailTouched = true" required>
            <div class="invalid-feedback" *ngIf="emailTouched && emailError">{{ emailError }}</div>
          </div>

          <div class="form-group">
            <label>Password</label>
            <div class="password-wrap">
              <input [type]="showPassword ? 'text' : 'password'" class="form-control"
                     [class.is-invalid]="passwordTouched && passwordError"
                     placeholder="Min 6 characters"
                     [(ngModel)]="model.password" name="password"
                     (ngModelChange)="clearError('password')"
                     (blur)="passwordTouched = true" required>
              <button type="button" class="toggle-pw" (click)="showPassword = !showPassword">
                {{ showPassword ? '🙈' : '👁' }}
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="passwordTouched && passwordError">{{ passwordError }}</div>
            <div class="password-strength" *ngIf="model.password">
              <div class="strength-bar" [style.width]="getPasswordStrength() + '%'"
                   [style.background]="getPasswordColor()"></div>
            </div>
            <small class="text-muted" *ngIf="model.password">{{ getPasswordLabel() }}</small>
          </div>

          <div class="server-error" *ngIf="serverError">{{ serverError }}</div>

          <button type="submit" class="auth-submit" [disabled]="loading">
            <span *ngIf="!loading">Create Account</span>
            <span *ngIf="loading" class="spinner-border spinner-border-sm me-1"></span>
            <span *ngIf="loading">Creating account...</span>
          </button>
        </form>

        <div class="auth-footer">
          Already have an account? <a routerLink="/login">Sign in</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrapper { display: flex; justify-content: center; align-items: center; min-height: 80vh; padding: 2rem 1rem; }
    .auth-card {
      width: 100%; max-width: 420px; background: #fff; border-radius: 16px;
      box-shadow: 0 8px 32px rgba(0,0,0,0.08); padding: 2rem;
    }
    .auth-header { text-align: center; margin-bottom: 1.5rem; }
    .auth-header h2 { font-size: 1.5rem; font-weight: 700; margin-bottom: 0.25rem; }
    .auth-header p { color: #666; font-size: 0.9rem; }
    .google-btn-wrap { margin-bottom: 1rem; }
    .google-btn {
      width: 100%; padding: 10px; border: 1.5px solid #ddd; border-radius: 8px;
      background: #fff; cursor: pointer; font-size: 0.9rem; font-weight: 500;
      display: flex; align-items: center; justify-content: center; gap: 8px;
      transition: border-color 0.2s, background 0.2s;
    }
    .google-btn:hover:not(:disabled) { border-color: #bbb; background: #f8f9fa; }
    .google-btn:disabled { opacity: 0.6; cursor: default; }
    .divider { text-align: center; margin: 1rem 0; position: relative; color: #aaa; font-size: 0.85rem; }
    .divider::before, .divider::after {
      content: ''; position: absolute; top: 50%; width: calc(50% - 20px);
      height: 1px; background: #e0e0e0;
    }
    .divider::before { left: 0; }
    .divider::after { right: 0; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 4px; color: #333; }
    .form-control { width: 100%; padding: 10px 12px; border: 1.5px solid #ddd; border-radius: 8px; font-size: 0.9rem; transition: border-color 0.2s; }
    .form-control:focus { border-color: #0d6efd; outline: none; box-shadow: 0 0 0 3px rgba(13,110,253,0.1); }
    .form-control.is-invalid { border-color: #dc3545; }
    .invalid-feedback { color: #dc3545; font-size: 0.8rem; margin-top: 4px; display: block; }
    .password-wrap { position: relative; }
    .toggle-pw {
      position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
      background: none; border: none; cursor: pointer; font-size: 1rem; padding: 0;
    }
    .password-strength { height: 4px; background: #e9ecef; border-radius: 2px; margin-top: 6px; overflow: hidden; }
    .strength-bar { height: 100%; border-radius: 2px; transition: width 0.3s, background 0.3s; }
    .server-error { background: #fff3cd; border: 1px solid #ffc107; color: #664d03; padding: 10px; border-radius: 8px; margin-bottom: 1rem; font-size: 0.85rem; }
    .auth-submit {
      width: 100%; padding: 12px; background: #0d6efd; color: #fff; border: none;
      border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;
      transition: background 0.2s;
    }
    .auth-submit:hover:not(:disabled) { background: #0b5ed7; }
    .auth-submit:disabled { opacity: 0.7; cursor: default; }
    .auth-footer { text-align: center; margin-top: 1.5rem; font-size: 0.9rem; color: #666; }
    .auth-footer a { color: #0d6efd; text-decoration: none; font-weight: 600; }
    .auth-footer a:hover { text-decoration: underline; }
  `]
})
export class RegisterComponent {
  model = { fullName: '', email: '', password: '' };
  showPassword = false;
  loading = false;
  serverError = '';
  nameTouched = false;
  emailTouched = false;
  passwordTouched = false;
  nameError = '';
  emailError = '';
  passwordError = '';
  googleClientId: string;

  constructor(
    private auth: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.googleClientId = (window as any).__GOOGLE_CLIENT_ID__ || '';
  }

  clearError(field: string) {
    if (field === 'name') this.nameError = '';
    if (field === 'email') this.emailError = '';
    if (field === 'password') this.passwordError = '';
    this.serverError = '';
  }

  validate(): boolean {
    let valid = true;
    this.nameError = '';
    this.emailError = '';
    this.passwordError = '';

    if (!this.model.fullName.trim()) {
      this.nameError = 'Full name is required';
      valid = false;
    } else if (this.model.fullName.trim().length < 2) {
      this.nameError = 'Name must be at least 2 characters';
      valid = false;
    }

    if (!this.model.email.trim()) {
      this.emailError = 'Email is required';
      valid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.model.email)) {
      this.emailError = 'Please enter a valid email address';
      valid = false;
    }

    if (!this.model.password) {
      this.passwordError = 'Password is required';
      valid = false;
    } else if (this.model.password.length < 6) {
      this.passwordError = 'Password must be at least 6 characters';
      valid = false;
    }

    return valid;
  }

  getPasswordStrength(): number {
    const pw = this.model.password;
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 6) score += 25;
    if (pw.length >= 8) score += 15;
    if (/[A-Z]/.test(pw)) score += 20;
    if (/[0-9]/.test(pw)) score += 20;
    if (/[^A-Za-z0-9]/.test(pw)) score += 20;
    return Math.min(100, score);
  }

  getPasswordColor(): string {
    const s = this.getPasswordStrength();
    if (s < 30) return '#dc3545';
    if (s < 60) return '#ffc107';
    if (s < 80) return '#198754';
    return '#0d6efd';
  }

  getPasswordLabel(): string {
    const s = this.getPasswordStrength();
    if (s < 30) return 'Weak';
    if (s < 60) return 'Fair';
    if (s < 80) return 'Strong';
    return 'Very Strong';
  }

  onSubmit() {
    this.nameTouched = true;
    this.emailTouched = true;
    this.passwordTouched = true;
    if (!this.validate()) return;

    this.loading = true;
    this.auth.register(this.model).subscribe({
      next: (res) => {
        if (res?.isSuccess && this.auth.isLoggedIn()) {
          this.loading = false;
          this.toastr.success('Welcome to AIShopVerse!', 'Account Created');
          this.router.navigateByUrl('/');
        } else {
          this.serverError = res?.message || 'Registration failed. Please try again.';
          this.loading = false;
        }
      },
      error: (err) => {
        this.serverError = err?.error?.message || err?.message || 'Registration failed. Please try again.';
        this.loading = false;
      }
    });
  }

  registerWithGoogle() {
    this.toastr.info('Google sign-up is not configured yet.', 'Coming Soon');
  }
}
