import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GridModule } from '@progress/kendo-angular-grid';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-allocations',
  standalone: true,
  imports: [CommonModule, GridModule, AppLayoutComponent, PageHeaderComponent, StatusBadgeComponent],
  templateUrl: './allocations.component.html',
  styleUrl: './allocations.component.css'
})
export class AdminAllocationsComponent {
  rows = [
    { empId: "E-2001", empName: "Elena Patel", projId: "P-101", projName: "Atlas Payments", manager: "Marcus Lee", timeline: "Jan 10 - Dec 31", util: 100, status: "Active" },
    { empId: "E-2003", empName: "Maya Chen", projId: "P-101", projName: "Atlas Payments", manager: "Marcus Lee", timeline: "Feb 01 - Jun 30", util: 50, status: "Active" },
    { empId: "E-2005", empName: "Ada Okonkwo", projId: "P-102", projName: "Borealis CRM", manager: "Priya Shah", timeline: "Mar 15 - Nov 30", util: 80, status: "Active" },
    { empId: "E-2006", empName: "Tomás Silva", projId: "P-103", projName: "Cobalt Insights", manager: "Marcus Lee", timeline: "Jan 01 - Apr 30", util: 100, status: "Ended" },
  ];
}
