import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-avatar',
  standalone: true,
  templateUrl: './avatar.component.html'
})
export class AvatarComponent {
  @Input() name: string = '';
  @Input() size: number = 36;

  getInitials(): string {
    if (!this.name) return '';
    return this.name.split(" ").map(n => n[0]).slice(0, 2).join("").toUpperCase();
  }
}
