import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { CreateProjectRequest, ProjectStatus } from '../../../../../core/models/project.model';
import { User } from '../../../../../core/models/user.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-create-project-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DateInputsModule,
    DialogModule,
    DropDownsModule,
    InputsModule,
    PageFeedbackComponent
  ],
  templateUrl: './create-project-dialog.component.html'
})
export class CreateProjectDialogComponent {
  private readonly formBuilder = new FormBuilder();

  @Input() managers: User[] = [];
  @Input() isSubmitting = false;
  @Input() error: string | null = null;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<CreateProjectRequest>();

  readonly statuses = Object.values(ProjectStatus);
  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    startDate: [new Date(), Validators.required],
    endDate: [new Date(), Validators.required],
    status: [ProjectStatus.PLANNED, Validators.required],
    managerId: ['', Validators.required]
  });

  submit(): void {
    this.form.markAllAsTouched();
    const value = this.form.getRawValue();

    if (this.form.invalid || value.startDate > value.endDate || this.isSubmitting) {
      return;
    }

    this.submitted.emit(value);
  }

  hasDateRangeError(): boolean {
    const value = this.form.getRawValue();
    return !!value.startDate && !!value.endDate && value.startDate > value.endDate;
  }
}
