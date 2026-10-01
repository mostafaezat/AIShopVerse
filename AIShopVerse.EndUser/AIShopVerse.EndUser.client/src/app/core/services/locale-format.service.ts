import { Injectable, InjectionToken, inject } from '@angular/core';
import { LanguageService } from './language.service';

/**
 * Currency used for every price in the store.
 *
 * Angular's built-in `currency` pipe takes its currency from `LOCALE_ID`
 * (en-US -> USD), which is implicit. This token makes it explicit so the store
 * can move to another currency without touching templates.
 */
export const STORE_CURRENCY = new InjectionToken<string>('STORE_CURRENCY', {
  providedIn: 'root',
  factory: () => 'USD'
});

/** BCP-47 locale per supported app language. */
const LOCALES: Record<string, string> = {
  en: 'en-US',
  ar: 'ar-EG'
};

/**
 * Formats dates, numbers, percentages and prices using the active language.
 *
 * Arabic locales default to Arabic-Indic digits (٠١٢٣), which is unusual for
 * prices in Gulf/Egyptian retail, so `latn` is requested explicitly. Flip
 * `USE_ARABIC_INDIC_DIGITS` to true if the target market expects them.
 */
const USE_ARABIC_INDIC_DIGITS = false;

@Injectable({ providedIn: 'root' })
export class LocaleFormatService {
  private languageService = inject(LanguageService);
  private storeCurrency = inject(STORE_CURRENCY);

  get locale(): string {
    return LOCALES[this.languageService.currentLanguage()] ?? 'en-US';
  }

  private get numberingSystem(): 'latn' | undefined {
    if (this.languageService.currentLanguage() !== 'ar') return undefined;
    return USE_ARABIC_INDIC_DIGITS ? undefined : 'latn';
  }

  date(value: string | number | Date | null | undefined, style: 'short' | 'medium' | 'long' = 'medium'): string {
    if (value === null || value === undefined || value === '') return '';

    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const options: Intl.DateTimeFormatOptions =
      style === 'short'
        ? { day: 'numeric', month: 'short', numberingSystem: this.numberingSystem }
        : style === 'long'
          ? { dateStyle: 'long', numberingSystem: this.numberingSystem }
          : { dateStyle: 'medium', numberingSystem: this.numberingSystem };

    return new Intl.DateTimeFormat(this.locale, options).format(date);
  }

  price(value: number | null | undefined, fractionDigits?: number): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '';

    return new Intl.NumberFormat(this.locale, {
      style: 'currency',
      currency: this.storeCurrency,
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
      numberingSystem: this.numberingSystem
    }).format(value);
  }

  number(value: number | null | undefined, fractionDigits?: number): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '';

    return new Intl.NumberFormat(this.locale, {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
      numberingSystem: this.numberingSystem
    }).format(value);
  }

  percent(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '';

    return new Intl.NumberFormat(this.locale, {
      style: 'percent',
      maximumFractionDigits: 0,
      numberingSystem: this.numberingSystem
    }).format(value);
  }
}
