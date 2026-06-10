import { Role } from './user.model';

export enum EmployeeStatus {
  BENCH = 'Bench',
  ALLOCATED = 'Allocated'
}

export enum SkillCategory {
  TECHNICAL = 'Technical',
  SOFT = 'Soft',
  MANAGEMENT = 'Management',
  DOMAIN = 'Domain'
}

export enum ProficiencyLevel {
  BEGINNER = 'Beginner',
  INTERMEDIATE = 'Intermediate',
  ADVANCED = 'Advanced',
  EXPERT = 'Expert'
}

export interface EmployeeSkill {
  id: string;
  employeeId: string;
  skillName: string;
  category: SkillCategory | string;
  proficiency: ProficiencyLevel | string;
  addedAt: string | Date;
}

export interface Employee {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  role: Role | string;
  department: string;
  designation: string;
  allocationStatus: EmployeeStatus | string;
  isActive: boolean;
  managerId?: string | null;
  managerName?: string | null;
}

export interface UpdateEmployeeManagerRequest {
  newManagerId: string;
}

export interface EmployeeManagerUpdatePreview {
  employeeId: string;
  currentManagerId: string | null;
  newManagerId: string;
  activeProjects: string[];
}

export interface EmployeeManagerUpdateResult {
  employee: Employee;
  endedProjects: string[];
  message: string;
}

export interface CreateEmployeeSkillRequest {
  skillName: string;
  category: SkillCategory;
  proficiency: ProficiencyLevel;
}

export interface UpdateEmployeeSkillProficiencyRequest {
  proficiency: ProficiencyLevel;
}

export interface EmployeeDetailDTO extends Employee {
  skills: EmployeeSkill[];
  activeAllocations: any[];
  recentTags: string[];
}
