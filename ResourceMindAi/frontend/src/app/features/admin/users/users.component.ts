import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class AdminUsersComponent {
  drawerOpen = signal(false);
  roles = ['Admin', 'Manager', 'Employee'];

  rows = [
    { id: "U-1001", name: "Asha Rao", email: "asha@acme.io", username: "asha.rao", role: "Admin", status: "Active" },
    { id: "U-1002", name: "Marcus Lee", email: "marcus@acme.io", username: "marcus.lee", role: "Manager", status: "Active" },
    { id: "U-1003", name: "Priya Shah", email: "priya@acme.io", username: "priya.shah", role: "Manager", status: "Active" },
    { id: "U-1004", name: "Elena Patel", email: "elena@acme.io", username: "elena.patel", role: "Employee", status: "Active" },
    { id: "U-1005", name: "Jonas Weber", email: "jonas@acme.io", username: "jonas.weber", role: "Employee", status: "Inactive" },
    { id: "U-1006", name: "Maya Chen", email: "maya@acme.io", username: "maya.chen", role: "Employee", status: "Active" },
  ];

  openDrawer() { this.drawerOpen.set(true); }
  closeDrawer() { this.drawerOpen.set(false); }
}
