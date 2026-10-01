import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [SharedModule, RouterModule, TranslatePipe],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private translate = inject(TranslateService);
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
    const t = this.translate;
    this.nameError = '';
    this.emailError = '';
    this.passwordError = '';

    if (!this.model.fullName.trim()) {
      this.nameError = t.instant('auth.nameRequired');
      valid = false;
    } else if (this.model.fullName.trim().length < 2) {
      this.nameError = t.instant('auth.nameMinLength');
      valid = false;
    }

    if (!this.model.email.trim()) {
      this.emailError = t.instant('auth.emailRequired');
      valid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.model.email)) {
      this.emailError = t.instant('auth.emailInvalid');
      valid = false;
    }

    if (!this.model.password) {
      this.passwordError = t.instant('auth.passwordRequired');
      valid = false;
    } else if (this.model.password.length < 6) {
      this.passwordError = t.instant('auth.passwordMinLength');
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
    const t = this.translate;
    if (s < 30) return t.instant('auth.passwordWeak');
    if (s < 60) return t.instant('auth.passwordFair');
    if (s < 80) return t.instant('auth.passwordStrong');
    return t.instant('auth.passwordVeryStrong');
  }

  onSubmit() {
    this.nameTouched = true;
    this.emailTouched = true;
    this.passwordTouched = true;
    if (!this.validate()) return;

    const t = this.translate;
    this.loading = true;
    this.auth.register(this.model).subscribe({
      next: (res) => {
        if (res?.isSuccess && this.auth.isLoggedIn()) {
          this.loading = false;
          this.toastr.success(t.instant('toast.registerSuccess'), t.instant('common.success'));
          this.router.navigateByUrl('/');
        } else {
          this.serverError = t.instant('auth.registerFailed');
          this.loading = false;
        }
      },
      error: (err) => {
        this.serverError = t.instant('auth.registerFailed');
        this.loading = false;
      }
    });
  }

  registerWithGoogle() {
    const t = this.translate;
    this.toastr.info(t.instant('auth.googleNotConfigured'), t.instant('auth.comingSoon'));
  }
}