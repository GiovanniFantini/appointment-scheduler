export enum ValidationStatus {
  Pending = 0,
  AutoApproved = 1,
  ManuallyApproved = 2,
  RequiresReview = 3,
  Rejected = 4,
  SelfCorrected = 5,
}

export interface ShiftWithValidation {
  id: number;
  employeeName: string;
  date: string;
  startTime: string;
  endTime: string;
  checkInTime?: string;
  checkOutTime?: string;
  validationStatus: ValidationStatus;
  hasAnomalies: boolean;
  hasOvertime: boolean;
  totalHours: number;
}
