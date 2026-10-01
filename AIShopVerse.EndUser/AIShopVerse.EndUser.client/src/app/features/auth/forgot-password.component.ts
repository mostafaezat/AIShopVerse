import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [SharedModule, RouterModule, TranslatePipe],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private translate = inject(TranslateService);
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
    const t = this.translate;
    if (!this.email.trim()) {
      this.emailError = t.instant('auth.emailRequired');
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email)) {
      this.emailError = t.instant('auth.emailInvalid');
      return;
    }

    this.loading = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: (res) => {
        const data = res?.data;
        this.submitted = true;
        this.devResetLink = data?.devResetLink || '';
        this.devToken = data?.devResetToken || '';
        this.toastr.success(t.instant('toast.resetLinkSent'), t.instant('common.success'));
      },
      error: (err) => {
        this.serverError = t.instant('auth.resetLinkFailed');
        this.loading = false;
      }
    });
  }
}