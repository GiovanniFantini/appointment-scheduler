import apiClient from '../axios'

export interface BranchTimeClockSettings {
  branchId: number
  branchName: string
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
  branchLatitude?: number | null
  branchLongitude?: number | null
}

export type UpdateTimeClockSettingsRequest = Omit<
  BranchTimeClockSettings,
  'branchId' | 'branchName' | 'branchLatitude' | 'branchLongitude'
> & {
  /** Coordinate filiale per il geofence; null = lascia invariate. */
  branchLatitude?: number | null
  branchLongitude?: number | null
}

export interface TimeEntry {
  id: number
  employeeId: number
  employeeName: string
  branchId: number
  branchName: string
  eventId: number
  eventParticipantId: number
  eventTitle: string
  type: number
  typeName: string
  source: number
  status: number
  statusName: string
  workDate: string
  actualTimestampUtc: string
  expectedTime?: string | null
  deviationMinutes?: number | null
  latitude?: number | null
  longitude?: number | null
  gpsAccuracyMeters?: number | null
  distanceFromBranchMeters?: number | null
  geofenceOk?: boolean | null
  notes?: string | null
  isManualCorrection: boolean
  createdAt: string
}

export interface TimeClockAnomaly {
  id: number
  employeeId: number
  employeeName: string
  eventId?: number | null
  eventParticipantId?: number | null
  eventTitle?: string | null
  timeEntryId?: number | null
  type: number
  typeName: string
  status: number
  statusName: string
  severity: number
  workDate: string
  deviationMinutes?: number | null
  overtimeMinutes?: number | null
  employeeReason?: number | null
  employeeReasonName?: string | null
  employeeNotes?: string | null
  justifiedAt?: string | null
  reviewNotes?: string | null
  reviewedAt?: string | null
  createdAt: string
}

export interface TimeClockReportRow {
  employeeId: number
  employeeName: string
  branchId: number
  branchName: string
  workDate: string
  eventTitle: string
  clockInUtc?: string | null
  clockOutUtc?: string | null
  workedMinutes: number
  breakMinutes: number
  scheduledMinutes?: number | null
  overtimeMinutes: number
  hasOpenAnomaly: boolean
}

/** Client API per la timbratura lato merchant. */
export const timeClockApi = {
  async getSettings(branchId: number): Promise<BranchTimeClockSettings> {
    const res = await apiClient.get<BranchTimeClockSettings>('/merchant/time-clock/settings', {
      params: { branchId },
    })
    return res.data
  },

  async updateSettings(
    branchId: number,
    payload: UpdateTimeClockSettingsRequest,
  ): Promise<BranchTimeClockSettings> {
    const res = await apiClient.put<BranchTimeClockSettings>(
      `/merchant/time-clock/settings/${branchId}`,
      payload,
    )
    return res.data
  },

  async getEntries(params: {
    branchId?: number
    from?: string
    to?: string
    employeeId?: number
  }): Promise<TimeEntry[]> {
    const res = await apiClient.get<TimeEntry[]>('/merchant/time-clock/entries', { params })
    return res.data
  },

  async getAnomalies(params: { branchId?: number; status?: number }): Promise<TimeClockAnomaly[]> {
    const res = await apiClient.get<TimeClockAnomaly[]>('/merchant/time-clock/anomalies', { params })
    return res.data
  },

  async approveAnomaly(id: number, reviewNotes?: string): Promise<TimeClockAnomaly> {
    const res = await apiClient.post<TimeClockAnomaly>(
      `/merchant/time-clock/anomalies/${id}/approve`, { reviewNotes },
    )
    return res.data
  },

  async rejectAnomaly(id: number, reviewNotes?: string): Promise<TimeClockAnomaly> {
    const res = await apiClient.post<TimeClockAnomaly>(
      `/merchant/time-clock/anomalies/${id}/reject`, { reviewNotes },
    )
    return res.data
  },

  async runDetection(branchId?: number): Promise<{ created: number }> {
    const res = await apiClient.post<{ created: number }>(
      '/merchant/time-clock/run-detection', null, { params: { branchId } },
    )
    return res.data
  },

  async getReport(params: {
    branchId?: number
    from?: string
    to?: string
  }): Promise<TimeClockReportRow[]> {
    const res = await apiClient.get<TimeClockReportRow[]>('/merchant/time-clock/report', { params })
    return res.data
  },
}
