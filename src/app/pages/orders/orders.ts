import { Component, inject, OnInit, signal } from '@angular/core';
import {
  DashboardService,
  OrderData,
  OrderTicket,
} from '../../services/dashboard-service/dashboard-service';

@Component({
  imports: [],
  selector: 'app-orders',
  styleUrl: './orders.css',
  templateUrl: './orders.html',
})
export class Orders implements OnInit {
  dashboardService = inject(DashboardService);
  ordertickets = signal<OrderTicket[]>([]);
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
}
