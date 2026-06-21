import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  CreateMilestoneRequest,
  Milestone,
  MilestoneStatus,
  Project,
  UpdateMilestoneRequest
} from '../../../../../core/models/project.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-milestones-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DateInputsModule,
    DialogModule,
    DropDownsModule,
    InputsModule,
    PageFeedbackComponent,
    StatusBadgeComponent
  ],
  templateUrl: './milestones-dialog.component.html',
  styleUrl: './milestones-dialog.component.css'
})
export class MilestonesDialogComponent {
  private readonly formBuilder = new FormBuilder();

  @Input() project: Project | null = null;
  @Input() milestones: Milestone[] = [];
  @Input() isLoading = false;
  @Input() isSaving = false;
  @Input() error: string | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() milestoneAdded = new EventEmitter<CreateMilestoneRequest>();
  @Output() milestoneUpdated = new EventEmitter<{ milestoneId: string; request: UpdateMilestoneRequest }>();

  readonly statuses = Object.values(MilestoneStatus);
  readonly editingMilestoneId = signal<string | null>(null);

  readonly addForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    dueDate: [new Date(), Validators.required],
    status: [MilestoneStatus.PENDING, Validators.required]
  });

  readonly editForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    dueDate: [new Date(), Validators.required],
    status: [MilestoneStatus.PENDING, Validators.required]
  });

  submitAdd(): void {
    this.addForm.markAllAsTouched();
    if (this.addForm.invalid || this.isSaving) {
      return;
    }

    this.milestoneAdded.emit(this.addForm.getRawValue());
  }

  edit(milestone: Milestone): void {
    this.editingMilestoneId.set(milestone.id);
    this.editForm.reset({
      title: milestone.title,
      dueDate: new Date(milestone.dueDate),
      status: milestone.status as MilestoneStatus
    });
  }

  cancelEdit(): void {
    this.editingMilestoneId.set(null);
  }

  submitUpdate(): void {
    const milestoneId = this.editingMilestoneId();
    this.editForm.markAllAsTouched();
    if (!milestoneId || this.editForm.invalid || this.isSaving) {
      return;
    }

    this.milestoneUpdated.emit({ milestoneId, request: this.editForm.getRawValue() });
  }
}
