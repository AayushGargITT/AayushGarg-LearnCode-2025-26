import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { ManagerDeactivationDetails } from '../../../../../core/models/user.model';

@Component({
  selector: 'app-deactivation-blocked-dialog',
  standalone: true,
  imports: [CommonModule, ButtonsModule, DialogModule],
  templateUrl: './deactivation-blocked-dialog.component.html'
})
export class DeactivationBlockedDialogComponent {
  @Input() details: ManagerDeactivationDetails | null = null;
  @Output() closed = new EventEmitter<void>();
}
