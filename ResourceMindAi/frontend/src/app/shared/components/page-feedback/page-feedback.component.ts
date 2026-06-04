import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export type PageFeedbackType = 'success' | 'error' | 'info';

@Component({
  selector: 'app-page-feedback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './page-feedback.component.html',
  styleUrl: './page-feedback.component.css'
})
export class PageFeedbackComponent {
  @Input() message: string | null = null;
  @Input() type: PageFeedbackType = 'info';
}
