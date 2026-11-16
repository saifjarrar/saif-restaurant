import { Routes } from '@angular/router';

export const ordersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/orders/orders').then((m) => m.Orders),
  },
  {
    path: 'new-order',
    loadComponent: () => import('./components/new-order/new-order').then((m) => m.NewOrder),
  },
];
