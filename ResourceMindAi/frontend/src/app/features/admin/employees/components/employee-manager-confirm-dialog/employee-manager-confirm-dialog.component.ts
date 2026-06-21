import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { Employee, EmployeeManagerUpdatePreview } from '../../../../../core/models/employee.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-employee-manager-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ButtonsModule, DialogModule, PageFeedbackComponent],
  templateUrl: './employee-manager-confirm-dialog.component.html'
})
export class EmployeeManagerConfirmDialogComponent {
  @Input() employee: Employee | null = null;
  @Input() preview: EmployeeManagerUpdatePreview | null = null;
  @Input() isSaving = false;
  @Input() error: string | null = null;

  @Output() cancelled = new EventEmitter<void>();
  @Output() confirmed = new EventEmitter<void>();
}
