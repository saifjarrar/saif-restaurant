import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from './language';

@Pipe({ name: 'translate', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(text: string | null | undefined): string {
    return text ? this.language.translate(text) : '';
  }
}
