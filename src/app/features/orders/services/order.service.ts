import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_URL } from '../../../config/api-url';

import { CreateOrderRequest, OrderData, OrderTicket } from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class OrderService {
  private http = inject(HttpClient);

  private baseUrl = `${API_URL}/Dashboard`;

  // Get all orders
  getOrders(): Observable<OrderData> {
    return this.http.get<OrderData>(this.baseUrl);
  }

  // Create a new order
  createOrder(order: CreateOrderRequest): Observable<OrderTicket> {
    return this.http.post<OrderTicket>(`${this.baseUrl}/orders`, order);
  }

  // Advance order status
  advanceOrder(id: number): Observable<OrderTicket> {
    return this.http.put<OrderTicket>(`${this.baseUrl}/orders/${id}/advance`, {});
  }

  // Cancel order
  cancelOrder(id: number): Observable<OrderTicket> {
    return this.http.put<OrderTicket>(`${this.baseUrl}/orders/${id}/cancel`, {});
  }
}
