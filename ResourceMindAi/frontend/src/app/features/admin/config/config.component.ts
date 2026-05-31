import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [CommonModule, AppLayoutComponent, PageHeaderComponent],
  templateUrl: './config.component.html',
  styleUrl: './config.component.css'
})
export class AdminConfigComponent {}
