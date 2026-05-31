import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { GridModule } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonsModule,
    DialogModule,
    GridModule,
    InputsModule,
    AppLayoutComponent,
    PageHeaderComponent,
    StatusBadgeComponent
  ],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.css'
})
export class AdminEmployeesComponent {
  filter = signal<string>('All');
  drawerOpen = signal(false);

  rows = [
    { id: "E-2001", name: "Elena Patel", dept: "Engineering", title: "Senior Backend Engineer", status: "ALLOCATED", active: "Active" },
    { id: "E-2002", name: "Jonas Weber", dept: "Engineering", title: "Frontend Engineer", status: "BENCH", active: "Active" },
    { id: "E-2003", name: "Maya Chen", dept: "QA", title: "QA Lead", status: "ALLOCATED", active: "Active" },
    { id: "E-2004", name: "Diego Alvarez", dept: "DevOps", title: "Platform Engineer", status: "BENCH", active: "Active" },
    { id: "E-2005", name: "Ada Okonkwo", dept: "Engineering", title: "Staff Engineer", status: "ALLOCATED", active: "Active" },
    { id: "E-2006", name: "Tomás Silva", dept: "Engineering", title: "Backend Engineer", status: "BENCH", active: "Inactive" },
  ];

  filteredRows = computed(() => {
    const f = this.filter();
    return this.rows.filter(r => f === 'All' || (f === 'Bench' ? r.status === 'BENCH' : r.status === 'ALLOCATED'));
  });

  setFilter(f: string) { this.filter.set(f); }
  openDrawer() { this.drawerOpen.set(true); }
  closeDrawer() { this.drawerOpen.set(false); }
}
