import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { AssignEmployeeManagerRequest, Employee } from '../../../../../core/models/employee.model';
import { User } from '../../../../../core/models/user.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-assign-manager-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    PageFeedbackComponent
  ],
  templateUrl: './assign-manager-dialog.component.html'
})
export class AssignManagerDialogComponent implements OnChanges {
  private readonly formBuilder = inject(FormBuilder);

  @Input() employee: Employee | null = null;
  @Input() managers: User[] = [];
  @Input() isLoading = false;
  @Input() isSaving = false;
  @Input() error: string | null = null;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<AssignEmployeeManagerRequest>();

  readonly form = this.formBuilder.group({
    managerId: ['', Validators.required]
  });

  ngOnChanges(): void {
    if (this.employee) {
      this.form.reset({ managerId: '' });
    }
  }

  cancel(): void {
    this.form.reset({ managerId: '' });
    this.cancelled.emit();
  }

  submit(): void {
    if (this.form.invalid || this.isSaving || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.emit(this.form.getRawValue() as AssignEmployeeManagerRequest);
  }
}
