import { Component, inject } from '@angular/core';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [SharedModule, RouterModule, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private translate = inject(TranslateService);
  model = { email: '', password: '' };
  showPassword = false;
  loading = false;
  serverError = '';
  emailTouched = false;
  passwordTouched = false;
  emailError = '';
  passwordError = '';
  googleClientId: string;
  private returnUrl: string | null = null;

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {
    this.googleClientId = (window as any).__GOOGLE_CLIENT_ID__ || '';
    const params = this.route.snapshot.queryParamMap;
    if (params.get('sessionExpired') === '1') {
      this.serverError = this.translate.instant('auth.sessionExpired');
    }
    const returnUrl = params.get('returnUrl');
    this.returnUrl = returnUrl && returnUrl.startsWith('/') ? returnUrl : null;
  }

  clearError(field: string) {
    if (field === 'email') this.emailError = '';
    if (field === 'password') this.passwordError = '';
    this.serverError = '';
  }

  validate(): boolean {
    let valid = true;
    const t = this.translate;
    this.emailError = '';
    this.passwordError = '';

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
    }

    return valid;
  }

  onSubmit() {
    this.emailTouched = true;
    this.passwordTouched = true;
    if (!this.validate()) return;

    this.loading = true;
    this.auth.login(this.model).subscribe({
      next: (res) => {
        const t = this.translate;
        if (res?.isSuccess && this.auth.isLoggedIn()) {
          this.loading = false;
          this.toastr.success(t.instant('toast.loginSuccess'), t.instant('common.success'));
          this.router.navigateByUrl(this.returnUrl || '/');
        } else {
          this.serverError = t.instant('auth.invalidCredentials');
          this.loading = false;
        }
      },
      error: (err) => {
        const t = this.translate;
        this.serverError = t.instant('auth.loginFailed');
        this.loading = false;
      }
    });
  }

  loginWithGoogle() {
    const t = this.translate;
    this.toastr.info(t.instant('auth.googleNotConfigured'), t.instant('auth.comingSoon'));
  }
}