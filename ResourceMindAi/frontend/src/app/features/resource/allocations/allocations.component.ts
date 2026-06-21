import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { GridModule } from '@progress/kendo-angular-grid';
import { ResourceAllocation } from '../../../core/models/resource-self.model';
import { ResourceService } from '../../../core/services/resource.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-resource-allocations',
  standalone: true,
  imports: [
    CommonModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    PageFeedbackComponent,
    StatusBadgeComponent
  ],
  templateUrl: './allocations.component.html',
  providers: [PageStateService]
})
export class ResourceAllocationsComponent {
  private readonly resourceService = inject(ResourceService);
  readonly pageState = inject(PageStateService);

  readonly allocations = signal<ResourceAllocation[]>([]);
  readonly totalCurrentUtilisation = signal(0);

  constructor() {
    this.loadAllocations();
  }

  loadAllocations(): void {
    this.pageState.startLoading();
    this.resourceService.getAllocations().subscribe({
      next: response => {
        this.allocations.set(response.allocations);
        this.totalCurrentUtilisation.set(response.totalCurrentUtilisationPercent);
        this.pageState.stopLoading();
      },
      error: err => {
        this.allocations.set([]);
        this.totalCurrentUtilisation.set(0);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load your allocations.');
      }
    });
  }
}
