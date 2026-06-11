import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  CreateManagerAllocationRequest,
  ManagerAllocation,
  ManagerProject,
  ManagerResource,
  ResourceMatch,
  ResourceMatchResponse
} from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-manager-allocate',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DateInputsModule,
    DropDownsModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    AvatarComponent,
    PageFeedbackComponent
  ],
  templateUrl: './allocate.component.html',
  styleUrl: './allocate.component.css',
  providers: [PageStateService]
})
export class ManagerAllocateComponent {
  private readonly managerService = inject(ManagerService);
  private readonly formBuilder = new FormBuilder();
  readonly pageState = inject(PageStateService);

  readonly projects = signal<ManagerProject[]>([]);
  readonly employees = signal<ManagerResource[]>([]);
  readonly matches = signal<ResourceMatchResponse | null>(null);
  readonly isFinding = signal(false);
  readonly isSubmitting = signal(false);

  readonly activeAllocations = computed<ManagerAllocation[]>(() =>
    this.employees().flatMap(employee => employee.activeAllocations));

  readonly aiForm = this.formBuilder.nonNullable.group({
    projectId: ['', Validators.required],
    requirement: ['', [Validators.required, Validators.minLength(5)]]
  });

  readonly allocationForm = this.formBuilder.nonNullable.group({
    projectId: ['', Validators.required],
    employeeId: ['', Validators.required],
    utilisationPercent: [50, [Validators.required, Validators.min(1), Validators.max(100)]],
    fromDate: [new Date(), Validators.required],
    toDate: [new Date(), Validators.required]
  });

  constructor() {
    this.loadData();
  }

  loadData(): void {
    this.pageState.startLoading();

    this.managerService.getProjects().subscribe({
      next: projects => {
        this.projects.set(projects);
        this.loadResources();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load manager projects.');
      }
    });
  }

  findMatches(): void {
    this.aiForm.markAllAsTouched();
    if (this.aiForm.invalid || this.isFinding()) {
      return;
    }

    this.pageState.clearFeedback();
    this.isFinding.set(true);
    this.matches.set(null);

    this.managerService.findResources(this.aiForm.getRawValue()).subscribe({
      next: response => {
        this.matches.set(response);
        this.isFinding.set(false);
      },
      error: err => {
        this.isFinding.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to find resource matches.');
      }
    });
  }

  allocateDirectly(): void {
    this.allocationForm.markAllAsTouched();
    const value = this.allocationForm.getRawValue();
    if (
      this.allocationForm.invalid
      || value.fromDate >= value.toDate
      || this.hasProjectEndDateError()
      || this.isSubmitting()
    ) {
      return;
    }

    this.submitAllocation(value);
  }

  allocateMatch(match: ResourceMatch): void {
    const projectId = this.aiForm.controls.projectId.value;
    const intent = this.matches()?.intent;
    if (!projectId) {
      return;
    }

    const utilisationPercent =
      intent?.availabilityRequirement ?? match.availablePercent;

    if (utilisationPercent <= 0) {
      this.pageState.setError('This employee has no remaining allocation capacity.');
      return;
    }

    this.submitAllocation({
      projectId,
      employeeId: match.employee.id,
      utilisationPercent,
      fromDate: intent?.fromDate ?? new Date(),
      toDate: intent?.toDate ?? null
    });
  }

  endAllocation(allocation: ManagerAllocation): void {
    this.pageState.clearFeedback();
    this.isSubmitting.set(true);

    this.managerService.endAllocation(allocation.id).subscribe({
      next: () => {
        this.pageState.setSuccess('Allocation ended successfully.');
        this.isSubmitting.set(false);
        this.loadResources();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to end allocation.');
      }
    });
  }

  hasAllocationDateError(): boolean {
    const value = this.allocationForm.getRawValue();
    return !!value.fromDate && !!value.toDate && value.fromDate >= value.toDate;
  }

  hasProjectEndDateError(): boolean {
    const project = this.projects().find(
      item => item.id === this.allocationForm.controls.projectId.value
    );
    const toDate = this.allocationForm.controls.toDate.value;

    return !!project?.endDate
      && !!toDate
      && toDate > new Date(project.endDate);
  }

  selectedProjectEndDate(): Date | null {
    const project = this.projects().find(
      item => item.id === this.allocationForm.controls.projectId.value
    );

    return project?.endDate ? new Date(project.endDate) : null;
  }

  private submitAllocation(request: CreateManagerAllocationRequest): void {
    this.pageState.clearFeedback();
    this.isSubmitting.set(true);

    this.managerService.allocate(request).subscribe({
      next: () => {
        this.pageState.setSuccess('Resource allocated successfully.');
        this.isSubmitting.set(false);
        this.loadResources();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to allocate resource.');
      }
    });
  }

  private loadResources(): void {
    this.managerService.getResourceDashboard().subscribe({
      next: dashboard => {
        this.employees.set([...dashboard.onBench, ...dashboard.activeEmployees]);
        this.pageState.stopLoading();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load team resources.');
      }
    });
  }
}
