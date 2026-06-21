export enum Role {
  ADMIN = 'Admin',
  MANAGER = 'Manager',
  RESOURCE = 'Resource'
}

export interface User {
  id: string;
  fullName: string;
  email: string;
  username: string;
  passwordHash?: string;
  role: Role;
  isActive: boolean;
  forcePasswordChange: boolean;
  employeeId?: string;
  department?: string;
  designation?: string;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  username: string;
  temporaryPassword: string;
  role: Role;
  department?: string | null;
  designation?: string | null;
}

export interface DeactivateUserResult {
  user: User;
  message: string;
  endedAllocationCount: number;
}

export interface ManagerDeactivationDetails {
  projects: string[];
  employees: string[];
}
