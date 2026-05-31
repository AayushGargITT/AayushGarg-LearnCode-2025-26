import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-card.component.html',
  styleUrl: './stat-card.component.css'
})
export class StatCardComponent {
  @Input() label: string = '';
  @Input() value: string | number = '';
  @Input() hint?: string;
  @Input() accent?: 'indigo' | 'emerald' | 'amber' | 'rose' = 'indigo';

  getRingClass(): string {
    switch (this.accent) {
      case 'emerald': return 'ring-emerald-500/30';
      case 'amber': return 'ring-amber-500/30';
      case 'rose': return 'ring-rose-500/30';
      case 'indigo':
      default:
        return 'ring-indigo-500/30';
    }
  }
}
