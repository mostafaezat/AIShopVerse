import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-wrapper">
      <div class="auth-card">
        <div class="auth-header" *ngIf="!submitted">
          <div class="icon-circle">🔒</div>
          <h2>Reset Password</h2>
          <p>Enter your new password below.</p>
        </div>

        <div class="auth-header" *ngIf="submitted">
          <div class="icon-circle success">✓</div>
          <h2>Password Reset!</h2>
          <p>Your password has been updated successfully.</p>
        </div>

        <form (ngSubmit)="onSubmit()" *ngIf="!submitted">
          <div class="form-group">
            <label>New Password</label>
            <div class="password-wrap">
              <input [type]="showPassword ? 'text' : 'password'" class="form-control"
                     [class.is-invalid]="passwordTouched && passwordError"
                     placeholder="Min 6 characters"
                     [(ngModel)]="newPassword" name="newPassword"
                     (ngModelChange)="passwordError = ''"
                     (blur)="passwordTouched = true" required>
              <button type="button" class="toggle-pw" (click)="showPassword = !showPassword">
                {{ showPassword ? '🙈' : '👁' }}
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="passwordTouched && passwordError">{{ passwordError }}</div>
          </div>

          <div class="form-group">
            <label>Confirm Password</label>
            <div class="password-wrap">
              <input [type]="showConfirmPassword ? 'text' : 'password'" class="form-control"
                     [class.is-invalid]="confirmTouched && confirmError"
                     placeholder="Re-enter password"
                     [(ngModel)]="confirmPassword" name="confirmPassword"
                     (ngModelChange)="confirmError = ''"
                     (blur)="confirmTouched = true" required>
              <button type="button" class="toggle-pw" (click)="showConfirmPassword = !showConfirmPassword">
                {{ showConfirmPassword ? '🙈' : '👁' }}
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="confirmTouched && confirmError">{{ confirmError }}</div>
          </div>

          <div class="server-error" *ngIf="serverError">{{ serverError }}</div>

          <button type="submit" class="auth-submit" [disabled]="loading">
            <span *ngIf="!loading">Reset Password</span>
            <span *ngIf="loading" class="spinner-border spinner-border-sm me-1"></span>
            <span *ngIf="loading">Resetting...</span>
          </button>
        </form>

        <div class="auth-footer" *ngIf="submitted">
          <a routerLink="/login">Go to Sign In</a>
        </div>

        <div class="auth-footer" *ngIf="!submitted">
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
    .password-wrap { position: relative; }
    .toggle-pw {
      position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
      background: none; border: none; cursor: pointer; font-size: 1rem; padding: 0;
    }
    .server-error { background: #fff3cd; border: 1px solid #ffc107; color: #664d03; padding: 10px; border-radius: 8px; margin-bottom: 1rem; font-size: 0.85rem; }
    .auth-submit {
      width: 100%; padding: 12px; background: #0d6efd; color: #fff; border: none;
      border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;
      transition: background 0.2s;
    }
    .auth-submit:hover:not(:disabled) { background: #0b5ed7; }
    .auth-submit:disabled { opacity: 0.7; cursor: default; }
    .auth-footer { text-align: center; margin-top: 1.5rem; font-size: 0.9rem; }
    .auth-footer a { color: #0d6efd; text-decoration: none; font-weight: 600; }
    .auth-footer a:hover { text-decoration: underline; }
  `]
})
export class ResetPasswordComponent implements OnInit {
  email = '';
  token = '';
  newPassword = '';
  confirmPassword = '';
  showPassword = false;
  showConfirmPassword = false;
  loading = false;
  submitted = false;
  passwordTouched = false;
  confirmTouched = false;
  passwordError = '';
  confirmError = '';
  serverError = '';

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.email || !this.token) {
      this.router.navigate(['/forgot-password']);
    }
  }

  validate(): boolean {
    let valid = true;
    this.passwordError = '';
    this.confirmError = '';

    if (!this.newPassword) {
      this.passwordError = 'Password is required';
      valid = false;
    } else if (this.newPassword.length < 6) {
      this.passwordError = 'Password must be at least 6 characters';
      valid = false;
    }

    if (!this.confirmPassword) {
      this.confirmError = 'Please confirm your password';
      valid = false;
    } else if (this.newPassword !== this.confirmPassword) {
      this.confirmError = 'Passwords do not match';
      valid = false;
    }

    return valid;
  }

  onSubmit() {
    this.passwordTouched = true;
    this.confirmTouched = true;
    if (!this.validate()) return;

    this.loading = true;
    this.auth.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.submitted = true;
        this.toastr.success('Password reset successfully!', 'Success');
      },
      error: (err) => {
        this.serverError = err.error?.message || 'Failed to reset password. The link may have expired.';
        this.loading = false;
      }
    });
  }
}
