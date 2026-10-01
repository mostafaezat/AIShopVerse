import { Directive, HostBinding, Input, inject } from '@angular/core';
import { LanguageService } from '../../core/services/language.service';

/**
 * Sets the writing direction of the host element.
 *
 * By default it follows the active app language. Pass `false` to force LTR on a
 * sub-region inside an RTL page (phone numbers, card numbers, emails, codes).
 */
@Directive({
  selector: '[appRtl]',
  standalone: true
})
export class RtlDirective {
  private languageService = inject(LanguageService);

  @Input('appRtl') forceRtl: boolean | '' | undefined;

  @HostBinding('dir')
  get dir(): 'rtl' | 'ltr' {
    if (this.forceRtl === '') return 'rtl';
    if (typeof this.forceRtl === 'boolean') return this.forceRtl ? 'rtl' : 'ltr';
    return this.languageService.isRTL() ? 'rtl' : 'ltr';
  }
}
