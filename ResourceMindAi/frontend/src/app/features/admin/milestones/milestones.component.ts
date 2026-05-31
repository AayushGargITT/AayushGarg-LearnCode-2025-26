import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-milestones',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './milestones.component.html',
  styleUrl: './milestones.component.css'
})
export class AdminMilestonesComponent {}
