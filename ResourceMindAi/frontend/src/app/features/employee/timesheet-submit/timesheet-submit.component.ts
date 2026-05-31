import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AppLayoutComponent } from '../../../shared/components/app-layout/app-layout.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-employee-timesheet-submit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AppLayoutComponent, PageHeaderComponent],
  templateUrl: './timesheet-submit.component.html',
  styleUrl: './timesheet-submit.component.css'
})
export class EmployeeTimesheetSubmitComponent {
  fb = inject(FormBuilder);
  
  tsForm = this.fb.group({
    weekStart: ['', Validators.required],
    project: ['p1', Validators.required],
    hours: ['', [Validators.required, Validators.min(0)]],
    tags: ['']
  });

  onSubmit() {
    if (this.tsForm.invalid) return;
    alert('Timesheet Submitted!');
    this.tsForm.reset({ project: 'p1' });
  }
}
