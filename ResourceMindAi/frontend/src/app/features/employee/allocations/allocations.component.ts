import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-employee-allocations',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './allocations.component.html',
  styleUrl: './allocations.component.css'
})
export class EmployeeAllocationsComponent {
  allocations = [
    { project: "Atlas Payments", manager: "Marcus Lee", util: 100, timeline: "Jan 10 - Dec 31" }
  ];
}
