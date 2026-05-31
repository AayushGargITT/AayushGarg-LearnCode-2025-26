import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { NotFoundComponent } from './shared/components/not-found/not-found.component';

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: '', loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes) },
    {
        path: 'admin',
        canActivate: [authGuard],
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
    },
    {
        path: 'manager',
        canActivate: [authGuard],
        loadChildren: () => import('./features/manager/manager.routes').then(m => m.managerRoutes)
    },
    {
        path: 'employee',
        canActivate: [authGuard],
        loadChildren: () => import('./features/employee/employee.routes').then(m => m.employeeRoutes)
    },
    { path: '**', component: NotFoundComponent }
];
