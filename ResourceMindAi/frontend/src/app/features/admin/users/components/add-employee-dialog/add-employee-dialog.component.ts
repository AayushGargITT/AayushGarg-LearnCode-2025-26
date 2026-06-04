import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AddEmployeeRequest, User } from '../../../../../core/models/user.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-add-employee-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    InputsModule,
    PageFeedbackComponent
  ],
  templateUrl: './add-employee-dialog.component.html'
})
export class AddEmployeeDialogComponent {
  private readonly fb = inject(FormBuilder);

  @Input() user: User | null = null;
  @Input() error: string | null = null;
  @Input() isSaving = false;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<AddEmployeeRequest>();

  readonly form = this.fb.group({
    designation: ['', Validators.required],
    department: ['', Validators.required],
  });

  cancel(): void {
    this.form.reset();
    this.cancelled.emit();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.emit(this.form.getRawValue() as AddEmployeeRequest);
  }
}
