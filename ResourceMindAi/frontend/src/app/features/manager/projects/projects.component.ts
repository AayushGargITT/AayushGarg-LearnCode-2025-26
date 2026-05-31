import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HealthDotComponent } from '../../../shared/components/health-dot/health-dot.component';

@Component({
  selector: 'app-manager-projects',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent, StatusBadgeComponent, HealthDotComponent],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.css'
})
export class ManagerProjectsComponent {
  projects: any[] = [
    {
      name: "Atlas Payments",
      desc: "Core payment gateway integration with Stripe and PayPal.",
      status: "ACTIVE",
      health: "ON_TRACK",
      teamSize: 4,
      milestones: [
        { title: "Requirements Sign-off", date: "Feb 15", done: true },
        { title: "Alpha Release", date: "Jun 30", done: false },
        { title: "Beta Release", date: "Sep 30", done: false },
      ]
    },
    {
      name: "Cobalt Insights",
      desc: "Data analytics and reporting platform for enterprise clients.",
      status: "ON_HOLD",
      health: "AT_RISK",
      teamSize: 2,
      milestones: [
        { title: "Data Pipeline", date: "Jan 30", done: true },
        { title: "Dashboard MVP", date: "Mar 15", done: false },
      ]
    }
  ];
}
