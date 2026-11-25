import { Routes } from '@angular/router';
export const routes: Routes = [
  { path: '', loadComponent: () => import('./workspace').then((m) => m.Workspace) },
  {
    path: 'connected',
    loadComponent: () => import('./connected-workspace').then((m) => m.ConnectedWorkspace),
  },
  {
    path: 'hot-seat',
    loadComponent: () => import('./hot-seat/hot-seat').then((m) => m.HotSeat),
  },
  { path: '**', redirectTo: '' },
];
