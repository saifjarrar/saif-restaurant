import { TranslatePipe } from '../../i18n/translate.pipe';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import {
  DashboardService,
  OrderData,
  OrderTicket,
} from '../../services/dashboard-service/dashboard-service';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  imports: [TranslatePipe, CurrencyPipe, DatePipe],
  selector: 'app-orders',
  styleUrl: './orders.css',
  templateUrl: './orders.html',
})
export class Orders implements OnInit {
  dashboardService = inject(DashboardService);
  ordertickets = signal<OrderTicket[]>([]);
  readonly selectedOrderId = signal<number | null>(null);

  readonly selectedOrder = computed(() => {
    const orders = this.ordertickets();

    return orders.find((order) => order.id === this.selectedOrderId()) ?? orders[0] ?? null;
  });
  ngOnInit(): void {
    this.dashboardService.getorder().subscribe({
      next: (data: OrderData) => {
        this.ordertickets.set(data.ordertickets ?? []);
      },
      error: (err: any) => {
        alert(err.error?.message ?? err.message);
      },
    });
  }
  getdashboard() {
    this.dashboardService.getorder();
  }
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
}
