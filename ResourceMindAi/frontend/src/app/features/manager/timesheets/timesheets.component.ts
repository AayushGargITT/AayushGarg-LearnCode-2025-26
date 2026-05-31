import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-manager-timesheets',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent, AvatarComponent, StatusBadgeComponent],
  templateUrl: './timesheets.component.html',
  styleUrl: './timesheets.component.css'
})
export class ManagerTimesheetsComponent {
  rows = [
    { empName: "Elena Patel", project: "Atlas Payments", hours: 40, tags: ["Backend API", "Bug Fixes"], status: "SUBMITTED" },
    { empName: "Maya Chen", project: "Atlas Payments", hours: 40, tags: ["Integration Testing", "E2E"], status: "SUBMITTED" },
    { empName: "Ada Okonkwo", project: "Borealis CRM", hours: 32, tags: ["Architecture"], status: "SUBMITTED" },
    { empName: "Sara Kim", project: "Delta Auth", hours: 0, tags: [], status: "MISSED" },
  ];
}
