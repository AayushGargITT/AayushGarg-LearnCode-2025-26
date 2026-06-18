import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { GridModule } from '@progress/kendo-angular-grid';
import { EmployeeAllocation } from '../../../core/models/employee-self.model';
import { EmployeeService } from '../../../core/services/employee.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-employee-allocations',
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
export class EmployeeAllocationsComponent {
  private readonly employeeService = inject(EmployeeService);
  readonly pageState = inject(PageStateService);

  readonly allocations = signal<EmployeeAllocation[]>([]);
  readonly totalCurrentUtilisation = signal(0);

  constructor() {
    this.loadAllocations();
  }

  loadAllocations(): void {
    this.pageState.startLoading();
    this.employeeService.getAllocations().subscribe({
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
