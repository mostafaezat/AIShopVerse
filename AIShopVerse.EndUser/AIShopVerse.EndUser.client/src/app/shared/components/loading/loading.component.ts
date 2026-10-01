import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.scss'
})
export class LoadingComponent {
  private translate = inject(TranslateService);

  @Input() show = false;
  @Input() messageKey = 'common.loading';
  @Input() message = '';
  @Input() fullscreen = true;

  get resolvedMessage(): string {
    return this.message || this.translate.instant(this.messageKey);
  }
}