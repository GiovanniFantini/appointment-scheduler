export interface ReportTimeEntry {
  employeeId: number
  employeeName: string
  branchId: number
  branchName: string
  workDate: string
  eventTitle: string
  clockInUtc: string | null
  clockOutUtc: string | null
  workedMinutes: number
  breakMinutes: number
  scheduledMinutes: number | null
  overtimeMinutes: number
  hasOpenAnomaly: boolean
}
export interface MerchantReport {
  from: string
  to: string
  shiftCount: number
  assignedShiftCount: number
  pendingRequests: number
  approvedRequests: number
  rejectedRequests: number
  days: { date: string; shiftCount: number; assignedShiftCount: number }[]
  timeEntries: ReportTimeEntry[]
}
