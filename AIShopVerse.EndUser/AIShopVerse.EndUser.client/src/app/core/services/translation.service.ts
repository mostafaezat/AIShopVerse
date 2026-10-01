import { Injectable, inject } from '@angular/core';
import {
  TranslateService,
  LangChangeEvent,
  TranslationChangeEvent,
  Translation
} from '@ngx-translate/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private translate = inject(TranslateService);

  readonly onLangChange: Observable<LangChangeEvent> = this.translate.onLangChange;
  readonly onTranslationChange: Observable<TranslationChangeEvent> =
    this.translate.onTranslationChange;

  /** Instant, non-reactive lookup - use in component logic and service code. */
  t(key: string | string[], params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  /** Reactive, change-aware lookup - use in templates. */
  tAsync(key: string | string[], params?: Record<string, unknown>): Observable<Translation> {
    return this.translate.get(key, params);
  }

  use(lang: string): Observable<unknown> {
    return this.translate.use(lang);
  }

  getCurrentLang(): string {
    return this.translate.getCurrentLang() ?? 'ar';
  }
}
