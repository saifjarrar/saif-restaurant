import { Routes } from '@angular/router';
import { authGuard } from './guards/auth-guards-guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'landing',
    pathMatch: 'full',
  },
  {
    path: 'landing',
    loadComponent: () => import('./pages/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'signup',
    loadComponent: () => import('./pages/signup/signup').then((m) => m.Signup),
  },
  {
    path: 'layout',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/layout/layout').then((m) => m.Layout),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'reservation',
        loadComponent: () => import('./pages/reservation/reservation').then((m) => m.Reservation),
      },
      {
        path: 'menu',
        loadComponent: () => import('./pages/menu/menu').then((m) => m.Menu),
      },
      {
        path: 'offers',
        loadChildren: () => import('./features/offers/offers.routes').then((m) => m.offersRoutes),
      },
      {
        path: 'contact',
        loadComponent: () => import('./pages/contact/contact').then((m) => m.Contact),
      },
      {
        path: 'orders',
        loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ordersRoutes),
      },
    ],
  },
];
