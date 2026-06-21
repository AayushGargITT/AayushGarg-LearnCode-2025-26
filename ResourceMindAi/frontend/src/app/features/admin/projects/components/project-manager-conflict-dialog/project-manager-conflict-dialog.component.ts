import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { ProjectManagerUpdateValidation } from '../../../../../core/models/project.model';

@Component({
  selector: 'app-project-manager-conflict-dialog',
  standalone: true,
  imports: [CommonModule, ButtonsModule, DialogModule],
  templateUrl: './project-manager-conflict-dialog.component.html'
})
export class ProjectManagerConflictDialogComponent {
  @Input() details: ProjectManagerUpdateValidation | null = null;
  @Output() closed = new EventEmitter<void>();
}
