import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { GridModule } from '@progress/kendo-angular-grid';
import { AdminDashboard } from '../../../core/models/admin-dashboard.model';
import { AdminDashboardService } from '../../../core/services/admin-dashboard.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { HealthDotComponent } from '../../../shared/components/health-dot/health-dot.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatCardComponent,
    StatusBadgeComponent,
    HealthDotComponent,
    PageFeedbackComponent
  ],
  templateUrl: './dashboard.component.html',
  providers: [PageStateService]
})
export class AdminDashboardComponent {
  private readonly dashboardService = inject(AdminDashboardService);
  readonly pageState = inject(PageStateService);

  readonly dashboard = signal<AdminDashboard | null>(null);

  constructor() {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.pageState.startLoading();
    this.pageState.clearFeedback();

    this.dashboardService.getDashboard().subscribe({
      next: dashboard => {
        this.dashboard.set(dashboard);
        this.pageState.stopLoading();
      },
      error: err => {
        this.dashboard.set(null);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load dashboard data.');
      }
    });
  }
}
