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
  @Input() health: string = 'ON_TRACK';

  getColorClass(): string {
    return this.health === 'ON_TRACK' || this.health === 'Green' ? 'bg-emerald-500'
      : this.health === 'NEEDS_ATTENTION' || this.health === 'Amber' ? 'bg-amber-500'
      : 'bg-rose-500';
  }

  getLabel(): string {
    return this.health === 'ON_TRACK' || this.health === 'Green' ? 'On track'
      : this.health === 'NEEDS_ATTENTION' || this.health === 'Amber' ? 'Needs attention'
      : 'At risk';
  }
}
