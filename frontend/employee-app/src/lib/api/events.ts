import apiClient from '../axios'

export interface EventParticipant {
  employeeId: number
  fullName: string
  isOwner: boolean
  skillId?: number | null
  skillName?: string | null
  skillColor?: string | null
}

export interface EventDto {
  id: number
  branchId: number
  branchName: string
  departmentId?: number | null
  departmentName?: string | null
  title: string
  eventTypeName: string
  startDate: string
  endDate?: string | null
  isAllDay: boolean
  startTime?: string | null
  endTime?: string | null
  isOnCall: boolean
  notes?: string | null
  participants: EventParticipant[]
}

export interface UpsertTurnoRequest {
  branchId: number
  departmentId?: number | null
  title: string
  startDate: string
  endDate?: string | null
  isAllDay: boolean
  startTime?: string | null
  endTime?: string | null
  isOnCall: boolean
  notes?: string | null
  ownerEmployeeIds: number[]
}

interface CloneWeekRequest {
  sourceWeekStart: string
  targetWeekStart: string
  numberOfWeeks: number
}

function toApiPayload(payload: UpsertTurnoRequest) {
  return {
    branchId: payload.branchId,
    departmentId: payload.departmentId ?? null,
    appliesToAllBranches: false,
    title: payload.title,
    eventType: 1, // Turno
    startDate: payload.startDate,
    endDate: payload.endDate ?? null,
    isAllDay: payload.isAllDay,
    startTime: payload.startTime ?? null,
    endTime: payload.endTime ?? null,
    isOnCall: payload.isOnCall,
    recurrence: null,
    notificationEnabled: false,
    notes: payload.notes ?? null,
    ownerEmployeeIds: payload.ownerEmployeeIds,
    coOwnerEmployeeIds: [],
    participantOverrides: [],
    requiredSkills: [],
    participantSkills: [],
  }
}

export const eventsApi = {
  async getById(id: number): Promise<EventDto> {
    const res = await apiClient.get<EventDto>(`/events/${id}`)
    return res.data
  },

  async createTurno(payload: UpsertTurnoRequest): Promise<EventDto> {
    const res = await apiClient.post<EventDto>('/events', toApiPayload(payload))
    return res.data
  },

  async updateTurno(id: number, payload: UpsertTurnoRequest): Promise<EventDto> {
    const res = await apiClient.put<EventDto>(`/events/${id}`, toApiPayload(payload))
    return res.data
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/events/${id}`)
  },

  async cloneWeek(payload: CloneWeekRequest): Promise<EventDto[]> {
    const res = await apiClient.post<EventDto[]>('/events/clone-week', payload)
    return res.data
  },
}
