import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { InputsModule } from '@progress/kendo-angular-inputs';
import {
  CreateEmployeeSkillRequest,
  Employee,
  EmployeeSkill,
  ProficiencyLevel,
  SkillCategory,
  UpdateEmployeeSkillProficiencyRequest
} from '../../../../../core/models/employee.model';
import { PageFeedbackComponent } from '../../../../../shared/components/page-feedback/page-feedback.component';

@Component({
  selector: 'app-employee-skills-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    DialogModule,
    DropDownsModule,
    InputsModule,
    PageFeedbackComponent
  ],
  templateUrl: './employee-skills-dialog.component.html',
  styleUrl: './employee-skills-dialog.component.css'
})
export class EmployeeSkillsDialogComponent implements OnChanges {
  private readonly formBuilder = new FormBuilder();

  @Input() employee: Employee | null = null;
  @Input() skills: EmployeeSkill[] = [];
  @Input() isLoading = false;
  @Input() isSaving = false;
  @Input() error: string | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() skillAdded = new EventEmitter<CreateEmployeeSkillRequest>();
  @Output() proficiencyUpdated = new EventEmitter<{ skillId: string; request: UpdateEmployeeSkillProficiencyRequest }>();

  readonly categories = Object.values(SkillCategory);
  readonly proficiencies = Object.values(ProficiencyLevel);
  readonly proficiencyBySkill = signal<Record<string, ProficiencyLevel>>({});

  readonly addSkillForm = this.formBuilder.nonNullable.group({
    skillName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    category: [SkillCategory.TECHNICAL, Validators.required],
    proficiency: [ProficiencyLevel.BEGINNER, Validators.required]
  });

  ngOnChanges(): void {
    this.proficiencyBySkill.set(
      this.skills.reduce<Record<string, ProficiencyLevel>>((selected, skill) => {
        selected[skill.id] = skill.proficiency as ProficiencyLevel;
        return selected;
      }, {})
    );
  }

  submitSkill(): void {
    this.addSkillForm.markAllAsTouched();
    if (this.addSkillForm.invalid || this.isSaving) {
      return;
    }

    this.skillAdded.emit(this.addSkillForm.getRawValue());
  }

  setSkillProficiency(skillId: string, proficiency: ProficiencyLevel): void {
    this.proficiencyBySkill.update(values => ({ ...values, [skillId]: proficiency }));
  }

  updateProficiency(skill: EmployeeSkill): void {
    const proficiency = this.proficiencyBySkill()[skill.id];
    if (!proficiency || proficiency === skill.proficiency || this.isSaving) {
      return;
    }

    this.proficiencyUpdated.emit({ skillId: skill.id, request: { proficiency } });
  }

  resetForm(): void {
    this.addSkillForm.reset({
      skillName: '',
      category: SkillCategory.TECHNICAL,
      proficiency: ProficiencyLevel.BEGINNER
    });
  }
}
