import { Injectable, signal, computed, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

export type SupportedLanguage = 'ar' | 'en';

const STORAGE_KEY = 'preferredLanguage';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);
  private document = inject(DOCUMENT);

  readonly currentLanguage = signal<SupportedLanguage>('ar');
  readonly isRTL = computed(() => this.currentLanguage() === 'ar');

  constructor() {
    this.setupTranslateEvents();
    this.initLanguage();
  }

  /**
   * Arabic is the default for every first-time visitor. Only an explicitly
   * persisted selection overrides it.
   */
  private initLanguage(): void {
    const saved = this.readPersistedLanguage();
    this.applyLanguage(saved ?? 'ar', false);
  }

  private readPersistedLanguage(): SupportedLanguage | null {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return this.isSupported(saved) ? saved : null;
    } catch {
      return null;
    }
  }

  private isSupported(lang: string | null | undefined): lang is SupportedLanguage {
    return lang === 'ar' || lang === 'en';
  }

  private setupTranslateEvents(): void {
    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      if (this.isSupported(event.lang)) {
        this.currentLanguage.set(event.lang);
        this.updateDocumentDirection(event.lang);
      }
    });
  }

  private updateDocumentDirection(lang: SupportedLanguage): void {
    const html = this.document.documentElement;
    html.lang = lang;
    html.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }

  setLanguage(lang: SupportedLanguage, persist = true): void {
    if (!this.isSupported(lang)) return;
    this.applyLanguage(lang, persist);
  }

  private applyLanguage(lang: SupportedLanguage, persist: boolean): void {
    this.translate.use(lang);
    this.currentLanguage.set(lang);
    this.updateDocumentDirection(lang);

    if (persist) {
      try {
        localStorage.setItem(STORAGE_KEY, lang);
      } catch {
        // storage unavailable (private mode) - language still switches for this session
      }
    }
  }

  toggleLanguage(): void {
    this.setLanguage(this.currentLanguage() === 'ar' ? 'en' : 'ar');
  }

  isArabic(): boolean {
    return this.currentLanguage() === 'ar';
  }

  getLanguageLabel(lang: SupportedLanguage): string {
    return lang === 'ar' ? 'العربية' : 'English';
  }

  getCurrentLanguageLabel(): string {
    return this.getLanguageLabel(this.currentLanguage());
  }
}
