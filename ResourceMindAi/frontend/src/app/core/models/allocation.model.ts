export interface Allocation {
  id: string;
  employeeId: string;
  employeeName?: string;
  projectId: string;
  projectName?: string;
  utilisationPercent: number;
  fromDate: Date | string;
  toDate: Date | string;
  isActive: boolean;
  createdAt: Date;
}

export interface AllocationDTO extends Allocation {
  employeeDesignation?: string;
  projectManager?: string;
}
