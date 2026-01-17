export enum AnomalyType {
  LateCheckIn = 1,
  EarlyCheckIn = 2,
  LateCheckOut = 3,
  EarlyCheckOut = 4,
  MissingCheckIn = 5,
  MissingCheckOut = 6,
  ExtendedBreak = 7,
  UnusualPattern = 8,
  LocationMismatch = 9,
}

export enum AnomalyReason {
  Traffic = 1,
  AuthorizedLeave = 2,
  TimeRecovery = 3,
  PersonalEmergency = 4,
  Forgotten = 5,
  TechnicalIssue = 6,
  SmartWorking = 7,
  Other = 8,
  NotSpecified = 9,
}

export enum OvertimeType {
  Paid = 1,
  BankedHours = 2,
  TimeRecovery = 3,
  Voluntary = 4,
  Pending = 5,
}

export interface TimbratureResponse {
  success: boolean;
  message: string;
  context?: string;
  nextSteps?: string;
  empatheticMessage?: string;
  hasAnomaly: boolean;
  anomalyType?: AnomalyType;
  anomalyId?: number;
  quickResolutionOptions?: string[];
  suggestedBreakTime?: string;
  totalShiftHours?: number;
  hasOvertime: boolean;
  overtimeMinutes?: number;
  timestamp: string;
}

export interface CurrentStatusResponse {
  isCheckedIn: boolean;
  isOnBreak: boolean;
  checkInTime?: string;
  breakStartTime?: string;
  statusMessage: string;
  suggestedAction?: string;
  currentShift?: any;
  currentBreak?: ShiftBreak;
  totalWorkedHoursToday: number;
  totalWorkedHoursThisWeek: number;
}

export interface ShiftBreak {
  id: number;
  shiftId: number;
  breakStartTime: string;
  breakEndTime?: string;
  durationMinutes: number;
  breakType: string;
  isAutoSuggested: boolean;
  notes?: string;
  isShortBreak: boolean;
}

export interface ShiftAnomaly {
  id: number;
  shiftId: number;
  type: AnomalyType;
  severity: number;
  empatheticMessage: string;
  employeeReason?: AnomalyReason;
  employeeNotes?: string;
  isResolved: boolean;
  resolutionMethod?: string;
  merchantNotes?: string;
  detectedAt: string;
  resolvedAt?: string;
  requiresMerchantReview: boolean;
}

export interface WellbeingStats {
  hoursThisWeek: number;
  hoursThisMonth: number;
  overtimeThisWeek: number;
  overtimeThisMonth: number;
  hasWellbeingAlert: boolean;
  wellbeingMessage?: string;
  consecutiveLongDays: number;
  lastDayOff?: string;
}

export interface CheckInRequest {
  shiftId: number;
  location?: string;
  notes?: string;
}

export interface CheckOutRequest {
  shiftId: number;
  location?: string;
  notes?: string;
}

export interface StartBreakRequest {
  shiftId: number;
  breakType?: string;
  notes?: string;
}

export interface EndBreakRequest {
  breakId: number;
  notes?: string;
}

export interface ResolveAnomalyRequest {
  anomalyId: number;
  reason: AnomalyReason;
  notes?: string;
}

export interface ClassifyOvertimeRequest {
  overtimeId: number;
  type: OvertimeType;
  notes?: string;
}
