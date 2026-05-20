import { useState, useCallback, useRef, useEffect, useMemo } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import itLocale from '@fullcalendar/core/locales/it'
import type { EventClickArg, DatesSetArg, EventInput, EventDropArg } from '@fullcalendar/core'
import type { DateClickArg, EventResizeDoneArg } from '@fullcalendar/interaction'
import apiClient from '../../lib/axios'
import EventModal from '../../components/EventModal/EventModal'
import type { CalEvent, EventType } from '../../components/EventModal/EventModal'
import CopyWeekDialog from '../../components/CopyWeekDialog/CopyWeekDialog'
import { skillsApi, type Skill } from '../../lib/api/skills'
import { useBranch } from '../../contexts/BranchContext'
import BranchSelector from '../../components/shared/BranchSelector'
import { MerchantUser } from '../../App'
import './CalendarioPage.css'

interface EmployeeOption {
  id: number
  firstName: string
  lastName: string
}

interface CalendarioPageProps {
  user: MerchantUser
}

interface ApiEventParticipant {
  employeeId: number
  fullName: string
  isOwner: boolean
  startTimeOverride?: string
  endTimeOverride?: string
  participantNotes?: string
  skillId?: number | null
  skillName?: string | null
  skillColor?: string | null
  departmentId?: number | null
  departmentName?: string | null
  departmentColor?: string | null
}

interface ApiRequiredSkill {
  skillId: number
  skillName: string
  skillColor: string
  quantity: number
  coveredQuantity: number
}

interface ApiEvent {
  id: number
  merchantId: number
  branchId: number
  branchName?: string
  departmentId?: number | null
  departmentName?: string | null
  departmentColor?: string | null
  appliesToAllBranches?: boolean
  title: string
  eventType: number        // numeric enum value from server
  eventTypeName: string    // "Turno" | "ChiusuraAziendale" | etc.
  startDate: string        // "2024-01-15"
  endDate?: string
  isAllDay: boolean
  startTime?: string       // "09:00:00"
  endTime?: string
  isOnCall: boolean
  recurrence?: string
  notificationEnabled: boolean
  notes?: string
  createdAt: string
  participants: ApiEventParticipant[]
  requiredSkills?: ApiRequiredSkill[]
  coverageStatus?: number
  coverageStatusName?: 'None' | 'Empty' | 'Partial' | 'Covered'
}

interface ApiEmployeeRequest {
  id: number
  employeeId: number
  employeeFullName: string
  typeName: string   // "Ferie" | "CambioTurno" | "Permessi" | "Malattia"
  statusName: string // "Pending" | "Approved" | "Rejected"
  startDate: string
  endDate?: string
  startTime?: string
  endTime?: string
  eventId?: number | null
  notes?: string
}

const EVENT_COLORS: Record<string, string> = {
  Turno: '#3b82f6',
  ChiusuraAziendale: '#1f2937',
  Ferie: '#ec4899',
  Permessi: '#8b5cf6',
  Malattia: '#f59e0b',
  CambioTurno: '#0ea5e9',
}

const LEGEND_ITEMS = [
  { label: 'Turno', color: EVENT_COLORS.Turno },
  { label: 'ChiusuraAziendale', color: EVENT_COLORS.ChiusuraAziendale },
  { label: 'Ferie', color: EVENT_COLORS.Ferie },
  { label: 'Permessi', color: EVENT_COLORS.Permessi },
  { label: 'Malattia', color: EVENT_COLORS.Malattia },
]

function apiEventToCalEvent(e: ApiEvent): Partial<CalEvent> {
  return {
    id: e.id,
    title: e.title,
    eventType: e.eventTypeName as EventType,
    branchId: e.branchId,
    departmentId: e.departmentId ?? null,
    appliesToAllBranches: e.appliesToAllBranches ?? false,
    isAllDay: e.isAllDay,
    startDate: e.startDate,
    endDate: e.endDate,
    startTime: e.startTime ? e.startTime.slice(0, 5) : undefined,
    endTime: e.endTime ? e.endTime.slice(0, 5) : undefined,
    isOnCall: e.isOnCall,
    ownerEmployeeIds: e.participants.filter(p => p.isOwner).map(p => p.employeeId),
    coOwnerEmployeeIds: e.participants.filter(p => !p.isOwner).map(p => p.employeeId),
    participantOverrides: e.participants
      .filter(p => p.startTimeOverride || p.endTimeOverride || p.participantNotes || p.departmentId != null)
      .map(p => ({
        employeeId: p.employeeId,
        startTimeOverride: p.startTimeOverride ? p.startTimeOverride.slice(0, 5) : undefined,
        endTimeOverride: p.endTimeOverride ? p.endTimeOverride.slice(0, 5) : undefined,
        participantNotes: p.participantNotes ?? undefined,
        departmentId: p.departmentId ?? null,
      })),
    participantSkills: e.participants
      .filter(p => p.skillId != null)
      .map(p => ({ employeeId: p.employeeId, skillId: p.skillId ?? null })),
    requiredSkills: (e.requiredSkills ?? []).map(rs => ({ skillId: rs.skillId, quantity: rs.quantity })),
    recurrence: (e.recurrence ?? 'Nessuna') as CalEvent['recurrence'],
    notificationEnabled: e.notificationEnabled,
    notes: e.notes ?? undefined,
  }
}

function apiEventToUpdatePayload(e: ApiEvent) {
  return {
    title: e.title,
    eventType: e.eventType,
    branchId: e.branchId,
    departmentId: e.departmentId ?? null,
    appliesToAllBranches: e.appliesToAllBranches ?? false,
    startDate: e.startDate,
    endDate: e.endDate ?? e.startDate,
    isAllDay: e.isAllDay,
    startTime: e.startTime,
    endTime: e.endTime,
    isOnCall: e.isOnCall,
    recurrence: e.recurrence,
    notificationEnabled: e.notificationEnabled,
    notes: e.notes,
    ownerEmployeeIds: e.participants.filter(p => p.isOwner).map(p => p.employeeId),
    coOwnerEmployeeIds: e.participants.filter(p => !p.isOwner).map(p => p.employeeId),
    participantOverrides: e.participants
      .filter(p => p.startTimeOverride || p.endTimeOverride || p.participantNotes || p.departmentId != null)
      .map(p => ({
        employeeId: p.employeeId,
        startTimeOverride: p.startTimeOverride,
        endTimeOverride: p.endTimeOverride,
        participantNotes: p.participantNotes,
        departmentId: p.departmentId ?? null,
      })),
    requiredSkills: (e.requiredSkills ?? []).map(rs => ({ skillId: rs.skillId, quantity: rs.quantity })),
    participantSkills: e.participants
      .filter(p => p.skillId != null)
      .map(p => ({ employeeId: p.employeeId, skillId: p.skillId })),
  }
}

function coverageDotColor(name?: string): string | null {
  if (!name) return null
  switch (name) {
    case 'Covered': return '#10b981'
    case 'Partial': return '#f59e0b'
    case 'Empty': return '#94a3b8'
    default: return null
  }
}

function requestToFCEvent(r: ApiEmployeeRequest): EventInput {
  const color = EVENT_COLORS[r.typeName] ?? '#6366f1'
  const isPending = r.statusName === 'Pending'
  const hasHourly = !!(r.startTime && r.endTime)

  // Hourly requests render as timed events on the specific date; full-day as all-day spans.
  if (hasHourly) {
    const start = `${r.startDate}T${r.startTime}`
    const end = `${r.startDate}T${r.endTime}`
    const hoursLabel = `${r.startTime!.slice(0, 5)}-${r.endTime!.slice(0, 5)}`
    return {
      id: `req-${r.id}`,
      title: `${r.typeName} ${hoursLabel} – ${r.employeeFullName}${isPending ? ' (in attesa)' : ''}`,
      start,
      end,
      allDay: false,
      backgroundColor: isPending ? 'transparent' : color,
      borderColor: color,
      textColor: isPending ? color : '#fff',
      classNames: isPending ? ['request-pending'] : ['request-approved'],
      extendedProps: { apiRequest: r },
    }
  }

  // FullCalendar all-day end dates are exclusive, so add 1 day to endDate
  const endExclusive = (() => {
    const base = r.endDate ?? r.startDate
    const d = new Date(base + 'T00:00:00')
    d.setDate(d.getDate() + 1)
    return d.toISOString().split('T')[0]
  })()

  return {
    id: `req-${r.id}`,
    title: `${r.typeName} – ${r.employeeFullName}${isPending ? ' (in attesa)' : ''}`,
    start: r.startDate,
    end: endExclusive,
    allDay: true,
    backgroundColor: isPending ? 'transparent' : color,
    borderColor: color,
    textColor: isPending ? color : '#fff',
    classNames: isPending ? ['request-pending'] : ['request-approved'],
    extendedProps: { apiRequest: r },
  }
}

function toFCEvent(e: ApiEvent, showBranchBadge: boolean): EventInput {
  // Per i Turni con mansione richiesta, usa il colore della mansione "principale"
  // (la prima della lista) come pillola — UX stile Google Calendar.
  let color = EVENT_COLORS[e.eventTypeName] ?? '#6366f1'
  if (e.eventTypeName === 'Turno' && e.requiredSkills && e.requiredSkills.length > 0) {
    color = e.requiredSkills[0].skillColor || color
  }
  const textColor = e.eventTypeName === 'ChiusuraAziendale' ? '#fff' : undefined

  let start: string
  let end: string | undefined

  if (e.isAllDay) {
    start = e.startDate
    end = e.endDate
  } else {
    start = e.startDate + (e.startTime ? `T${e.startTime}` : '')
    end = (e.endDate ?? e.startDate) + (e.endTime ? `T${e.endTime}` : '')
  }

  // In vista "Tutte le filiali" prefissa il nome della filiale per dare contesto.
  const prefix = showBranchBadge && e.branchName ? `[${e.branchName}] ` : ''
  const deptSuffix = e.departmentName ? ` · ${e.departmentName}` : ''

  return {
    id: String(e.id),
    title: `${prefix}${e.title}${deptSuffix}`,
    start,
    end,
    allDay: e.isAllDay,
    backgroundColor: color,
    borderColor: color,
    textColor,
    classNames: e.isOnCall ? ['on-call'] : [],
    extendedProps: { apiEvent: e },
  }
}

export default function CalendarioPage({ user: _user }: CalendarioPageProps) {
  const calendarRef = useRef<FullCalendar>(null)
  const { activeBranchId, activeDepartmentId, isMultiBranch } = useBranch()
  const [events, setEvents] = useState<EventInput[]>([])
  const [selectedEvent, setSelectedEvent] = useState<Partial<CalEvent> | null>(null)
  const [defaultDate, setDefaultDate] = useState<string>('')
  const [modalOpen, setModalOpen] = useState(false)
  const [copyDialogOpen, setCopyDialogOpen] = useState(false)
  const [currentView, setCurrentView] = useState<string>('dayGridMonth')
  const [skills, setSkills] = useState<Skill[]>([])
  const [employees, setEmployees] = useState<EmployeeOption[]>([])
  const [skillFilter, setSkillFilter] = useState<Set<number>>(new Set())
  const [employeeFilter, setEmployeeFilter] = useState<number | ''>('')
  const [hoverInfo, setHoverInfo] = useState<{ apiEvent: ApiEvent; left: number; top: number } | null>(null)

  useEffect(() => {
    let mounted = true
    skillsApi.list().then(list => {
      if (mounted) setSkills(list.filter(s => s.isActive))
    }).catch(() => { /* feature opzionale */ })
    apiClient.get<EmployeeOption[]>('/employees').then(res => {
      if (mounted) setEmployees(res.data ?? [])
    }).catch(() => { /* ignore */ })
    return () => { mounted = false }
  }, [])

  const fetchEvents = useCallback(async (from: string, to: string) => {
    try {
      const params = new URLSearchParams({ from, to })
      if (activeBranchId != null) params.set('branchId', String(activeBranchId))
      if (activeDepartmentId != null) params.set('departmentId', String(activeDepartmentId))
      const [eventsRes, requestsRes] = await Promise.all([
        apiClient.get(`/events?${params.toString()}`),
        apiClient.get(`/employee-requests`),
      ])

      // Badge filiale solo quando si vedono più filiali insieme (vista "Tutte").
      const showBranchBadge = isMultiBranch && activeBranchId == null
      const eventItems: EventInput[] = Array.isArray(eventsRes.data)
        ? (eventsRes.data as ApiEvent[]).map(e => toFCEvent(e, showBranchBadge))
        : []

      // Filter out rejected and those outside the current range, then convert
      const requestItems: EventInput[] = Array.isArray(requestsRes.data)
        ? (requestsRes.data as ApiEmployeeRequest[])
            .filter(r => r.statusName !== 'Rejected')
            .filter(r => {
              const start = r.startDate
              const end = r.endDate ?? r.startDate
              return end >= from && start <= to
            })
            .map(requestToFCEvent)
        : []

      setEvents([...eventItems, ...requestItems])
    } catch {
      // silently fail
    }
  }, [activeBranchId, activeDepartmentId, isMultiBranch])

  const handleDatesSet = (info: DatesSetArg) => {
    const from = info.startStr.split('T')[0]
    const to = info.endStr.split('T')[0]
    setCurrentView(info.view.type)
    fetchEvents(from, to)
  }

  const refreshCurrentRange = () => {
    const api = calendarRef.current?.getApi()
    if (!api) return
    const from = api.view.activeStart.toISOString().split('T')[0]
    const toExclusive = new Date(api.view.activeEnd)
    toExclusive.setDate(toExclusive.getDate() - 1)
    const to = toExclusive.toISOString().split('T')[0]
    fetchEvents(from, to)
  }

  const handleEventDrop = async (info: EventDropArg) => {
    setHoverInfo(null)
    const apiEvent = info.event.extendedProps.apiEvent as ApiEvent | undefined
    if (!apiEvent) { info.revert(); return }
    if (info.event.allDay !== apiEvent.isAllDay) {
      // Cambio modalità allDay non supportato via drag — revert
      info.revert()
      return
    }
    const newStartDate = info.event.start?.toISOString().split('T')[0] ?? apiEvent.startDate
    let newStartTime: string | undefined = undefined
    let newEndTime: string | undefined = undefined
    if (!apiEvent.isAllDay && info.event.start && info.event.end) {
      const pad = (n: number) => String(n).padStart(2, '0')
      const s = info.event.start
      const e = info.event.end
      newStartTime = `${pad(s.getHours())}:${pad(s.getMinutes())}:00`
      newEndTime = `${pad(e.getHours())}:${pad(e.getMinutes())}:00`
    }
    try {
      await apiClient.put(`/events/${apiEvent.id}`, {
        ...apiEventToUpdatePayload(apiEvent),
        startDate: newStartDate,
        endDate: newStartDate,
        startTime: newStartTime,
        endTime: newEndTime,
      })
      refreshCurrentRange()
    } catch {
      info.revert()
    }
  }

  const handleEventResize = async (info: EventResizeDoneArg) => {
    setHoverInfo(null)
    const apiEvent = info.event.extendedProps.apiEvent as ApiEvent | undefined
    if (!apiEvent || apiEvent.isAllDay) { info.revert(); return }
    if (!info.event.start || !info.event.end) { info.revert(); return }
    const pad = (n: number) => String(n).padStart(2, '0')
    const s = info.event.start
    const e = info.event.end
    const newStartTime = `${pad(s.getHours())}:${pad(s.getMinutes())}:00`
    const newEndTime = `${pad(e.getHours())}:${pad(e.getMinutes())}:00`
    try {
      await apiClient.put(`/events/${apiEvent.id}`, {
        ...apiEventToUpdatePayload(apiEvent),
        startTime: newStartTime,
        endTime: newEndTime,
      })
      refreshCurrentRange()
    } catch {
      info.revert()
    }
  }

  const handleEventClick = (info: EventClickArg) => {
    setHoverInfo(null)
    // Requests are read-only in the calendar view: navigate to Richieste page instead
    if (info.event.extendedProps.apiRequest) {
      window.location.href = '/richieste'
      return
    }
    const apiEvent = info.event.extendedProps.apiEvent as ApiEvent
    setSelectedEvent(apiEventToCalEvent(apiEvent))
    setDefaultDate('')
    setModalOpen(true)
  }

  const handleDateClick = (info: DateClickArg) => {
    setHoverInfo(null)
    setSelectedEvent(null)
    setDefaultDate(info.dateStr.split('T')[0])
    setModalOpen(true)
  }

  const handleNewEvent = () => {
    setHoverInfo(null)
    setSelectedEvent(null)
    setDefaultDate(new Date().toISOString().split('T')[0])
    setModalOpen(true)
  }

  const handleModalClose = () => {
    setModalOpen(false)
    setSelectedEvent(null)
  }

  const handleSaved = () => {
    setModalOpen(false)
    setSelectedEvent(null)

    // Refresh the exact range currently visible in the calendar
    // so newly created events always appear immediately.
    const api = calendarRef.current?.getApi()
    if (api) {
      const from = api.view.activeStart.toISOString().split('T')[0]
      const toExclusive = new Date(api.view.activeEnd)
      toExclusive.setDate(toExclusive.getDate() - 1)
      const to = toExclusive.toISOString().split('T')[0]
      fetchEvents(from, to)
      return
    }

    // Fallback if calendar API is not available yet.
    const now = new Date()
    const from = now.toISOString().split('T')[0]
    const futureDate = new Date(now)
    futureDate.setDate(now.getDate() + 60)
    fetchEvents(from, futureDate.toISOString().split('T')[0])
  }

  // Filtri:
  // - skillFilter: si applica solo ai Turni che dichiarano requiredSkills. I turni senza
  //   mansioni richieste restano sempre visibili (logica skip-friendly).
  // - employeeFilter: si applica sia ai turni sia alle request (ferie/permessi).
  // - Le request non hanno mansioni, quindi non sono mai filtrate da skillFilter.
  const filteredEvents = useMemo(() => {
    if (skillFilter.size === 0 && employeeFilter === '') return events
    return events.filter(ev => {
      const apiEvent = ev.extendedProps?.apiEvent as ApiEvent | undefined
      const apiRequest = ev.extendedProps?.apiRequest as ApiEmployeeRequest | undefined

      if (apiRequest) {
        if (employeeFilter !== '' && apiRequest.employeeId !== employeeFilter) return false
        return true
      }
      if (!apiEvent) return true

      if (skillFilter.size > 0) {
        const evSkills = apiEvent.requiredSkills?.map(rs => rs.skillId) ?? []
        // Turni senza mansioni richieste: restano sempre visibili (non li nascondiamo
        // solo perché stai filtrando per mansione — sarebbero "scomparsi" inaspettatamente).
        if (evSkills.length > 0 && !evSkills.some(s => skillFilter.has(s))) return false
      }
      if (employeeFilter !== '') {
        if (!apiEvent.participants.some(p => p.employeeId === employeeFilter)) return false
      }
      return true
    })
  }, [events, skillFilter, employeeFilter])

  const toggleSkillFilter = (id: number) => {
    setSkillFilter(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }

  // Quando cambia la filiale/reparto attivi, ricarica il range visibile.
  useEffect(() => {
    const api = calendarRef.current?.getApi()
    if (!api) return
    const from = api.view.activeStart.toISOString().split('T')[0]
    const toExclusive = new Date(api.view.activeEnd)
    toExclusive.setDate(toExclusive.getDate() - 1)
    fetchEvents(from, toExclusive.toISOString().split('T')[0])
  }, [fetchEvents])

  // Lunedì della settimana corrente in vista, usato dal CopyWeekDialog
  const currentWeekStart = (() => {
    const api = calendarRef.current?.getApi()
    if (!api) return ''
    const d = new Date(api.view.activeStart)
    const dow = d.getDay() === 0 ? 7 : d.getDay()
    d.setDate(d.getDate() - (dow - 1))
    return d.toISOString().split('T')[0]
  })()

  return (
    <div className="calendario-page">
      <div className="calendario-header">
        <h1 className="calendario-title">Calendario</h1>
        <div className="calendario-actions">
          {isMultiBranch && <BranchSelector />}
          {currentView === 'timeGridWeek' && (
            <button className="btn-secondary" onClick={() => setCopyDialogOpen(true)}>
              📋 Copia settimana scorsa
            </button>
          )}
          <button className="btn-new-event" onClick={handleNewEvent}>
            + Nuovo Turno
          </button>
        </div>
      </div>

      <div className="legend">
        {LEGEND_ITEMS.map(item => {
          // Quando il merchant ha configurato delle mansioni, i Turni con fabbisogno
          // mostrano il colore della mansione: chiariamolo nella legenda.
          if (item.label === 'Turno' && skills.length > 0) {
            return (
              <div key={item.label} className="legend-item">
                <span className="legend-dot" style={{ background: item.color }} />
                Turno (senza mansione)
              </div>
            )
          }
          return (
            <div key={item.label} className="legend-item">
              <span className="legend-dot" style={{ background: item.color }} />
              {item.label}
            </div>
          )
        })}
        <div className="legend-item">
          <span className="legend-dot" style={{ background: '#3b82f6', border: '2px dashed rgba(255,255,255,0.5)' }} />
          Turno in reperibilità
        </div>
        <div className="legend-item">
          <span className="legend-dot" style={{ background: 'transparent', border: '2px dashed #ec4899' }} />
          Richiesta in attesa
        </div>
        <div className="legend-item">
          <span className="legend-dot" style={{ background: '#ec4899' }} />
          Richiesta approvata
        </div>
      </div>

      {(skills.length > 0 || employees.length > 0) && (
        <div className="calendar-filters">
          {skills.length > 0 && (
            <div className="calendar-filters-group">
              <span className="calendar-filters-label">Mansioni:</span>
              <div className="calendar-filters-chips">
                {skills.map(s => {
                  const active = skillFilter.has(s.id)
                  return (
                    <button
                      key={s.id}
                      type="button"
                      className={`skill-filter-chip ${active ? 'active' : ''}`}
                      onClick={() => toggleSkillFilter(s.id)}
                      style={active ? { background: s.color, borderColor: s.color, color: '#fff' } : { borderColor: s.color, color: s.color }}
                      title={`Mostra solo turni con ${s.name}`}
                    >
                      {s.name}
                    </button>
                  )
                })}
                {skillFilter.size > 0 && (
                  <button
                    type="button"
                    className="skill-filter-clear"
                    onClick={() => setSkillFilter(new Set())}
                    title="Rimuovi filtro mansioni"
                  >
                    ✕
                  </button>
                )}
              </div>
            </div>
          )}
          {employees.length > 0 && (
            <div className="calendar-filters-group">
              <span className="calendar-filters-label">Dipendente:</span>
              <select
                className="calendar-filter-select"
                value={employeeFilter === '' ? '' : String(employeeFilter)}
                onChange={(e) => setEmployeeFilter(e.target.value === '' ? '' : Number(e.target.value))}
              >
                <option value="">Tutti</option>
                {employees.map(emp => (
                  <option key={emp.id} value={emp.id}>{emp.firstName} {emp.lastName}</option>
                ))}
              </select>
            </div>
          )}
        </div>
      )}

      <div className="calendar-wrapper">
        <FullCalendar
          ref={calendarRef}
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView="dayGridMonth"
          locale={itLocale}
          headerToolbar={{
            left: 'prev,next today',
            center: 'title',
            right: 'timeGridDay,timeGridWeek,dayGridMonth',
          }}
          buttonText={{
            today: 'Oggi',
            day: 'Giorno',
            week: 'Settimana',
            month: 'Mese'
          }}
          events={filteredEvents}
          eventClick={handleEventClick}
          dateClick={handleDateClick}
          datesSet={handleDatesSet}
          eventDrop={handleEventDrop}
          eventResize={handleEventResize}
          eventMouseEnter={(arg) => {
            // Niente popover quando il modal evento o il dialog copia settimana sono aperti
            if (modalOpen || copyDialogOpen) return
            const apiEvent = arg.event.extendedProps.apiEvent as ApiEvent | undefined
            if (!apiEvent || apiEvent.eventTypeName !== 'Turno') return
            const rect = (arg.el as HTMLElement).getBoundingClientRect()
            const popoverWidth = 300
            const popoverHeightApprox = 220
            // Clamp orizzontale: mantieni il popover dentro la viewport
            let left = rect.left + window.scrollX + rect.width / 2
            const minLeft = window.scrollX + popoverWidth / 2 + 8
            const maxLeft = window.scrollX + window.innerWidth - popoverWidth / 2 - 8
            if (left < minLeft) left = minLeft
            if (left > maxLeft) left = maxLeft
            // Clamp verticale: se non c'è spazio sotto, mostra sopra
            let top = rect.bottom + window.scrollY + 6
            if (rect.bottom + popoverHeightApprox + 6 > window.innerHeight) {
              top = rect.top + window.scrollY - popoverHeightApprox - 6
              if (top < window.scrollY + 8) top = window.scrollY + 8
            }
            setHoverInfo({ apiEvent, left, top })
          }}
          eventMouseLeave={() => setHoverInfo(null)}
          height="100%"
          editable={true}
          eventStartEditable={true}
          eventDurationEditable={true}
          selectable={true}
          nowIndicator={true}
          eventDidMount={(arg) => {
            const apiEvent = arg.event.extendedProps.apiEvent as ApiEvent | undefined
            if (!apiEvent) return
            const dot = coverageDotColor(apiEvent.coverageStatusName)
            if (dot && apiEvent.eventTypeName === 'Turno') {
              const span = document.createElement('span')
              span.className = 'fc-coverage-dot'
              span.style.background = dot
              const label =
                apiEvent.coverageStatusName === 'Covered' ? 'Completa' :
                apiEvent.coverageStatusName === 'Partial' ? 'Parziale' :
                apiEvent.coverageStatusName === 'Empty' ? 'Non coperta' :
                apiEvent.coverageStatusName
              span.title = `Copertura: ${label}`
              arg.el.appendChild(span)
            }
            if (apiEvent.recurrence?.startsWith('WEEKLY')) {
              const icon = document.createElement('span')
              icon.className = 'fc-recurring-icon'
              icon.textContent = '🔁'
              icon.title = 'Turno ricorrente'
              arg.el.appendChild(icon)
            }
          }}
        />
      </div>

      {modalOpen && (
        <EventModal
          event={selectedEvent}
          defaultDate={defaultDate}
          onClose={handleModalClose}
          onSaved={handleSaved}
        />
      )}

      {copyDialogOpen && (
        <CopyWeekDialog
          currentWeekStart={currentWeekStart}
          onClose={() => setCopyDialogOpen(false)}
          onCopied={() => {
            setCopyDialogOpen(false)
            refreshCurrentRange()
          }}
        />
      )}

      {hoverInfo && (
        <div
          className="event-hover-popover"
          style={{ left: hoverInfo.left, top: hoverInfo.top }}
          role="tooltip"
        >
          <div className="event-hover-title">{hoverInfo.apiEvent.title || 'Turno'}</div>
          <div className="event-hover-meta">
            {(() => {
              const ev = hoverInfo.apiEvent
              if (ev.isAllDay) return 'Tutto il giorno'
              if (ev.startTime && ev.endTime) return `${ev.startTime.slice(0,5)} – ${ev.endTime.slice(0,5)}`
              return null
            })()}
          </div>
          {hoverInfo.apiEvent.requiredSkills && hoverInfo.apiEvent.requiredSkills.length > 0 && (
            <div className="event-hover-section">
              <div className="event-hover-section-title">Mansioni richieste</div>
              <div className="event-hover-chips">
                {hoverInfo.apiEvent.requiredSkills.map(rs => {
                  const covered = rs.coveredQuantity >= rs.quantity
                  return (
                    <span
                      key={rs.skillId}
                      className="event-hover-chip"
                      style={{ background: rs.skillColor, color: '#fff' }}
                    >
                      {rs.skillName} {rs.coveredQuantity}/{rs.quantity} {covered ? '✓' : '⚠'}
                    </span>
                  )
                })}
              </div>
            </div>
          )}
          {hoverInfo.apiEvent.participants.length > 0 && (
            <div className="event-hover-section">
              <div className="event-hover-section-title">Partecipanti</div>
              <ul className="event-hover-participants">
                {hoverInfo.apiEvent.participants.map(p => (
                  <li key={p.employeeId}>
                    {p.fullName}
                    {p.skillName && (
                      <span
                        className="event-hover-chip-inline"
                        style={{ background: p.skillColor ?? '#94a3b8' }}
                      >
                        {p.skillName}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {hoverInfo.apiEvent.coverageStatusName && hoverInfo.apiEvent.coverageStatusName !== 'None' && (
            <div className="event-hover-coverage">
              <span
                className="event-hover-coverage-dot"
                style={{ background: coverageDotColor(hoverInfo.apiEvent.coverageStatusName) ?? '#94a3b8' }}
              />
              Copertura: {(() => {
                switch (hoverInfo.apiEvent.coverageStatusName) {
                  case 'Covered': return 'Completa'
                  case 'Partial': return 'Parziale'
                  case 'Empty': return 'Non coperta'
                  default: return hoverInfo.apiEvent.coverageStatusName
                }
              })()}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
