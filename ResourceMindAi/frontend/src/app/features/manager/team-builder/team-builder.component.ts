import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  ManagerProject,
  TeamBuilderResponse
} from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-manager-team-builder',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DropDownsModule,
    InputsModule,
    AppLayoutComponent,
    AvatarComponent,
    PageFeedbackComponent,
    PageHeaderComponent,
    StatusBadgeComponent
  ],
  templateUrl: './team-builder.component.html',
  providers: [PageStateService]
})
export class ManagerTeamBuilderComponent {
  private readonly managerService = inject(ManagerService);
  private readonly formBuilder = inject(FormBuilder);
  readonly pageState = inject(PageStateService);

  readonly projects = signal<ManagerProject[]>([]);
  readonly result = signal<TeamBuilderResponse | null>(null);
  readonly isBuilding = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    projectId: ['', Validators.required],
    requirement: ['', [Validators.required, Validators.minLength(5)]]
  });

  constructor() {
    this.loadProjects();
  }

  buildTeam(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isBuilding()) {
      return;
    }

    this.pageState.clearFeedback();
    this.result.set(null);
    this.isBuilding.set(true);

    this.managerService.buildTeam(this.form.getRawValue()).subscribe({
      next: response => {
        this.result.set(response);
        this.isBuilding.set(false);
      },
      error: err => {
        this.isBuilding.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to build a team recommendation.');
      }
    });
  }

  private loadProjects(): void {
    this.pageState.startLoading();
    this.managerService.getProjects().subscribe({
      next: projects => {
        this.projects.set(projects);
        this.pageState.stopLoading();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load manager projects.');
      }
    });
  }
}
