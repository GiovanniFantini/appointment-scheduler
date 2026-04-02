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
      const { data } = await apiClient.get<ApiEvent[]>('/events/employee', {
        params: { from, to },
      })
      const events = Array.isArray(data) ? data.map(apiEventToFCEvent) : []
      successCallback(events)
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
