import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-wrapper">
      <div class="auth-card">
        <div class="auth-header" *ngIf="!submitted">
          <div class="icon-circle">🔑</div>
          <h2>Forgot Password?</h2>
          <p>Enter your email and we'll send you a reset link.</p>
        </div>

        <div class="auth-header" *ngIf="submitted">
          <div class="icon-circle success">✓</div>
          <h2>Check Your Email</h2>
          <p *ngIf="devResetLink">Dev mode — reset link shown below:</p>
          <p *ngIf="!devResetLink">If an account exists with that email, a reset link has been sent.</p>
        </div>

        <form (ngSubmit)="onSubmit()" *ngIf="!submitted">
          <div class="form-group">
            <label>Email Address</label>
            <input type="email" class="form-control" [class.is-invalid]="emailTouched && emailError"
                   placeholder="you@example.com"
                   [(ngModel)]="email" name="email" email
                   (ngModelChange)="emailError = ''"
                   (blur)="emailTouched = true" required>
            <div class="invalid-feedback" *ngIf="emailTouched && emailError">{{ emailError }}</div>
          </div>

          <div class="server-error" *ngIf="serverError">{{ serverError }}</div>

          <button type="submit" class="auth-submit" [disabled]="loading">
            <span *ngIf="!loading">Send Reset Link</span>
            <span *ngIf="loading" class="spinner-border spinner-border-sm me-1"></span>
            <span *ngIf="loading">Sending...</span>
          </button>
        </form>

        <div *ngIf="submitted && devResetLink" class="dev-link-box">
          <p class="dev-label">Dev Reset Link:</p>
          <a [routerLink]="['/reset-password']" [queryParams]="{token: devToken, email: email}" class="dev-link">
            {{ devResetLink }}
          </a>
        </div>

        <div class="auth-footer">
          <a routerLink="/login">← Back to Sign In</a>
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
    .auth-header h2 { font-size: 1.5rem; font-weight: 700; margin: 0.5rem 0 0.25rem; }
    .auth-header p { color: #666; font-size: 0.9rem; margin: 0; }
    .icon-circle {
      width: 64px; height: 64px; border-radius: 50%; background: #f0f7ff;
      display: inline-flex; align-items: center; justify-content: center;
      font-size: 1.5rem; margin-bottom: 0.5rem;
    }
    .icon-circle.success { background: #d1e7dd; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 4px; color: #333; }
    .form-control { width: 100%; padding: 10px 12px; border: 1.5px solid #ddd; border-radius: 8px; font-size: 0.9rem; transition: border-color 0.2s; }
    .form-control:focus { border-color: #0d6efd; outline: none; box-shadow: 0 0 0 3px rgba(13,110,253,0.1); }
    .form-control.is-invalid { border-color: #dc3545; }
    .invalid-feedback { color: #dc3545; font-size: 0.8rem; margin-top: 4px; display: block; }
    .server-error { background: #fff3cd; border: 1px solid #ffc107; color: #664d03; padding: 10px; border-radius: 8px; margin-bottom: 1rem; font-size: 0.85rem; }
    .auth-submit {
      width: 100%; padding: 12px; background: #0d6efd; color: #fff; border: none;
      border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;
      transition: background 0.2s;
    }
    .auth-submit:hover:not(:disabled) { background: #0b5ed7; }
    .auth-submit:disabled { opacity: 0.7; cursor: default; }
    .dev-link-box {
      background: #f8f9fa; border: 1px dashed #6c757d; border-radius: 8px;
      padding: 12px; margin: 1rem 0; word-break: break-all;
    }
    .dev-label { font-size: 0.8rem; font-weight: 600; color: #6c757d; margin-bottom: 4px; }
    .dev-link { font-size: 0.8rem; color: #0d6efd; text-decoration: none; }
    .dev-link:hover { text-decoration: underline; }
    .auth-footer { text-align: center; margin-top: 1.5rem; font-size: 0.9rem; }
    .auth-footer a { color: #0d6efd; text-decoration: none; font-weight: 600; }
    .auth-footer a:hover { text-decoration: underline; }
  `]
})
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  submitted = false;
  emailTouched = false;
  emailError = '';
  serverError = '';
  devResetLink = '';
  devToken = '';

  constructor(
    private auth: AuthService,
    private toastr: ToastrService
  ) {}

  onSubmit() {
    this.emailTouched = true;
    if (!this.email.trim()) {
      this.emailError = 'Email is required';
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email)) {
      this.emailError = 'Please enter a valid email';
      return;
    }

    this.loading = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: (res) => {
        const data = res?.data;
        this.submitted = true;
        this.devResetLink = data?.devResetLink || '';
        this.devToken = data?.devResetToken || '';
        this.toastr.success('Reset link sent!', 'Success');
      },
      error: (err) => {
        this.serverError = err.error?.message || 'Failed to send reset link. Please try again.';
        this.loading = false;
      }
    });
  }
}
