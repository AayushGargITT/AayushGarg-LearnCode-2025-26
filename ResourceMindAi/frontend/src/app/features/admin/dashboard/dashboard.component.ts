import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GridModule } from '@progress/kendo-angular-grid';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HealthDotComponent } from '../../../shared/components/health-dot/health-dot.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, GridModule, AppLayoutComponent, PageHeaderComponent, StatCardComponent, StatusBadgeComponent, HealthDotComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class AdminDashboardComponent {
  recentProjects: { name: string, manager: string, status: string, health: any }[] = [
    { name: "Atlas Payments", manager: "Marcus Lee", status: "ACTIVE", health: "ON_TRACK" },
    { name: "Borealis CRM", manager: "Priya Shah", status: "ACTIVE", health: "NEEDS_ATTENTION" },
    { name: "Cobalt Insights", manager: "Marcus Lee", status: "ON_HOLD", health: "AT_RISK" },
    { name: "Delta Auth", manager: "Sara Kim", status: "PLANNED", health: "ON_TRACK" },
  ];
}
