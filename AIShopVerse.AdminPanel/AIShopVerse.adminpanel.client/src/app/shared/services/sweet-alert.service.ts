import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon, SweetAlertOptions, SweetAlertResult } from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class SweetAlertService {
  private getBaseOptions(): Partial<SweetAlertOptions> {
    return {
      buttonsStyling: true,
      customClass: {
        confirmButton: 'btn btn-primary',
        cancelButton: 'btn btn-secondary',
        denyButton: 'btn btn-outline-danger'
      }
    };
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
      confirmButtonText: 'Yes',
      cancelButtonText: 'No',
      confirmButtonColor: '#dc3545',
      cancelButtonColor: '#6c757d',
      reverseButtons: true,
      ...options
    }).then(result => result.isConfirmed);
  }

  confirmDelete(itemName: string = 'this item'): Promise<boolean> {
    return this.confirm(
      'Delete Confirmation',
      `Are you sure you want to delete ${itemName}? This action cannot be undone.`
    );
  }

  toast(icon: SweetAlertIcon, title: string, options?: SweetAlertOptions): void {
    this.fire({
      toast: true,
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

  loading(title: string = 'Please wait...', text?: string): void {
    this.fire({
      title,
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