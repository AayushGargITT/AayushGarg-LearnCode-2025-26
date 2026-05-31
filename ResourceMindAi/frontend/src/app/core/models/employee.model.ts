export enum EmployeeStatus {
  BENCH = 'BENCH',
  ALLOCATED = 'ALLOCATED'
}

export enum SkillCategory {
  BACKEND = 'BACKEND',
  FRONTEND = 'FRONTEND',
  DEVOPS = 'DEVOPS',
  QA = 'QA',
  OTHER = 'OTHER'
}

export enum ProficiencyLevel {
  BEGINNER = 'BEGINNER',
  INTERMEDIATE = 'INTERMEDIATE',
  ADVANCED = 'ADVANCED'
}

export interface Skill {
  id: string;
  employeeId: string;
  skillName: string;
  category: SkillCategory | string;
  proficiency: ProficiencyLevel | string;
  addedAt: Date;
}

export interface Employee {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  status: EmployeeStatus | string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface EmployeeDetailDTO extends Employee {
  skills: Skill[];
  activeAllocations: any[]; // To be defined with Allocation
  recentTags: string[];
}
