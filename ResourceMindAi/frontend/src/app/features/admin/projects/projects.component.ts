import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    DateInputsModule,
    DialogModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent
  ],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.css'
})
export class AdminProjectsComponent {
  drawerOpen = signal(false);

  rows = [
    { id: "P-101", name: "Atlas Payments", manager: "Marcus Lee", timeline: "Jan '24 - Dec '24", status: "ACTIVE" },
    { id: "P-102", name: "Borealis CRM", manager: "Priya Shah", timeline: "Mar '24 - Nov '24", status: "ACTIVE" },
    { id: "P-103", name: "Cobalt Insights", manager: "Marcus Lee", timeline: "Jan '24 - Jun '24", status: "ON_HOLD" },
    { id: "P-104", name: "Delta Auth", manager: "Sara Kim", timeline: "Sep '24 - Mar '25", status: "PLANNED" },
  ];

  openDrawer() { this.drawerOpen.set(true); }
  closeDrawer() { this.drawerOpen.set(false); }
}
