import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  private readonly destroyRef = inject(DestroyRef);

  @Input() error: string | null = null;
  @Input() isSaving = false;

  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<CreateUserRequest>();

  readonly roles = [Role.ADMIN, Role.MANAGER, Role.RESOURCE];

  readonly form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', Validators.required],
    temporaryPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)
      ]
    ],
    role: [Role.RESOURCE, Validators.required],
    department: ['', [Validators.maxLength(100)]],
    designation: ['', [Validators.maxLength(150)]],
  });

  constructor() {
    this.updateResourceFieldValidation(this.form.controls.role.value);

    this.form.controls.role.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(role => this.updateResourceFieldValidation(role));
  }

  cancel(): void {
    this.form.reset({
      role: Role.RESOURCE,
      fullName: '',
      email: '',
      username: '',
      temporaryPassword: '',
      department: '',
      designation: ''
    });
    this.updateResourceFieldValidation(Role.RESOURCE);
    this.cancelled.emit();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.emit(this.form.getRawValue() as CreateUserRequest);
  }

  private updateResourceFieldValidation(role: Role | null): void {
    const validators = role === Role.ADMIN
      ? [Validators.maxLength(100)]
      : [Validators.required, Validators.maxLength(100)];
    const designationValidators = role === Role.ADMIN
      ? [Validators.maxLength(150)]
      : [Validators.required, Validators.maxLength(150)];

    this.form.controls.department.setValidators(validators);
    this.form.controls.designation.setValidators(designationValidators);
    this.form.controls.department.updateValueAndValidity({ emitEvent: false });
    this.form.controls.designation.updateValueAndValidity({ emitEvent: false });
  }
}
