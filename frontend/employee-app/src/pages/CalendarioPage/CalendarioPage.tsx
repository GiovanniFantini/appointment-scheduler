import { useState, useRef } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import itLocale from '@fullcalendar/core/locales/it'
import { EventClickArg, EventInput } from '@fullcalendar/core'
import apiClient from '../../lib/axios'
import EventDetailModal, { EventDetail } from '../../components/EventDetailModal/EventDetailModal'
import './CalendarioPage.css'

// Matches EventDto from server
interface ApiEvent {
  id: number
  title: string
  eventTypeName: string    // "Turno" | "Ferie" | "Permessi" | "Malattia" | "ChiusuraAziendale"
  startDate: string        // "2024-01-15"
  endDate?: string
  isAllDay: boolean
  startTime?: string       // "09:00:00"
  endTime?: string
  isOnCall: boolean
  notes?: string
  participants: Array<{ employeeId: number; fullName: string; isOwner: boolean }>
}

// Matches EmployeeRequestDto from server.
interface ApiEmployeeRequest {
  id: number
  typeName: string   // "Ferie" | "Permessi" | "Malattia"
  statusName: string // "Pending" | "Approved" | "Rejected"
  startDate: string
  endDate?: string
  startTime?: string
  endTime?: string
  eventId?: number | null
  notes?: string
}

// Matches EffectiveShiftDto from server: a shift with leaves already subtracted into segments.
interface ApiEffectiveShift {
  eventId: number
  employeeId: number
  title: string
  date: string
  isAllDay: boolean
  canonicalStart?: string
  canonicalEnd?: string
  segments: Array<{ from?: string; to?: string }>
  appliedLeaves: Array<{
    requestId: number
    typeName: string
    isFullDay: boolean
    startTime?: string
    endTime?: string
    notes?: string
  }>
  isFullyAbsent: boolean
  isOnCall: boolean
}

function getEventColor(eventTypeName?: string): string {
  const map: Record<string, string> = {
    Turno: '#3b82f6',
    Ferie: '#ec4899',
    Permessi: '#8b5cf6',
    Malattia: '#f59e0b',
    ChiusuraAziendale: '#1f2937',
  }
  return eventTypeName ? (map[eventTypeName] ?? '#6366f1') : '#6366f1'
}

function apiEventToFCEvent(e: ApiEvent): EventInput {
  const color = getEventColor(e.eventTypeName)

  let start: string
  let end: string | undefined

  if (e.isAllDay) {
    start = e.startDate
    end = e.endDate
  } else {
    start = e.startDate + (e.startTime ? `T${e.startTime}` : '')
    end = (e.endDate ?? e.startDate) + (e.endTime ? `T${e.endTime}` : '')
  }

  return {
    id: String(e.id),
    title: e.title,
    start,
    end,
    allDay: e.isAllDay,
    backgroundColor: color,
    borderColor: color,
    extendedProps: {
      eventTypeName: e.eventTypeName,
      isOnCall: e.isOnCall,
      participants: e.participants.map(p => ({ id: p.employeeId, name: p.fullName })),
      notes: e.notes,
    },
  }
}

/**
 * Converts an effective-shift into one or more FullCalendar events: one per presence segment.
 * If the employee is fully absent, renders a greyed-out background event to preserve shift visibility.
 */
function effectiveShiftToFCEvents(s: ApiEffectiveShift): EventInput[] {
  const baseColor = '#3b82f6' // Turno
  const hasLeaves = s.appliedLeaves.length > 0

  if (s.isFullyAbsent) {
    return [{
      id: `shift-${s.eventId}-absent`,
      title: `${s.title} (assente)`,
      start: s.date,
      allDay: true,
      backgroundColor: '#e5e7eb',
      borderColor: '#9ca3af',
      textColor: '#6b7280',
      extendedProps: {
        eventTypeName: 'Turno',
        eventId: s.eventId,
        isOnCall: s.isOnCall,
        isEffective: true,
        appliedLeaves: s.appliedLeaves,
      },
    }]
  }

  if (s.isAllDay || s.segments.length === 1 && !s.segments[0].from) {
    return [{
      id: `shift-${s.eventId}`,
      title: s.title,
      start: s.date,
      allDay: true,
      backgroundColor: baseColor,
      borderColor: baseColor,
      extendedProps: {
        eventTypeName: 'Turno',
        eventId: s.eventId,
        isOnCall: s.isOnCall,
        isEffective: true,
        appliedLeaves: s.appliedLeaves,
      },
    }]
  }

  return s.segments.map((seg, idx) => {
    const start = seg.from ? `${s.date}T${seg.from}` : s.date
    const end = seg.to ? `${s.date}T${seg.to}` : undefined
    const title = hasLeaves && s.segments.length > 1
      ? `${s.title} (parte ${idx + 1}/${s.segments.length})`
      : s.title
    return {
      id: `shift-${s.eventId}-seg-${idx}`,
      title,
      start,
      end,
      allDay: false,
      backgroundColor: baseColor,
      borderColor: baseColor,
      extendedProps: {
        eventTypeName: 'Turno',
        eventId: s.eventId,
        isOnCall: s.isOnCall,
        isEffective: true,
        appliedLeaves: s.appliedLeaves,
      },
    }
  })
}

/**
 * Converts an employee request (Ferie/Permessi/Malattia) into a FullCalendar event.
 * Approved requests render solid; pending requests render as a dashed outline.
 * Rejected requests are filtered out before this function is called.
 */
function requestToFCEvent(r: ApiEmployeeRequest): EventInput {
  const color = getEventColor(r.typeName)
  const isPending = r.statusName === 'Pending'
  const hasHourly = !!(r.startTime && r.endTime)
  const statusSuffix = isPending ? ' (in attesa)' : ''

  if (hasHourly) {
    const start = `${r.startDate}T${r.startTime}`
    const end = `${r.startDate}T${r.endTime}`
    const hoursLabel = `${r.startTime!.slice(0, 5)}-${r.endTime!.slice(0, 5)}`
    return {
      id: `req-${r.id}`,
      title: `${r.typeName} ${hoursLabel}${statusSuffix}`,
      start,
      end,
      allDay: false,
      backgroundColor: isPending ? 'transparent' : color,
      borderColor: color,
      textColor: isPending ? color : '#fff',
      extendedProps: {
        eventTypeName: r.typeName,
        notes: r.notes,
        isRequest: true,
        statusName: r.statusName,
      },
    }
  }

  // FullCalendar all-day end is exclusive: add 1 day to endDate
  const baseEnd = r.endDate ?? r.startDate
  const d = new Date(baseEnd + 'T00:00:00')
  d.setDate(d.getDate() + 1)
  const endExclusive = d.toISOString().split('T')[0]

  return {
    id: `req-${r.id}`,
    title: `${r.typeName}${statusSuffix}`,
    start: r.startDate,
    end: endExclusive,
    allDay: true,
    backgroundColor: isPending ? 'transparent' : color,
    borderColor: color,
    textColor: isPending ? color : '#fff',
    extendedProps: {
      eventTypeName: r.typeName,
      notes: r.notes,
      isRequest: true,
      statusName: r.statusName,
    },
  }
}

export default function CalendarioPage() {
  const calendarRef = useRef<FullCalendar>(null)
  const [selectedEvent, setSelectedEvent] = useState<EventDetail | null>(null)
  const [error, setError] = useState('')

  const fetchEvents = async (
    info: { startStr: string; endStr: string },
    successCallback: (events: EventInput[]) => void,
    failureCallback: (error: Error) => void
  ) => {
    try {
      const from = info.startStr.split('T')[0]
      const to = info.endStr.split('T')[0]
      // Parallel fetch:
      //  - raw events (ChiusuraAziendale and any merchant-created Ferie/Permessi/Malattia events)
      //  - effective schedule (Turni with approved leaves applied as segments)
      //  - employee's own requests (Ferie/Permessi/Malattia approved or pending) so the calendar
      //    shows them as standalone blocks even when no shift exists on those days.
      const [rawRes, effectiveRes, requestsRes] = await Promise.all([
        apiClient.get<ApiEvent[]>('/events/employee', { params: { from, to } }),
        apiClient.get<ApiEffectiveShift[]>('/events/employee/effective-schedule', { params: { from, to } }),
        apiClient.get<ApiEmployeeRequest[]>('/employee-requests/my'),
      ])

      const raw = Array.isArray(rawRes.data) ? rawRes.data : []
      const effective = Array.isArray(effectiveRes.data) ? effectiveRes.data : []
      const requests = Array.isArray(requestsRes.data) ? requestsRes.data : []

      // Turni are rendered from the effective schedule (segmented by leaves).
      // ChiusuraAziendale comes from the raw feed.
      const nonShiftEvents = raw
        .filter(e => e.eventTypeName !== 'Turno')
        .map(apiEventToFCEvent)
      const shiftEvents = effective.flatMap(effectiveShiftToFCEvents)

      // Render approved/pending leave requests; filter to current visible range.
      const requestEvents = requests
        .filter(r => r.statusName !== 'Rejected')
        .filter(r => {
          const rStart = r.startDate
          const rEnd = r.endDate ?? r.startDate
          return rEnd >= from && rStart <= to
        })
        .map(requestToFCEvent)

      successCallback([...shiftEvents, ...nonShiftEvents, ...requestEvents])
    } catch (err) {
      failureCallback(err as Error)
      setError('Errore nel caricamento degli eventi')
    }
  }

  const handleEventClick = (info: EventClickArg) => {
    const ep = info.event.extendedProps
    setSelectedEvent({
      id: info.event.id,
      title: info.event.title,
      start: info.event.startStr,
      end: info.event.endStr || undefined,
      allDay: info.event.allDay,
      eventType: ep.eventTypeName as string | undefined,
      isOnCall: ep.isOnCall as boolean | undefined,
      participants: ep.participants as EventDetail['participants'],
      notes: ep.notes as string | undefined,
    })
  }

  return (
    <div className="calendario-page">
      <div className="calendario-header">
        <h1 className="calendario-title">Il mio calendario</h1>
      </div>

      {error && <div className="calendario-error">{error}</div>}

      <div className="calendario-container">
        <FullCalendar
          ref={calendarRef}
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView="dayGridMonth"
          locale={itLocale}
          headerToolbar={{
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek',
          }}
          events={fetchEvents}
          eventClick={handleEventClick}
          height="auto"
          eventDidMount={info => {
            const isOnCall = info.event.extendedProps.isOnCall
            if (isOnCall) {
              info.el.style.borderStyle = 'dashed'
            }
          }}
        />
      </div>

      {/* Legend */}
      <div className="calendario-legend">
        {[
          { label: 'Turno', color: '#3b82f6' },
          { label: 'Ferie', color: '#ec4899' },
          { label: 'Permessi', color: '#8b5cf6' },
          { label: 'Malattia', color: '#f59e0b' },
          { label: 'Chiusura aziendale', color: '#475569' },
        ].map(item => (
          <div key={item.label} className="legend-item">
            <span className="legend-dot" style={{ backgroundColor: item.color }} />
            <span className="legend-label">{item.label}</span>
          </div>
        ))}
        <div className="legend-item">
          <span className="legend-dot legend-dot--dashed" style={{ borderColor: '#94a3b8' }} />
          <span className="legend-label">Reperibile</span>
        </div>
      </div>

      {selectedEvent && (
        <EventDetailModal
          event={selectedEvent}
          onClose={() => setSelectedEvent(null)}
        />
      )}
    </div>
  )
}
