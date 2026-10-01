import { Component, Input, ContentChild, TemplateRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  private translate = inject(TranslateService);

  @Input() icon = 'bi-inbox';
  @Input() titleKey = 'common.none';
  @Input() messageKey = 'emptyState.noItems';
  @Input() title = '';
  @Input() message = '';
  @ContentChild('action') actionTemplate?: TemplateRef<unknown>;

  get resolvedTitle(): string {
    return this.title || this.translate.instant(this.titleKey);
  }

  get resolvedMessage(): string {
    return this.message || this.translate.instant(this.messageKey);
  }
}