import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-manager-allocate',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent, AvatarComponent],
  templateUrl: './allocate.component.html',
  styleUrl: './allocate.component.css'
})
export class ManagerAllocateComponent {
  loading = signal(false);
  showResults = signal(false);

  findMatch() {
    this.loading.set(true);
    this.showResults.set(false);
    setTimeout(() => {
      this.loading.set(false);
      this.showResults.set(true);
    }, 1500);
  }
}
