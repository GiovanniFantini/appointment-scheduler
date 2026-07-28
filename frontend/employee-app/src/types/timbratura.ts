// Tipi della feature Timbratura, allineati ai DTO del backend
// (AppointmentScheduler.Shared/DTOs/TimeClockDto.cs).
// Gli enum rispecchiano i valori C# (base 1).

export enum TimeEntryType {
  ClockIn = 1,
  ClockOut = 2,
  BreakStart = 3,
  BreakEnd = 4,
}

export enum TimeEntrySource {
  Web = 1,
  Mobile = 2,
  Manual = 3,
}

export enum TimeEntryStatus {
  Ok = 1,
  Anomaly = 2,
  Corrected = 3,
  PendingReview = 4,
}

export interface TimeEntryDto {
  id: number
  employeeId: number
  employeeName: string
  branchId: number
  branchName: string
  eventId: number
  eventParticipantId: number
  eventTitle: string
  type: TimeEntryType
  typeName: string
  source: TimeEntrySource
  status: TimeEntryStatus
  statusName: string
  workDate: string
  actualTimestampUtc: string
  expectedTime?: string
  deviationMinutes?: number
  latitude?: number
  longitude?: number
  gpsAccuracyMeters?: number
  distanceFromBranchMeters?: number
  geofenceOk?: boolean
  notes?: string
  isManualCorrection: boolean
  createdAt: string
}

export interface TimeClockShiftDto {
  eventId: number
  eventParticipantId: number
  title: string
  branchId: number
  branchName: string
  date: string
  startTime?: string
  endTime?: string
}

export interface CurrentClockStatusDto {
  timeClockEnabled: boolean
  isClockedIn: boolean
  isOnBreak: boolean
  clockInAtUtc?: string
  breakStartAtUtc?: string
  currentShift?: TimeClockShiftDto
  workedMinutesToday: number
  statusMessage: string
  suggestedAction?: string
  requiresGeolocation: boolean
  showClockPrompt: boolean
  todayEntries: TimeEntryDto[]
}

export interface ShiftClockStatusDto {
  shift: TimeClockShiftDto
  isClockedIn: boolean
  isOnBreak: boolean
  isCompleted: boolean
  isExpired: boolean
  isActive: boolean
  clockInAtUtc?: string
  breakStartAtUtc?: string
  workedMinutes: number
  statusMessage: string
  suggestedAction?: string
  entries: TimeEntryDto[]
}

export interface TodayShiftsDto {
  timeClockEnabled: boolean
  requiresGeolocation: boolean
  activeEventParticipantId?: number
  shifts: ShiftClockStatusDto[]
}

export interface ClockActionResultDto {
  success: boolean
  message: string
  entry: TimeEntryDto
  status: CurrentClockStatusDto
  anomaly?: TimeClockAnomalyDto
  hasAnomaly: boolean
  workedShiftMinutes?: number
}

// ── Anomalie ─────────────────────────────────────────────────────────────

export enum TimeClockAnomalyType {
  LateClockIn = 1,
  EarlyClockIn = 2,
  LateClockOut = 3,
  EarlyClockOut = 4,
  MissingClockIn = 5,
  MissingClockOut = 6,
  ExtendedBreak = 7,
  LocationMismatch = 8,
  OvertimeDetected = 9,
}

const ANOMALY_TYPE_LABEL: Record<number, string> = {
  [TimeClockAnomalyType.LateClockIn]: 'Entrata in ritardo',
  [TimeClockAnomalyType.EarlyClockIn]: 'Entrata in anticipo',
  [TimeClockAnomalyType.LateClockOut]: 'Uscita in ritardo',
  [TimeClockAnomalyType.EarlyClockOut]: 'Uscita in anticipo',
  [TimeClockAnomalyType.MissingClockIn]: 'Entrata mancante',
  [TimeClockAnomalyType.MissingClockOut]: 'Uscita mancante',
  [TimeClockAnomalyType.ExtendedBreak]: 'Pausa prolungata',
  [TimeClockAnomalyType.LocationMismatch]: 'Fuori area filiale',
  [TimeClockAnomalyType.OvertimeDetected]: 'Straordinario rilevato',
}

/** Etichetta italiana per un tipo di anomalia; fallback al nome inglese dell'enum. */
export function anomalyTypeLabel(type: TimeClockAnomalyType, fallback?: string): string {
  return ANOMALY_TYPE_LABEL[type] ?? fallback ?? '—'
}

export enum TimeClockAnomalyReason {
  NotSpecified = 0,
  Traffic = 1,
  AuthorizedLeave = 2,
  TimeRecovery = 3,
  PersonalEmergency = 4,
  Forgotten = 5,
  TechnicalIssue = 6,
  SmartWorking = 7,
  Other = 8,
}

const ANOMALY_REASON_LABEL: Record<number, string> = {
  [TimeClockAnomalyReason.NotSpecified]: 'Non specificata',
  [TimeClockAnomalyReason.Traffic]: 'Traffico / imprevisto di viaggio',
  [TimeClockAnomalyReason.AuthorizedLeave]: 'Permesso autorizzato',
  [TimeClockAnomalyReason.TimeRecovery]: 'Recupero ore',
  [TimeClockAnomalyReason.PersonalEmergency]: 'Emergenza personale',
  [TimeClockAnomalyReason.Forgotten]: 'Dimenticanza della timbratura',
  [TimeClockAnomalyReason.TechnicalIssue]: 'Problema tecnico',
  [TimeClockAnomalyReason.SmartWorking]: 'Lavoro da remoto',
  [TimeClockAnomalyReason.Other]: 'Altro',
}

/** Etichetta italiana per una motivazione di giustificazione; fallback al nome inglese dell'enum. */
export function anomalyReasonLabel(reason?: TimeClockAnomalyReason, fallback?: string): string {
  if (reason == null) return fallback ?? '—'
  return ANOMALY_REASON_LABEL[reason] ?? fallback ?? '—'
}

export enum TimeClockAnomalyStatus {
  Open = 1,
  Justified = 2,
  Approved = 3,
  Rejected = 4,
}

export interface TimeClockAnomalyDto {
  id: number
  employeeId: number
  employeeName: string
  eventId?: number
  eventParticipantId?: number
  eventTitle?: string
  timeEntryId?: number
  type: TimeClockAnomalyType
  typeName: string
  status: TimeClockAnomalyStatus
  statusName: string
  severity: number
  workDate: string
  deviationMinutes?: number
  overtimeMinutes?: number
  employeeReason?: TimeClockAnomalyReason
  employeeReasonName?: string
  employeeNotes?: string
  justifiedAt?: string
  reviewNotes?: string
  reviewedAt?: string
  createdAt: string
}

export interface JustifyAnomalyRequest {
  reason: TimeClockAnomalyReason
  notes?: string
}

export interface WellbeingStatsDto {
  workedMinutesThisWeek: number
  workedMinutesThisMonth: number
  overtimeMinutesThisWeek: number
  overtimeMinutesThisMonth: number
  openAnomalies: number
  consecutiveWorkedDays: number
  hasWellbeingAlert: boolean
  wellbeingMessage?: string
}

/** Payload per le azioni di timbratura (clock-in/out, break). */
export interface ClockActionRequest {
  eventParticipantId?: number
  latitude?: number
  longitude?: number
  gpsAccuracyMeters?: number
  notes?: string
}

// ── Settings (usati dalla merchant-app) ─────────────────────────────────

export interface BranchTimeClockSettingsDto {
  branchId: number
  branchName: string
  isEnabled: boolean
  clockingRequired: boolean
  /** Data (YYYY-MM-DD) da cui vale l'obbligo. Valorizzata dal server, sola lettura. */
  clockingRequiredSince?: string | null
  graceInMinutes: number
  graceOutMinutes: number
  earlyClockInToleranceMinutes: number
  lateClockOutToleranceMinutes: number
  geofencingEnabled: boolean
  geofenceRadiusMeters: number
  breakTrackingEnabled: boolean
  maxBreakMinutes: number
  roundingMinutes: number
  requirePhoto: boolean
  branchLatitude?: number
  branchLongitude?: number
}

export interface UpdateTimeClockSettingsRequest {
  isEnabled: boolean
  clockingRequired: boolean
  graceInMinutes: number
  graceOutMinutes: number
  earlyClockInToleranceMinutes: number
  lateClockOutToleranceMinutes: number
  geofencingEnabled: boolean
  geofenceRadiusMeters: number
  breakTrackingEnabled: boolean
  maxBreakMinutes: number
  roundingMinutes: number
  requirePhoto: boolean
}
