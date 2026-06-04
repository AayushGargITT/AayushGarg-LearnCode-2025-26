import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { CreateUserRequest, Role } from '../../../../../core/models/user.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-create-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    InputsModule,
    PageFeedbackComponent
  ],
  templateUrl: './create-user-dialog.component.html'
})
export class CreateUserDialogComponent {
  private readonly fb = inject(FormBuilder);

  @Input() error: string | null = null;
  @Input() isSaving = false;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<CreateUserRequest>();

  readonly roles = [Role.ADMIN, Role.MANAGER, Role.EMPLOYEE];

  readonly form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', Validators.required],
    role: [Role.EMPLOYEE, Validators.required],
  });

  cancel(): void {
    this.form.reset({ role: Role.EMPLOYEE });
    this.cancelled.emit();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.emit(this.form.getRawValue() as CreateUserRequest);
  }
}
