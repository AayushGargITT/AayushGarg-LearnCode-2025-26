import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-manager-resources',
  standalone: true,
  imports: [CommonModule, RouterModule, AppLayoutComponent, PageHeaderComponent, AvatarComponent, StatusBadgeComponent],
  templateUrl: './resources.component.html',
  styleUrl: './resources.component.css'
})
export class ManagerResourcesComponent {
  benchEmployees = [
    { name: "Jonas Weber", title: "Frontend Engineer", skills: ["React", "TypeScript"] },
    { name: "Diego Alvarez", title: "Platform Engineer", skills: ["AWS", "Terraform"] },
  ];
  
  allocatedEmployees = [
    { name: "Elena Patel", title: "Senior Backend Engineer", util: 100 },
    { name: "Maya Chen", title: "QA Lead", util: 100 },
    { name: "Ada Okonkwo", title: "Staff Engineer", util: 80 },
    { name: "Sara Kim", title: "Frontend Engineer", util: 50 },
  ];
}
