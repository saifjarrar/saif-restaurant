import { Component, inject, OnInit, signal } from '@angular/core';
import {
  DashboardService,
  MostOrdered,
  OrderTicket,
  DashboardData,
  Reservation,
  RestaurantInfo,
} from '../../services/dashboard-service/dashboard-service';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  imports: [DatePipe],
})
export class Dashboard implements OnInit {
  date = new Date();
  router = inject(Router);
  dashboardService = inject(DashboardService);

  restaurantinfo = signal<RestaurantInfo[]>([]);
  ordertickets = signal<OrderTicket[]>([]);
  tonightreservations = signal<Reservation[]>([]);
  mostordered = signal<MostOrdered[]>([]);

  ngOnInit(): void {
    this.dashboardService.getDashboard().subscribe({
      next: (data: DashboardData) => {
        this.restaurantinfo.set(data.restaurantinfo ?? []);
        this.ordertickets.set(data.ordertickets ?? []);
        this.tonightreservations.set(data.tonightreservations ?? []);
        this.mostordered.set(data.mostordered ?? []);
      },
      error: (err: any) => {
        alert(err.error?.message ?? err.message);
      },
    });
  }

  readonly savingOrderIds = signal<number[]>([]);

  markAs(id: number): void {
    this.saveOrder(id, this.dashboardService.advanceOrder(id));
  }

  cancelOrder(id: number): void {
    this.saveOrder(id, this.dashboardService.cancelOrder(id));
  }

  private saveOrder(id: number, request: Observable<OrderTicket>): void {
    if (this.savingOrderIds().includes(id)) return;

    this.savingOrderIds.update((ids) => [...ids, id]);

    request
      .pipe(
        finalize(() => {
          this.savingOrderIds.update((ids) => ids.filter((orderId) => orderId !== id));
        }),
      )
      .subscribe({
        next: (updated) => {
          this.ordertickets.update((orders) =>
            orders.map((order) => (order.id === updated.id ? updated : order)),
          );
        },
        error: (err) => alert(err.error?.message ?? 'Could not update the order.'),
      });
  }

  barWidth(quantity: number): number {
    const max = Math.max(...this.mostordered().map((d) => d.quantity), 1);

    return (quantity / max) * 100;
  }
  onNewOrder() {
    this.router.navigateByUrl('/layout/neworder');
    this.dashboardService.getDashboard();
  }
}
