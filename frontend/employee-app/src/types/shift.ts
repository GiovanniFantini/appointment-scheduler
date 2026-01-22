// Stesso file types del merchant-app
export enum ShiftType {
  Morning = 1,
  Afternoon = 2,
  Evening = 3,
  Night = 4,
  FullDay = 5,
  Custom = 6,
}

export interface ShiftEmployee {
  id: number;
  shiftId: number;
  employeeId: number;
  employeeName: string;
  isConfirmed: boolean;
  isCheckedIn: boolean;
  checkInTime?: string;
  isCheckedOut: boolean;
  checkOutTime?: string;
  checkInLocation?: string;
  checkOutLocation?: string;
  notes?: string;
}

export interface Shift {
  id: number;
  merchantId: number;
  shiftTemplateId?: number;
  shiftTemplateName?: string;
  /** @deprecated Use employees array for multi-employee assignment */
  employeeId?: number;
  /** @deprecated Use employees array for multi-employee assignment */
  employeeName?: string;
  /** Lista dipendenti assegnati al turno */
  employees: ShiftEmployee[];
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
