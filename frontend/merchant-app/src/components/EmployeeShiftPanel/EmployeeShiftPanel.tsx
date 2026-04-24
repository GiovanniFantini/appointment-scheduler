import { useCallback, useEffect, useRef, useState } from 'react'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import itLocale from '@fullcalendar/core/locales/it'
import type { DatesSetArg, EventInput, EventClickArg } from '@fullcalendar/core'
import type { DateClickArg } from '@fullcalendar/interaction'
import apiClient from '../../lib/axios'
import EventModal from '../EventModal/EventModal'
import type { CalEvent } from '../EventModal/EventModal'
import './EmployeeShiftPanel.css'

interface Props {
  employeeId: number
  employeeFullName: string
  onClose: () => void
}

interface ApiEffectiveShift {
  eventId: number
  employeeId: number
  title: string
  date: string
  isAllDay: boolean
  canonicalStart?: string
  canonicalEnd?: string
  segments: Array<{ from?: string; to?: string }>
  appliedLeaves: Array<{ requestId: number; typeName: string; isFullDay: boolean; startTime?: string; endTime?: string }>
  isFullyAbsent: boolean
  isOnCall: boolean
}

interface ApiEvent {
  id: number
  title: string
  eventTypeName: string
  startDate: string
  endDate?: string
  isAllDay: boolean
  startTime?: string
  endTime?: string
  isOnCall: boolean
  notes?: string
  participants: Array<{
    employeeId: number
    fullName: string
    isOwner: boolean
    startTimeOverride?: string
    endTimeOverride?: string
    participantNotes?: string
  }>
}

/**
 * Panel that shows a single employee's effective schedule and lets the merchant
 * quickly create a shift for them or clone an existing one week-by-week.
 */
export default function EmployeeShiftPanel({ employeeId, employeeFullName, onClose }: Props) {
  const calendarRef = useRef<FullCalendar>(null)
  const [events, setEvents] = useState<EventInput[]>([])
  const [modalOpen, setModalOpen] = useState(false)
  const [selectedEvent, setSelectedEvent] = useState<Partial<CalEvent> | null>(null)
  const [defaultDate, setDefaultDate] = useState('')
  const [cloneSourceWeek, setCloneSourceWeek] = useState('')
  const [cloneTargetWeek, setCloneTargetWeek] = useState('')
  const [cloneWeeks, setCloneWeeks] = useState(1)
  const [showClone, setShowClone] = useState(false)
  const [cloneLoading, setCloneLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [currentRange, setCurrentRange] = useState<{ from: string; to: string } | null>(null)

  const fetchSchedule = useCallback(async (from: string, to: string) => {
    try {
      const res = await apiClient.get<ApiEffectiveShift[]>(
        `/events/employee/${employeeId}/effective-schedule`,
        { params: { from, to } }
      )
      const shifts = Array.isArray(res.data) ? res.data : []

      const items: EventInput[] = shifts.flatMap((s): EventInput[] => {
        if (s.isFullyAbsent) {
          return [{
            id: `shift-${s.eventId}`,
            title: `${s.title} (assente)`,
            start: s.date,
            allDay: true,
            backgroundColor: '#e5e7eb',
            borderColor: '#9ca3af',
            textColor: '#6b7280',
            extendedProps: { eventId: s.eventId },
          }]
        }
        if (s.isAllDay) {
          return [{
            id: `shift-${s.eventId}`,
            title: s.title,
            start: s.date,
            allDay: true,
            backgroundColor: '#3b82f6',
            borderColor: '#3b82f6',
            extendedProps: { eventId: s.eventId },
          }]
        }
        return s.segments.map((seg, idx): EventInput => ({
          id: `shift-${s.eventId}-seg-${idx}`,
          title: s.segments.length > 1 ? `${s.title} (parte ${idx + 1})` : s.title,
          start: seg.from ? `${s.date}T${seg.from}` : s.date,
          end: seg.to ? `${s.date}T${seg.to}` : undefined,
          backgroundColor: '#3b82f6',
          borderColor: '#3b82f6',
          extendedProps: { eventId: s.eventId },
        }))
      })

      setEvents(items)
    } catch {
      setEvents([])
    }
  }, [employeeId])

  const handleDatesSet = (info: DatesSetArg) => {
    const from = info.startStr.split('T')[0]
    const to = info.endStr.split('T')[0]
    setCurrentRange({ from, to })
    fetchSchedule(from, to)
  }

  const handleDateClick = (info: DateClickArg) => {
    setSelectedEvent({
      title: 'Turno',
      eventType: 'Turno',
      isAllDay: false,
      startDate: info.dateStr.split('T')[0],
      startTime: '09:00',
      endTime: '17:00',
      ownerEmployeeIds: [employeeId],
      coOwnerEmployeeIds: [],
      recurrence: 'Nessuna',
      notificationEnabled: false,
    })
    setDefaultDate(info.dateStr.split('T')[0])
    setModalOpen(true)
  }

  const handleEventClick = async (info: EventClickArg) => {
    const eventId = info.event.extendedProps.eventId as number | undefined
    if (!eventId) return
    try {
      const res = await apiClient.get<ApiEvent>(`/events/${eventId}`)
      const e = res.data
      setSelectedEvent({
        id: e.id,
        title: e.title,
        eventType: e.eventTypeName as CalEvent['eventType'],
        isAllDay: e.isAllDay,
        startDate: e.startDate,
        endDate: e.endDate,
        startTime: e.startTime?.slice(0, 5),
        endTime: e.endTime?.slice(0, 5),
        isOnCall: e.isOnCall,
        ownerEmployeeIds: e.participants.filter(p => p.isOwner).map(p => p.employeeId),
        coOwnerEmployeeIds: e.participants.filter(p => !p.isOwner).map(p => p.employeeId),
        participantOverrides: e.participants
          .filter(p => p.startTimeOverride || p.endTimeOverride || p.participantNotes)
          .map(p => ({
            employeeId: p.employeeId,
            startTimeOverride: p.startTimeOverride?.slice(0, 5),
            endTimeOverride: p.endTimeOverride?.slice(0, 5),
            participantNotes: p.participantNotes,
          })),
        recurrence: 'Nessuna',
        notificationEnabled: false,
        notes: e.notes,
      })
      setModalOpen(true)
    } catch {
      // ignore
    }
  }

  const handleCloneWeek = async () => {
    if (!cloneSourceWeek || !cloneTargetWeek) {
      setMessage('Seleziona entrambe le settimane')
      return
    }
    setCloneLoading(true)
    setMessage('')
    try {
      const res = await apiClient.post('/events/clone-week', {
        sourceWeekStart: cloneSourceWeek,
        targetWeekStart: cloneTargetWeek,
        numberOfWeeks: cloneWeeks,
        employeeFilter: [employeeId],
      })
      const count = Array.isArray(res.data) ? res.data.length : 0
      setMessage(`${count} turno/i clonato/i.`)
      if (currentRange) await fetchSchedule(currentRange.from, currentRange.to)
    } catch {
      setMessage('Errore durante la clonazione')
    } finally {
      setCloneLoading(false)
    }
  }

  const handleSaved = () => {
    setModalOpen(false)
    setSelectedEvent(null)
    if (currentRange) fetchSchedule(currentRange.from, currentRange.to)
  }

  useEffect(() => {
    // Re-fetch when employee changes (not when range state stores a reference)
    if (currentRange) fetchSchedule(currentRange.from, currentRange.to)
  }, [employeeId, fetchSchedule, currentRange])

  return (
    <div className="shift-panel-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="shift-panel">
        <div className="shift-panel-header">
          <div>
            <h2 className="shift-panel-title">Turni di {employeeFullName}</h2>
            <p className="shift-panel-subtitle">Pianifica, modifica o clona i turni del dipendente</p>
          </div>
          <button className="shift-panel-close" onClick={onClose}>✕</button>
        </div>

        <div className="shift-panel-body">
          <div className="shift-panel-calendar">
            <FullCalendar
              ref={calendarRef}
              plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
              initialView="timeGridWeek"
              locale={itLocale}
              headerToolbar={{
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek',
              }}
              events={events}
              datesSet={handleDatesSet}
              dateClick={handleDateClick}
              eventClick={handleEventClick}
              height={500}
              selectable
            />
          </div>

          <div className="shift-panel-sidebar">
            <div className="shift-panel-section">
              <button className="btn-primary" onClick={() => {
                const today = new Date().toISOString().split('T')[0]
                setSelectedEvent({
                  title: 'Turno',
                  eventType: 'Turno',
                  isAllDay: false,
                  startDate: today,
                  startTime: '09:00',
                  endTime: '17:00',
                  ownerEmployeeIds: [employeeId],
                  coOwnerEmployeeIds: [],
                  recurrence: 'Nessuna',
                  notificationEnabled: false,
                })
                setDefaultDate(today)
                setModalOpen(true)
              }}>
                + Nuovo turno
              </button>
            </div>

            <div className="shift-panel-section">
              <button className="btn-secondary" onClick={() => setShowClone(v => !v)}>
                {showClone ? 'Nascondi clonazione' : 'Clona settimana'}
              </button>

              {showClone && (
                <div className="clone-box">
                  <label className="form-label">Settimana sorgente (lunedì)</label>
                  <input
                    type="date"
                    className="form-input"
                    value={cloneSourceWeek}
                    onChange={e => setCloneSourceWeek(e.target.value)}
                  />
                  <label className="form-label">Settimana target (lunedì)</label>
                  <input
                    type="date"
                    className="form-input"
                    value={cloneTargetWeek}
                    onChange={e => setCloneTargetWeek(e.target.value)}
                  />
                  <label className="form-label">N. settimane consecutive</label>
                  <input
                    type="number"
                    className="form-input"
                    min={1}
                    max={52}
                    value={cloneWeeks}
                    onChange={e => setCloneWeeks(Math.max(1, Math.min(52, Number(e.target.value) || 1)))}
                  />
                  <button
                    className="btn-primary"
                    disabled={cloneLoading}
                    onClick={handleCloneWeek}
                  >
                    {cloneLoading ? 'Clonazione...' : 'Clona'}
                  </button>
                  {message && <div className="clone-message">{message}</div>}
                </div>
              )}
            </div>
          </div>
        </div>

        {modalOpen && (
          <EventModal
            event={selectedEvent}
            defaultDate={defaultDate}
            onClose={() => { setModalOpen(false); setSelectedEvent(null) }}
            onSaved={handleSaved}
          />
        )}
      </div>
    </div>
  )
}
