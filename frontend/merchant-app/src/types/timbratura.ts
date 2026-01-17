export enum ValidationStatus {
  Pending = 1,
  AutoApproved = 2,
  ManuallyApproved = 3,
  RequiresReview = 4,
  Rejected = 5,
  SelfCorrected = 6,
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
