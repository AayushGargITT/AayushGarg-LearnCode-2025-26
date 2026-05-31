import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-health-dot',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './health-dot.component.html',
  styleUrl: './health-dot.component.css'
})
export class HealthDotComponent {
  @Input() health: 'ON_TRACK' | 'NEEDS_ATTENTION' | 'AT_RISK' = 'ON_TRACK';

  getColorClass(): string {
    return this.health === 'ON_TRACK' ? 'bg-emerald-500'
      : this.health === 'NEEDS_ATTENTION' ? 'bg-amber-500'
      : 'bg-rose-500';
  }

  getLabel(): string {
    return this.health === 'ON_TRACK' ? 'On track'
      : this.health === 'NEEDS_ATTENTION' ? 'Needs attention'
      : 'At risk';
  }
}
