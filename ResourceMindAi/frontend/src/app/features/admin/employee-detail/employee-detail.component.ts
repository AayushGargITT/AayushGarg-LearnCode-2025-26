import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-employee-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, AppLayoutComponent, StatusBadgeComponent],
  templateUrl: './employee-detail.component.html'
})
export class AdminEmployeeDetailComponent {}
