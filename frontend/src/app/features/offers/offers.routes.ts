import { Routes } from '@angular/router';

export const offersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/offers/offers').then((m) => m.Offers),
  },
];
