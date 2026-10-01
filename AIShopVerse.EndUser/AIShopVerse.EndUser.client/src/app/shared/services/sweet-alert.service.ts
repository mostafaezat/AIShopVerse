import { Injectable, inject } from '@angular/core';
import Swal, { SweetAlertIcon, SweetAlertOptions, SweetAlertResult } from 'sweetalert2';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class SweetAlertService {
  private translate = inject(TranslateService);

  private getBaseOptions(): Partial<SweetAlertOptions> {
    // SweetAlert2 detects RTL from the computed `direction` of its container,
    // which we already drive from <html dir> in LanguageService.
    return {
      buttonsStyling: true,
      customClass: {
        confirmButton: 'btn btn-primary',
        cancelButton: 'btn btn-secondary',
        denyButton: 'btn btn-outline-danger'
      }
    };
  }

  private t(key: string, params?: object): string {
    return this.translate.instant(key, params);
  }

  private fire(options: SweetAlertOptions): Promise<SweetAlertResult> {
    return Swal.fire({
      ...this.getBaseOptions(),
      ...options
    } as SweetAlertOptions);
  }

  success(title: string, text?: string, options?: SweetAlertOptions): Promise<SweetAlertResult> {
    return this.fire({
      icon: 'success',
      title,
      text,
      timer: 3000,
      timerProgressBar: true,
      showConfirmButton: false,
      ...options
    });
  }

  error(title: string, text?: string, options?: SweetAlertOptions): Promise<SweetAlertResult> {
    return this.fire({
      icon: 'error',
      title,
      text,
      ...options
    });
  }

  warning(title: string, text?: string, options?: SweetAlertOptions): Promise<SweetAlertResult> {
    return this.fire({
      icon: 'warning',
      title,
      text,
      ...options
    });
  }

  info(title: string, text?: string, options?: SweetAlertOptions): Promise<SweetAlertResult> {
    return this.fire({
      icon: 'info',
      title,
      text,
      timer: 5000,
      timerProgressBar: true,
      showConfirmButton: false,
      ...options
    });
  }

  confirm(
    title: string,
    text: string,
    options?: SweetAlertOptions
  ): Promise<boolean> {
    return this.fire({
      icon: 'question',
      title,
      text,
      showCancelButton: true,
      confirmButtonText: this.t('common.yes'),
      cancelButtonText: this.t('common.no'),
      confirmButtonColor: '#dc3545',
      cancelButtonColor: '#6c757d',
      reverseButtons: true,
      ...options
    }).then(result => result.isConfirmed);
  }

  confirmDelete(itemName?: string): Promise<boolean> {
    const item = itemName || this.t('dialogs.thisItem');
    return this.confirm(
      this.t('dialogs.deleteTitle'),
      this.t('dialogs.deleteMessage', { item })
    );
  }

  toast(icon: SweetAlertIcon, title: string, options?: SweetAlertOptions): void {
    void this.fire({
      toast: true,
      // Anchor the toast at the "end" side, which mirrors with the document.
      position: 'top-end',
      icon,
      title,
      showConfirmButton: false,
      timer: 3000,
      timerProgressBar: true,
      ...options
    });
  }

  toastSuccess(title: string): void {
    this.toast('success', title);
  }

  toastError(title: string): void {
    this.toast('error', title);
  }

  toastWarning(title: string): void {
    this.toast('warning', title);
  }

  toastInfo(title: string): void {
    this.toast('info', title);
  }

  loading(title?: string, text?: string): void {
    this.fire({
      title: title || this.t('dialogs.pleaseWait'),
      text,
      allowOutsideClick: false,
      allowEscapeKey: false,
      showConfirmButton: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });
  }

  close(): void {
    Swal.close();
  }

  html(html: string, options?: SweetAlertOptions): Promise<SweetAlertResult> {
    return this.fire({
      html,
      showCancelButton: true,
      ...options
    });
  }
}