import { Component, inject, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService, SupportedLanguage } from '../../../core/services/language.service';

interface LanguageOption {
  code: SupportedLanguage;
  label: string;
  ariaKey: string;
}

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div class="language-switcher" [class.open]="isOpen" #wrapper>
      <button
        class="lang-btn"
        (click)="toggleDropdown($event)"
        [attr.aria-expanded]="isOpen"
        [attr.aria-label]="switchAriaLabel | translate"
        aria-haspopup="listbox"
        type="button">
        <svg class="lang-globe" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <circle cx="12" cy="12" r="10"/><path d="M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
        </svg>
        <span class="lang-current">{{ languageService.getCurrentLanguageLabel() }}</span>
        <svg class="lang-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path d="M6 9l6 6 6-6"/>
        </svg>
      </button>

      <div class="lang-dropdown" *ngIf="isOpen" role="listbox" [attr.aria-label]="'language.selectLanguage' | translate">
        <button
          class="lang-option"
          *ngFor="let lang of languages; let i = index"
          (click)="selectLanguage(lang.code)"
          [class.active]="languageService.currentLanguage() === lang.code"
          role="option"
          [attr.aria-selected]="languageService.currentLanguage() === lang.code"
          [attr.tabindex]="i === 0 ? 0 : -1"
          type="button">
          <span class="lang-name">{{ lang.label }}</span>
          <svg *ngIf="languageService.currentLanguage() === lang.code" class="lang-check" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" aria-hidden="true">
            <path d="M20 6L9 17l-5-5"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .language-switcher {
      position: relative;
      display: inline-block;
    }

    .lang-btn {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 12px;
      background: rgba(255, 255, 255, 0.1);
      border: 1px solid rgba(255, 255, 255, 0.2);
      border-radius: 8px;
      color: white;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s ease, border-color 0.2s ease;
      white-space: nowrap;
    }

    .lang-btn:hover {
      background: rgba(255, 255, 255, 0.2);
      border-color: rgba(255, 255, 255, 0.4);
    }

    .lang-btn:focus-visible {
      outline: 2px solid #0d6efd;
      outline-offset: 2px;
    }

    .lang-globe, .lang-arrow {
      flex-shrink: 0;
    }

    .lang-arrow {
      transition: transform 0.2s ease;
    }

    .language-switcher.open .lang-arrow {
      transform: rotate(180deg);
    }

    .lang-dropdown {
      position: absolute;
      inset-block-start: calc(100% + 8px);
      inset-inline-end: 0;
      min-width: 160px;
      background: #fff;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      overflow: hidden;
      animation: langFadeIn 0.15s ease;
    }

    @keyframes langFadeIn {
      from { opacity: 0; transform: translateY(-4px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .lang-option {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      width: 100%;
      padding: 12px 16px;
      background: none;
      border: none;
      border-bottom: 1px solid #f0f0f0;
      color: #333;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      text-align: start;
      transition: background 0.15s ease;
    }

    .lang-option:last-child {
      border-bottom: none;
    }

    .lang-option:hover,
    .lang-option:focus-visible {
      background: #f8f9fa;
    }

    .lang-option:focus-visible {
      outline: 2px solid #0d6efd;
      outline-offset: -2px;
    }

    .lang-option.active {
      background: #e7f1ff;
      color: #0d6efd;
    }

    .lang-check {
      color: #0d6efd;
      flex-shrink: 0;
    }

    @media (max-width: 575.98px) {
      .lang-current { display: none; }
      .lang-btn { padding: 8px 10px; }
    }
  `]
})
export class LanguageSwitcherComponent {
  languageService = inject(LanguageService);
  isOpen = false;

  @ViewChild('wrapper') wrapperRef?: ElementRef<HTMLElement>;

  readonly languages: LanguageOption[] = [
    { code: 'ar', label: 'العربية', ariaKey: 'language.switchToArabic' },
    { code: 'en', label: 'English', ariaKey: 'language.switchToEnglish' }
  ];

  get switchAriaLabel(): string {
    return this.languageService.currentLanguage() === 'ar'
      ? 'language.switchToEnglish'
      : 'language.switchToArabic';
  }

  toggleDropdown(event: Event): void {
    event.stopPropagation();
    this.isOpen = !this.isOpen;
  }

  selectLanguage(code: SupportedLanguage): void {
    this.languageService.setLanguage(code);
    this.close();
    // Return focus to the trigger so keyboard users keep their place.
    const btn = this.wrapperRef?.nativeElement.querySelector<HTMLButtonElement>('.lang-btn');
    btn?.focus();
  }

  close(): void {
    this.isOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.language-switcher')) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.isOpen) return;
    this.close();
    this.wrapperRef?.nativeElement.querySelector<HTMLButtonElement>('.lang-btn')?.focus();
  }
}
