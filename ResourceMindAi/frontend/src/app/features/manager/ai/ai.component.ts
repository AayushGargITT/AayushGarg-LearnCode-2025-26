import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLayoutComponent, PageHeaderComponent],
  templateUrl: './ai.component.html',
  styleUrl: './ai.component.css'
})
export class ManagerAiComponent {
  prompt = signal('');

  sendPrompt() {
    if (!this.prompt().trim()) return;
    this.prompt.set('');
  }
}
