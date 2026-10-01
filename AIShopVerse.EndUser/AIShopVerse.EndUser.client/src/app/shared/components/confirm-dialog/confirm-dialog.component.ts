import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  private translate = inject(TranslateService);

  @Input() titleKey = 'dialogs.confirmTitle';
  @Input() messageKey = 'dialogs.confirmMessage';
  @Input() title = '';
  @Input() message = '';
  @Input() icon = 'bi-question-circle';
  @Input() iconColor = 'text-warning';
  @Input() confirmTextKey = 'common.confirm';
  @Input() cancelTextKey = 'common.cancel';
  @Input() confirmText = '';
  @Input() cancelText = '';
  @Input() confirmVariant = 'danger';
  @Input() confirmIcon = 'bi-check-lg';

  @Output() onConfirm = new EventEmitter<void>();
  @Output() onCancel = new EventEmitter<void>();

  get resolvedTitle(): string {
    return this.title || this.translate.instant(this.titleKey);
  }

  get resolvedMessage(): string {
    return this.message || this.translate.instant(this.messageKey);
  }

  get resolvedConfirmText(): string {
    return this.confirmText || this.translate.instant(this.confirmTextKey);
  }

  get resolvedCancelText(): string {
    return this.cancelText || this.translate.instant(this.cancelTextKey);
  }
}