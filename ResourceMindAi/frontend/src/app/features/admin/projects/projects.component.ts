import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { GridModule } from '@progress/kendo-angular-grid';
import {
  CreateMilestoneRequest,
  CreateProjectRequest,
  Milestone,
  Project,
  ProjectManagerUpdateValidation,
  UpdateProjectManagerRequest,
  UpdateMilestoneRequest
} from '../../../core/models/project.model';
import { User } from '../../../core/models/user.model';
import { AdminProjectService } from '../../../core/services/admin-project.service';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { RowActionItem, RowActionMenuComponent } from '../../../shared/components/row-action-menu/row-action-menu.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';
import { CreateProjectDialogComponent } from './components/create-project-dialog/create-project-dialog.component';
import { MilestonesDialogComponent } from './components/milestones-dialog/milestones-dialog.component';
import { ProjectManagerConflictDialogComponent } from './components/project-manager-conflict-dialog/project-manager-conflict-dialog.component';
import { UpdateProjectManagerDialogComponent } from './components/update-project-manager-dialog/update-project-manager-dialog.component';

type ProjectAction = 'manageMilestones' | 'updateManager';
type ProjectActionItem = RowActionItem<ProjectAction>;

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    PageFeedbackComponent,
    RowActionMenuComponent,
    CreateProjectDialogComponent,
    MilestonesDialogComponent,
    UpdateProjectManagerDialogComponent,
    ProjectManagerConflictDialogComponent
  ],
  templateUrl: './projects.component.html',
  providers: [PageStateService]
})
export class AdminProjectsComponent {
  private readonly adminProjectService = inject(AdminProjectService);
  private readonly adminUserService = inject(AdminUserService);
  readonly pageState = inject(PageStateService);

  readonly rows = signal<Project[]>([]);
  readonly managers = signal<User[]>([]);
  readonly createDialogOpen = signal(false);
  readonly isCreating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly selectedProject = signal<Project | null>(null);
  readonly milestones = signal<Milestone[]>([]);
  readonly isMilestoneLoading = signal(false);
  readonly isMilestoneSaving = signal(false);
  readonly milestoneError = signal<string | null>(null);
  readonly managerProject = signal<Project | null>(null);
  readonly isManagerSaving = signal(false);
  readonly managerError = signal<string | null>(null);
  readonly managerConflicts = signal<ProjectManagerUpdateValidation | null>(null);

  readonly availableProjectManagers = computed(() => {
    const currentManagerId = this.managerProject()?.managerId;
    return this.managers().filter(manager => manager.id !== currentManagerId);
  });

  constructor() {
    this.loadProjects();
    this.loadManagers();
  }

  loadProjects(): void {
    this.pageState.startLoading();
    this.adminProjectService.getAllProjects().subscribe({
      next: projects => {
        this.rows.set(projects);
        this.pageState.stopLoading();
      },
      error: err => {
        this.rows.set([]);
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load projects.');
      }
    });
  }

  openCreateDialog(): void {
    this.createError.set(null);
    this.pageState.clearFeedback();
    this.createDialogOpen.set(true);
  }

  closeCreateDialog(): void {
    this.createDialogOpen.set(false);
    this.createError.set(null);
    this.isCreating.set(false);
  }

  createProject(request: CreateProjectRequest): void {
    this.createError.set(null);
    this.isCreating.set(true);

    this.adminProjectService.createProject(request).subscribe({
      next: () => {
        this.closeCreateDialog();
        this.pageState.setSuccess('Project created successfully.');
        this.loadProjects();
      },
      error: err => {
        this.isCreating.set(false);
        this.createError.set(err.error?.message ?? 'Unable to create project.');
      }
    });
  }

  actionsFor(): ProjectActionItem[] {
    return [
      { text: 'Manage Milestones', action: 'manageMilestones' },
      { text: 'Update Manager', action: 'updateManager' }
    ];
  }

  onProjectAction(project: Project, item: ProjectActionItem): void {
    if (item.action === 'manageMilestones') {
      this.openMilestones(project);
      return;
    }

    if (item.action === 'updateManager') {
      this.openManagerDialog(project);
    }
  }

  closeMilestones(): void {
    this.selectedProject.set(null);
    this.milestones.set([]);
    this.milestoneError.set(null);
    this.isMilestoneLoading.set(false);
    this.isMilestoneSaving.set(false);
  }

  addMilestone(request: CreateMilestoneRequest): void {
    const project = this.selectedProject();
    if (!project) {
      return;
    }

    this.milestoneError.set(null);
    this.isMilestoneSaving.set(true);

    this.adminProjectService.addMilestone(project.id, request).subscribe({
      next: () => {
        this.pageState.setSuccess('Milestone added successfully.');
        this.loadMilestones(project.id);
        this.loadProjects();
      },
      error: err => {
        this.isMilestoneSaving.set(false);
        this.milestoneError.set(err.error?.message ?? 'Unable to add milestone.');
      }
    });
  }

  updateMilestone(event: { milestoneId: string; request: UpdateMilestoneRequest }): void {
    const project = this.selectedProject();
    if (!project) {
      return;
    }

    this.milestoneError.set(null);
    this.isMilestoneSaving.set(true);

    this.adminProjectService.updateMilestone(project.id, event.milestoneId, event.request).subscribe({
      next: () => {
        this.pageState.setSuccess('Milestone updated successfully.');
        this.loadMilestones(project.id);
        this.loadProjects();
      },
      error: err => {
        this.isMilestoneSaving.set(false);
        this.milestoneError.set(err.error?.message ?? 'Unable to update milestone.');
      }
    });
  }

  openManagerDialog(project: Project): void {
    this.managerProject.set(project);
    this.managerError.set(null);
    this.managerConflicts.set(null);
  }

  closeManagerDialog(): void {
    this.managerProject.set(null);
    this.managerError.set(null);
    this.isManagerSaving.set(false);
  }

  updateManager(request: UpdateProjectManagerRequest): void {
    const project = this.managerProject();
    if (!project) {
      return;
    }

    this.managerError.set(null);
    this.isManagerSaving.set(true);

    this.adminProjectService.updateManager(project.id, request).subscribe({
      next: result => {
        this.closeManagerDialog();
        this.pageState.setSuccess(result.message);
        this.loadProjects();
      },
      error: err => {
        this.isManagerSaving.set(false);

        const details = err.error?.details as ProjectManagerUpdateValidation | undefined;
        if (details?.conflicts?.length) {
          this.closeManagerDialog();
          this.managerConflicts.set(details);
          return;
        }

        this.managerError.set(err.error?.message ?? 'Unable to update project manager.');
      }
    });
  }

  closeManagerConflicts(): void {
    this.managerConflicts.set(null);
  }

  private loadManagers(): void {
    this.adminUserService.getActiveManagers().subscribe({
      next: managers => this.managers.set(managers),
      error: () => this.managers.set([])
    });
  }

  private openMilestones(project: Project): void {
    this.selectedProject.set(project);
    this.milestoneError.set(null);
    this.loadMilestones(project.id);
  }

  private loadMilestones(projectId: string): void {
    this.isMilestoneLoading.set(true);

    this.adminProjectService.getProjectMilestones(projectId).subscribe({
      next: milestones => {
        this.milestones.set(milestones);
        this.isMilestoneLoading.set(false);
        this.isMilestoneSaving.set(false);
      },
      error: err => {
        this.milestones.set([]);
        this.isMilestoneLoading.set(false);
        this.isMilestoneSaving.set(false);
        this.milestoneError.set(err.error?.message ?? 'Unable to load milestones.');
      }
    });
  }
}
