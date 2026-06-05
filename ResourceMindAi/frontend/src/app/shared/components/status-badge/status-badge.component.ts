import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReplaceUnderscorePipe } from '../../pipes/replace-underscore.pipe';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, ReplaceUnderscorePipe],
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.css'
})
export class StatusBadgeComponent {
  @Input() status: string = '';

  getBadgeClass(): string {
    const map: Record<string, string> = {
      BENCH: "muted",
      ALLOCATED: "indigo",
      Bench: "muted",
      Allocated: "indigo",
      SUBMITTED: "emerald",
      MISSED: "amber",
      PLANNED: "slate",
      ACTIVE: "emerald",
      COMPLETED: "indigo",
      ON_HOLD: "amber",
      Planned: "slate",
      Completed: "indigo",
      OnHold: "amber",
      Cancelled: "muted",
      NOT_STARTED: "muted",
      IN_PROGRESS: "sky",
      DONE: "emerald",
      Pending: "muted",
      InProgress: "sky",
      Overdue: "amber",
      Active: "emerald",
      Inactive: "muted",
      Ended: "muted",
      Admin: "rose",
      Manager: "indigo",
      Employee: "slate",
      Beginner: "muted",
      Intermediate: "sky",
      Advanced: "emerald",
      Expert: "indigo",
    };
    return map[this.status] ?? "muted";
  }
}
