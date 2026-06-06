import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { ManagerProject, ManagerProjectDetail } from '../../../core/models/manager.model';
import { ManagerService } from '../../../core/services/manager.service';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { HealthDotComponent } from '../../../shared/components/health-dot/health-dot.component';
import { PageFeedbackComponent } from '../../../shared/components/page-feedback/page-feedback.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PageStateService } from '../../../shared/services/page-state.service';

@Component({
  selector: 'app-manager-projects',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    GridModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    HealthDotComponent,
    PageFeedbackComponent
  ],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.css',
  providers: [PageStateService]
})
export class ManagerProjectsComponent {
  private readonly managerService = inject(ManagerService);
  readonly pageState = inject(PageStateService);

  readonly projects = signal<ManagerProject[]>([]);
  readonly selectedProject = signal<ManagerProjectDetail | null>(null);
  readonly isDetailLoading = signal(false);

  constructor() {
    this.loadProjects();
  }

  loadProjects(): void {
    this.pageState.startLoading();
    this.managerService.getProjects().subscribe({
      next: projects => {
        this.projects.set(projects);
        this.pageState.stopLoading();
      },
      error: err => {
        this.pageState.stopLoading();
        this.pageState.setError(err.error?.message ?? 'Unable to load projects.');
      }
    });
  }

  openProject(project: ManagerProject): void {
    this.isDetailLoading.set(true);
    this.managerService.getProjectDetail(project.id).subscribe({
      next: detail => {
        this.selectedProject.set(detail);
        this.isDetailLoading.set(false);
      },
      error: err => {
        this.isDetailLoading.set(false);
        this.pageState.setError(err.error?.message ?? 'Unable to load project detail.');
      }
    });
  }

  closeProject(): void {
    this.selectedProject.set(null);
  }
}
