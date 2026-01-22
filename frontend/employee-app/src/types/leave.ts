export enum LeaveType {
  Ferie = 1,
  ROL = 2,
  PAR = 3,
  Malattia = 4,
  ExFestivita = 5,
  Welfare = 6,
  Permesso = 7,
  Altro = 8
}

export enum LeaveRequestStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  merchantId: number;
  leaveType: LeaveType;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  daysRequested: number;
  status: LeaveRequestStatus;
  statusName: string;
  notes?: string;
  responseNotes?: string;
  approvedByMerchantId?: number;
  responseAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLeaveRequest {
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface EmployeeLeaveBalance {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveType: LeaveType;
  leaveTypeName: string;
  year: number;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}
