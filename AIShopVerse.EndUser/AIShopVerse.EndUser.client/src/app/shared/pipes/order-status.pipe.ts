import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService, Translation } from '@ngx-translate/core';
import { OrderStatus } from '../../core/models/order.model';

/**
 * Maps the numeric OrderStatus enum to a translated label.
 *
 * Falls back to the raw enum name (e.g. "Pending") if a translation is missing,
 * so a new backend status never renders blank.
 */
@Pipe({
  name: 'orderStatus',
  standalone: true,
  pure: false
})
export class OrderStatusPipe implements PipeTransform {
  private translate = inject(TranslateService);

  private static readonly KEYS: Record<number, string> = {
    [OrderStatus.Pending]: 'orders.statusPending',
    [OrderStatus.Paid]: 'orders.statusPaid',
    [OrderStatus.Processing]: 'orders.statusProcessing',
    [OrderStatus.Shipped]: 'orders.statusShipped',
    [OrderStatus.Delivered]: 'orders.statusDelivered',
    [OrderStatus.Cancelled]: 'orders.statusCancelled',
    [OrderStatus.Refunded]: 'orders.statusRefunded'
  };

  transform(status: OrderStatus | number | null | undefined): string {
    if (status === null || status === undefined) return '';

    const key = OrderStatusPipe.KEYS[Number(status)];
    if (!key) return String(status);

    const label = this.translate.instant(key);
    // instant() echoes the key back when the translation is missing.
    return label === key ? OrderStatus[Number(status)] ?? String(status) : (label as Translation as string);
  }
}
