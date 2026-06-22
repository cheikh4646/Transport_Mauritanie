import { Routes } from '@angular/router';
import { TravelerComponent } from './traveler/traveler.component';
import { AdminPanelComponent } from './admin-panel.component';

export const routes: Routes = [
  { path: '', component: TravelerComponent },
  { path: 'admin', component: AdminPanelComponent },
  { path: '**', redirectTo: '' }
];
