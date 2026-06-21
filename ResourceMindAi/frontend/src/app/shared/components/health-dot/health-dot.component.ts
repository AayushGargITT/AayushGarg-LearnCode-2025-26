import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-health-dot',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './health-dot.component.html'
})
export class HealthDotComponent {
  @Input() health: string = 'Healthy';

  getColorClass(): string {
    const health = this.normalizedHealth();
    return health === 'healthy' || health === 'on track' || health === 'green' ? 'bg-emerald-500'
      : health === 'at risk' || health === 'atrisk' || health === 'attention' || health === 'needs attention' || health === 'amber' ? 'bg-amber-500'
      : 'bg-rose-500';
  }

  getLabel(): string {
    const health = this.normalizedHealth();
    return health === 'healthy' || health === 'on track' || health === 'green' ? 'Healthy'
      : health === 'at risk' || health === 'atrisk' || health === 'attention' || health === 'needs attention' || health === 'amber' ? 'At risk'
      : 'Critical';
  }

  private normalizedHealth(): string {
    return (this.health ?? '')
      .replace(/_/g, ' ')
      .trim()
      .toLowerCase();
  }
}
