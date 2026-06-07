import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { GridModule } from '@progress/kendo-angular-grid';
import { Allocation } from '../../../core/models/allocation.model';
import { AdminAllocationService } from '../../../core/services/admin-allocation.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-allocations',
  standalone: true,
  imports: [
    CommonModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    PageFeedbackComponent
  ],
  templateUrl: './allocations.component.html',
  styleUrl: './allocations.component.css',
  providers: [PageStateService]
})
export class AdminAllocationsComponent {
  private readonly adminAllocationService = inject(AdminAllocationService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<Allocation[]>([]);

  constructor() {
    this.loadAllocations();
  }

  loadAllocations(): void {
    this.pageState.startLoading();

    this.adminAllocationService.getAllAllocations().subscribe({
      next: allocations => {
        this.rows.set(allocations);
        this.pageState.stopLoading();
      },
      error: err => {
        this.rows.set([]);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load allocations.');
      }
    });
  }

  statusFor(allocation: Allocation): string {
    return allocation.isActive ? 'Active' : 'Ended';
  }
}
