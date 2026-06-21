import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { Project, UpdateProjectManagerRequest } from '../../../../../core/models/project.model';
import { User } from '../../../../../core/models/user.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-update-project-manager-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    PageFeedbackComponent
  ],
  templateUrl: './update-project-manager-dialog.component.html'
})
export class UpdateProjectManagerDialogComponent implements OnChanges {
  private readonly formBuilder = inject(FormBuilder);

  @Input() project: Project | null = null;
  @Input() managers: User[] = [];
  @Input() isSaving = false;
  @Input() error: string | null = null;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<UpdateProjectManagerRequest>();

  readonly form = this.formBuilder.group({
    newManagerId: ['', Validators.required]
  });

  ngOnChanges(): void {
    if (this.project) {
      this.form.reset({ newManagerId: '' });
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.emit(this.form.getRawValue() as UpdateProjectManagerRequest);
  }
}
