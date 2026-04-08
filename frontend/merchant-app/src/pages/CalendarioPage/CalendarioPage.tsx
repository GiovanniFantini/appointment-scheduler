import { useState, useCallback } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import itLocale from '@fullcalendar/core/locales/it'
import type { EventClickArg, DatesSetArg, EventInput } from '@fullcalendar/core'
import type { DateClickArg } from '@fullcalendar/interaction'
import apiClient from '../../lib/axios'
import EventModal from '../../components/EventModal/EventModal'
import type { CalEvent, EventType } from '../../components/EventModal/EventModal'
import { MerchantUser } from '../../App'
import './CalendarioPage.css'

interface CalendarioPageProps {
  user: MerchantUser
}

interface ApiEvent {
  id: number
  merchantId: number
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
  participants: Array<{ employeeId: number; fullName: string; isOwner: boolean }>
}

interface ApiEmployeeRequest {
  id: number
  employeeId: number
  employeeFullName: string
  typeName: string   // "Ferie" | "CambioTurno" | "Permessi" | "Malattia"
  statusName: string // "Pending" | "Approved" | "Rejected"
  startDate: string
  endDate?: string
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
    isAllDay: e.isAllDay,
    startDate: e.startDate,
    endDate: e.endDate,
    startTime: e.startTime,
    endTime: e.endTime,
    isOnCall: e.isOnCall,
    ownerEmployeeIds: e.participants.filter(p => p.isOwner).map(p => p.employeeId),
    coOwnerEmployeeIds: e.participants.filter(p => !p.isOwner).map(p => p.employeeId),
    recurrence: (e.recurrence ?? 'Nessuna') as CalEvent['recurrence'],
    notificationEnabled: e.notificationEnabled,
    notes: e.notes ?? undefined,
  }
}

function requestToFCEvent(r: ApiEmployeeRequest): EventInput {
  const color = EVENT_COLORS[r.typeName] ?? '#6366f1'
  const isPending = r.statusName === 'Pending'

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

function toFCEvent(e: ApiEvent): EventInput {
  const color = EVENT_COLORS[e.eventTypeName] ?? '#6366f1'
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

  return {
    id: String(e.id),
    title: e.title,
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
  const [events, setEvents] = useState<EventInput[]>([])
  const [selectedEvent, setSelectedEvent] = useState<Partial<CalEvent> | null>(null)
  const [defaultDate, setDefaultDate] = useState<string>('')
  const [modalOpen, setModalOpen] = useState(false)

  const fetchEvents = useCallback(async (from: string, to: string) => {
    try {
      const [eventsRes, requestsRes] = await Promise.all([
        apiClient.get(`/events?from=${from}&to=${to}`),
        apiClient.get(`/employee-requests`),
      ])

      const eventItems: EventInput[] = Array.isArray(eventsRes.data)
        ? (eventsRes.data as ApiEvent[]).map(toFCEvent)
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
  }, [])

  const handleDatesSet = (info: DatesSetArg) => {
    const from = info.startStr.split('T')[0]
    const to = info.endStr.split('T')[0]
    fetchEvents(from, to)
  }

  const handleEventClick = (info: EventClickArg) => {
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
    setSelectedEvent(null)
    setDefaultDate(info.dateStr.split('T')[0])
    setModalOpen(true)
  }

  const handleNewEvent = () => {
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
    const now = new Date()
    const from = now.toISOString().split('T')[0]
    const futureDate = new Date(now)
    futureDate.setDate(now.getDate() + 60)
    fetchEvents(from, futureDate.toISOString().split('T')[0])
  }

  return (
    <div className="calendario-page">
      <div className="calendario-header">
        <h1 className="calendario-title">Calendario</h1>
        <button className="btn-new-event" onClick={handleNewEvent}>
          + Nuovo Evento
        </button>
      </div>

      <div className="legend">
        {LEGEND_ITEMS.map(item => (
          <div key={item.label} className="legend-item">
            <span className="legend-dot" style={{ background: item.color }} />
            {item.label}
          </div>
        ))}
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

      <div className="calendar-wrapper">
        <FullCalendar
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView="dayGridMonth"
          locale={itLocale}
          headerToolbar={{
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek',
          }}
          events={events}
          eventClick={handleEventClick}
          dateClick={handleDateClick}
          datesSet={handleDatesSet}
          height="100%"
          editable={false}
          selectable={true}
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
    </div>
  )
}
