// Enums
export enum ShiftType {
  Morning = 1,
  Afternoon = 2,
  Evening = 3,
  Night = 4,
  FullDay = 5,
  Custom = 6,
}

export enum RecurrencePattern {
  None = 0,
  Daily = 1,
  Weekly = 2,
  BiWeekly = 3,
  Monthly = 4,
}

export enum ShiftSwapStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4,
}

// Interfaces
export interface ShiftTemplate {
  id: number;
  merchantId: number;
  name: string;
  description?: string;
  shiftType: ShiftType;
  shiftTypeName: string;
  startTime: string; // TimeSpan as string
  endTime: string;
  breakDurationMinutes: number;
  totalHours: number;
  recurrencePattern: RecurrencePattern;
  recurrenceDays?: string;
  color: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface Shift {
  id: number;
  merchantId: number;
  shiftTemplateId?: number;
  shiftTemplateName?: string;
  employeeId?: number;
  employeeName?: string;
  date: string;
  startTime: string;
  endTime: string;
  breakDurationMinutes: number;
  totalHours: number;
  shiftType: ShiftType;
  shiftTypeName: string;
  color: string;
  notes?: string;
  isConfirmed: boolean;
  isCheckedIn: boolean;
  checkInTime?: string;
  isCheckedOut: boolean;
  checkOutTime?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface EmployeeShiftStats {
  employeeId: number;
  employeeName: string;
  totalHoursThisWeek: number;
  totalHoursThisMonth: number;
  totalHoursLastMonth: number;
  totalShiftsThisWeek: number;
  totalShiftsThisMonth: number;
  totalShiftsLastMonth: number;
  averageHoursPerShift: number;
  maxHoursPerWeek?: number;
  maxHoursPerMonth?: number;
  remainingHoursThisWeek: number;
  remainingHoursThisMonth: number;
  isOverLimit: boolean;
}

export interface ShiftSwapRequest {
  id: number;
  shiftId: number;
  shift: Shift;
  requestingEmployeeId: number;
  requestingEmployeeName: string;
  targetEmployeeId?: number;
  targetEmployeeName?: string;
  offeredShiftId?: number;
  offeredShift?: Shift;
  status: ShiftSwapStatus;
  statusName: string;
  message?: string;
  responseMessage?: string;
  requiresMerchantApproval: boolean;
  merchantResponseAt?: string;
  approvedByMerchantId?: number;
  createdAt: string;
  updatedAt?: string;
}

export interface EmployeeWorkingHoursLimit {
  id: number;
  employeeId: number;
  employeeName: string;
  merchantId: number;
  maxHoursPerDay?: number;
  maxHoursPerWeek?: number;
  maxHoursPerMonth?: number;
  minHoursPerWeek?: number;
  minHoursPerMonth?: number;
  allowOvertime: boolean;
  maxOvertimeHoursPerWeek?: number;
  maxOvertimeHoursPerMonth?: number;
  validFrom: string;
  validTo?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Request DTOs
export interface CreateShiftTemplateRequest {
  name: string;
  description?: string;
  shiftType: ShiftType;
  startTime: string;
  endTime: string;
  breakDurationMinutes: number;
  recurrencePattern: RecurrencePattern;
  recurrenceDays?: string;
  color: string;
}

export interface CreateShiftRequest {
  shiftTemplateId?: number;
  employeeId?: number;
  date: string;
  startTime: string;
  endTime: string;
  breakDurationMinutes: number;
  shiftType: ShiftType;
  color: string;
  notes?: string;
}

export interface CreateShiftsFromTemplateRequest {
  shiftTemplateId: number;
  startDate: string;
  endDate: string;
  employeeId?: number;
  daysOfWeek?: number[];
}

export interface UpdateShiftRequest {
  employeeId?: number;
  date: string;
  startTime: string;
  endTime: string;
  breakDurationMinutes: number;
  shiftType: ShiftType;
  color: string;
  notes?: string;
  isActive: boolean;
}

export interface AssignShiftRequest {
  employeeId: number;
  notes?: string;
}

export interface CreateEmployeeWorkingHoursLimitRequest {
  employeeId: number;
  maxHoursPerDay?: number;
  maxHoursPerWeek?: number;
  maxHoursPerMonth?: number;
  minHoursPerWeek?: number;
  minHoursPerMonth?: number;
  allowOvertime: boolean;
  maxOvertimeHoursPerWeek?: number;
  maxOvertimeHoursPerMonth?: number;
  validFrom: string;
  validTo?: string;
}
