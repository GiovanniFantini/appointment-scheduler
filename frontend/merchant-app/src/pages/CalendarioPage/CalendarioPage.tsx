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
  title: string
  eventType: EventType
  isAllDay: boolean
  startDate: string
  endDate?: string
  startTime?: string
  endTime?: string
  isOnCall?: boolean
  employeeIds?: number[]
  recurrence?: string
  notify?: boolean
  notes?: string
}

const EVENT_COLORS: Record<string, string> = {
  Turno: '#3b82f6',
  ChiusuraAziendale: '#1f2937',
  Ferie: '#ec4899',
  Permessi: '#8b5cf6',
  Malattia: '#f59e0b',
}

const LEGEND_ITEMS = Object.entries(EVENT_COLORS).map(([label, color]) => ({ label, color }))

function apiEventToCalEvent(e: ApiEvent): Partial<CalEvent> {
  return {
    id: e.id,
    title: e.title,
    eventType: e.eventType,
    isAllDay: e.isAllDay,
    startDate: e.startDate,
    endDate: e.endDate,
    startTime: e.startTime,
    endTime: e.endTime,
    isOnCall: e.isOnCall,
    employeeIds: e.employeeIds,
    recurrence: e.recurrence as CalEvent['recurrence'],
    notify: e.notify,
    notes: e.notes,
  }
}

function toFCEvent(e: ApiEvent): EventInput {
  const color = EVENT_COLORS[e.eventType] ?? '#6366f1'
  const textColor = e.eventType === 'ChiusuraAziendale' ? '#fff' : undefined

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
      const res = await apiClient.get(`/events?from=${from}&to=${to}`)
      if (Array.isArray(res.data)) {
        setEvents((res.data as ApiEvent[]).map(toFCEvent))
      }
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
    // Re-fetch current range — FullCalendar will trigger datesSet
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
