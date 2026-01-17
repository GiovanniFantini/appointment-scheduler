export enum AnomalyType {
  LateCheckIn = 0,
  EarlyCheckIn = 1,
  LateCheckOut = 2,
  EarlyCheckOut = 3,
  MissingCheckIn = 4,
  MissingCheckOut = 5,
  ExtendedBreak = 6,
  UnusualPattern = 7,
  LocationMismatch = 8,
}

export enum AnomalyReason {
  Traffic = 0,
  AuthorizedLeave = 1,
  TimeRecovery = 2,
  PersonalEmergency = 3,
  Forgotten = 4,
  TechnicalIssue = 5,
  SmartWorking = 6,
  Other = 7,
  NotSpecified = 8,
}

export enum OvertimeType {
  Paid = 0,
  BankedHours = 1,
  TimeRecovery = 2,
  Voluntary = 3,
  Pending = 4,
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
