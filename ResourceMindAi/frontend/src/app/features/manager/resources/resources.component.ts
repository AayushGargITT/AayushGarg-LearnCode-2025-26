import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { ManagerResource, ManagerResourceDashboard } from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-manager-resources',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    DialogModule,
    AppLayoutComponent,
    PageHeaderComponent,
    AvatarComponent,
    StatusBadgeComponent,
    PageFeedbackComponent
  ],
  templateUrl: './resources.component.html',
  styleUrl: './resources.component.css',
  providers: [PageStateService]
})
export class ManagerResourcesComponent {
  private readonly managerService = inject(ManagerService);
  readonly pageState = inject(PageStateService);

  readonly dashboard = signal<ManagerResourceDashboard>({ onBench: [], activeResources: [] });
  readonly selectedResource = signal<ManagerResource | null>(null);

  constructor() {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.pageState.startLoading();

    this.managerService.getResourceDashboard().subscribe({
      next: dashboard => {
        this.dashboard.set(dashboard);
        this.pageState.stopLoading();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load resource dashboard.');
      }
    });
  }

  openDetails(resource: ManagerResource): void {
    this.selectedResource.set(resource);
  }

  closeDetails(): void {
    this.selectedResource.set(null);
  }
}
