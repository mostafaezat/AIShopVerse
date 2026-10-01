import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleFormatService } from '../../core/services/locale-format.service';

/**
 * Locale-aware counterpart to Angular's built-in `currency` pipe.
 *
 * Angular's pipe reads `LOCALE_ID`, which cannot change at runtime, so the
 * active app language is resolved through LocaleFormatService instead.
 *
 * `pure: false` is required: the output depends on the language signal, and a
 * pure pipe would otherwise keep the value it cached for the first language.
 */
@Pipe({ name: 'price', standalone: true, pure: false })
export class PricePipe implements PipeTransform {
  private format = inject(LocaleFormatService);

  transform(value: number | null | undefined, fractionDigits?: number): string {
    return this.format.price(value, fractionDigits);
  }
}

@Pipe({ name: 'localizedDate', standalone: true, pure: false })
export class LocalizedDatePipe implements PipeTransform {
  private format = inject(LocaleFormatService);

  transform(
    value: string | number | Date | null | undefined,
    style: 'short' | 'medium' | 'long' = 'medium'
  ): string {
    return this.format.date(value, style);
  }
}

@Pipe({ name: 'localizedNumber', standalone: true, pure: false })
export class LocalizedNumberPipe implements PipeTransform {
  private format = inject(LocaleFormatService);

  transform(value: number | null | undefined, fractionDigits?: number): string {
    return this.format.number(value, fractionDigits);
  }
}

@Pipe({ name: 'localizedPercent', standalone: true, pure: false })
export class LocalizedPercentPipe implements PipeTransform {
  private format = inject(LocaleFormatService);

  transform(value: number | null | undefined): string {
    return this.format.percent(value);
  }
}
