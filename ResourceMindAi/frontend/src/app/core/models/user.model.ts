export enum Role {
  ADMIN = 'Admin',
  MANAGER = 'Manager',
  EMPLOYEE = 'Employee'
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
  role: Role;
}

export interface AddEmployeeRequest {
  designation: string;
  department: string;
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
