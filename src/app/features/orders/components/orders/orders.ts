import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';

import { OrderService } from '../../services/order.service';
import { OrderData, OrderTicket } from '../../models/order.model';
import { TranslatePipe } from '../../../../i18n/translate.pipe';

@Component({
  imports: [TranslatePipe, CurrencyPipe, DatePipe],
  selector: 'app-orders',
  styleUrl: './orders.css',
  templateUrl: './orders.html',
})
export class Orders implements OnInit {
  private orderService = inject(OrderService);

  ordertickets = signal<OrderTicket[]>([]);

  readonly selectedOrderId = signal<number | null>(null);

  readonly selectedOrder = computed(() => {
    const orders = this.ordertickets();

    return orders.find((order) => order.id === this.selectedOrderId()) ?? orders[0] ?? null;
  });

  readonly orderCounts = computed(() => {
    const orders = this.ordertickets();

    const counts = {
      all: orders.length,
      received: 0,
      inKitchen: 0,
      served: 0,
      cancelled: 0,
    };

    for (const order of orders) {
      switch (order.orderstatus.trim().toLowerCase()) {
        case 'received':
          counts.received++;
          break;

        case 'in the kitchen':
          counts.inKitchen++;
          break;

        case 'served':
          counts.served++;
          break;

        case 'cancelled':
          counts.cancelled++;
          break;
      }
    }

    return counts;
  });

  ngOnInit(): void {
    this.orderService.getOrders().subscribe({
      next: (data: OrderData) => {
        this.ordertickets.set(data.ordertickets ?? []);
      },
      error: (err: any) => {
        alert(err.error?.message ?? err.message);
      },
    });
  }
}
