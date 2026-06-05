export interface Allocation {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeDesignation: string;
  projectId: string;
  projectName: string;
  projectManager: string;
  utilisationPercent: number;
  fromDate: Date | string;
  toDate: Date | string;
  isActive: boolean;
  createdAt: Date | string;
}

export type AllocationDTO = Allocation;
