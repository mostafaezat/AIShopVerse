import { Component, OnInit, inject } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [SharedModule, RouterModule, TranslatePipe],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private translate = inject(TranslateService);
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
    const t = this.translate;
    this.passwordError = '';
    this.confirmError = '';

    if (!this.newPassword) {
      this.passwordError = t.instant('auth.passwordRequired');
      valid = false;
    } else if (this.newPassword.length < 6) {
      this.passwordError = t.instant('auth.passwordMinLength');
      valid = false;
    }

    if (!this.confirmPassword) {
      this.confirmError = t.instant('auth.confirmRequired');
      valid = false;
    } else if (this.newPassword !== this.confirmPassword) {
      this.confirmError = t.instant('auth.passwordsMismatch');
      valid = false;
    }

    return valid;
  }

  onSubmit() {
    this.passwordTouched = true;
    this.confirmTouched = true;
    if (!this.validate()) return;

    const t = this.translate;
    this.loading = true;
    this.auth.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.submitted = true;
        this.toastr.success(t.instant('toast.passwordReset'), t.instant('common.success'));
      },
      error: (err) => {
        this.serverError = t.instant('auth.resetPasswordFailed');
        this.loading = false;
      }
    });
  }
}