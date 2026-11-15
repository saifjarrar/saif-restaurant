import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { arabic } from './arabic';

type SiteLanguage = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly document = inject(DOCUMENT);
  private readonly selected = signal<SiteLanguage>('en');
  readonly language = this.selected.asReadonly();

  constructor() {
    let language: SiteLanguage = 'en';
    try {
      if (this.document.defaultView?.localStorage.getItem('site-language') === 'ar') language = 'ar';
    } catch {
      // Browsers can disable storage. The switch still works for this visit.
    }
    this.apply(language);
  }

  toggle(): void {
    const next = this.language() === 'ar' ? 'en' : 'ar';
    this.apply(next);
    try {
      this.document.defaultView?.localStorage.setItem('site-language', next);
    } catch {
      // The current selection remains available in memory.
    }
  }

  translate(text: string): string {
    return this.language() === 'ar' ? arabic.get(text.toLowerCase().trim()) ?? text : text;
  }

  private apply(language: SiteLanguage): void {
    this.selected.set(language);
    this.document.documentElement.lang = language;
    this.document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
  }
}
