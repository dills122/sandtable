import { Routes } from '@angular/router';
export const routes: Routes = [
  { path: '', loadComponent: () => import('./workspace').then((m) => m.Workspace) },
  {
    path: 'connected',
    loadComponent: () => import('./connected-workspace').then((m) => m.ConnectedWorkspace),
  },
  { path: '**', redirectTo: '' },
];
