import { useCallback, useEffect, useMemo, useState } from 'react'
import apiClient from '../../lib/axios'
import EventModal from '../../components/EventModal/EventModal'
import type { CalEvent } from '../../components/EventModal/EventModal'
import { MerchantUser } from '../../App'
import { formatBrowserDate } from '../../lib/dateUtils'
import './PianificazionePage.css'

interface Props {
  user: MerchantUser
}

interface Employee {
  id: number
  firstName: string
  lastName: string
  isActive: boolean
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
  participants: Array<{
    employeeId: number
    fullName: string
    isOwner: boolean
    startTimeOverride?: string
    endTimeOverride?: string
    participantNotes?: string
  }>
}

/** Returns the Monday of the week containing the given date (ISO: Mon=1..Sun=7). */
function mondayOf(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay() === 0 ? 7 : d.getDay()
  d.setDate(d.getDate() - (day - 1))
  d.setHours(0, 0, 0, 0)
  return d
}

function toISO(d: Date): string {
  return d.toISOString().split('T')[0]
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d)
  r.setDate(r.getDate() + n)
  return r
}

const DAY_NAMES = ['Lun', 'Mar', 'Mer', 'Gio', 'Ven', 'Sab', 'Dom']

export default function PianificazionePage(_: Props) {
  const [weekStart, setWeekStart] = useState<Date>(mondayOf(new Date()))
  const [employees, setEmployees] = useState<Employee[]>([])
  const [events, setEvents] = useState<ApiEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [selectedEvent, setSelectedEvent] = useState<Partial<CalEvent> | null>(null)
  const [defaultDate, setDefaultDate] = useState('')
  const [cloneTargetWeek, setCloneTargetWeek] = useState('')
  const [cloneWeeks, setCloneWeeks] = useState(1)
  const [cloneLoading, setCloneLoading] = useState(false)
  const [cloneMessage, setCloneMessage] = useState('')

  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)), [weekStart])
  const weekEnd = days[6]

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const from = toISO(weekStart)
      const to = toISO(weekEnd)
      const [empRes, evRes] = await Promise.all([
        apiClient.get<Employee[]>('/employees'),
        apiClient.get<ApiEvent[]>('/events', { params: { from, to } }),
      ])
      setEmployees((Array.isArray(empRes.data) ? empRes.data : []).filter(e => e.isActive))
      setEvents(Array.isArray(evRes.data) ? evRes.data.filter(e => e.eventTypeName === 'Turno') : [])
    } finally {
      setLoading(false)
    }
  }, [weekStart, weekEnd])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  const eventsByCell = useMemo(() => {
    const map = new Map<string, ApiEvent[]>()
    for (const ev of events) {
      for (const p of ev.participants) {
        const key = `${p.employeeId}-${ev.startDate}`
        const list = map.get(key) ?? []
        list.push(ev)
        map.set(key, list)
      }
    }
    return map
  }, [events])

  const openNewShift = (employeeId: number, date: string) => {
    setSelectedEvent({
      title: 'Turno',
      eventType: 'Turno',
      isAllDay: false,
      startDate: date,
      startTime: '09:00',
      endTime: '17:00',
      ownerEmployeeIds: [employeeId],
      coOwnerEmployeeIds: [],
      recurrence: 'Nessuna',
      notificationEnabled: false,
    })
    setDefaultDate(date)
    setModalOpen(true)
  }

  const openEditShift = async (eventId: number) => {
    try {
      const res = await apiClient.get<ApiEvent>(`/events/${eventId}`)
      const e = res.data
      setSelectedEvent({
        id: e.id,
        title: e.title,
        eventType: 'Turno',
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
      })
      setModalOpen(true)
    } catch {
      // ignore
    }
  }

  const handleCloneWeek = async () => {
    if (!cloneTargetWeek) {
      setCloneMessage('Seleziona la settimana target')
      return
    }
    setCloneLoading(true)
    setCloneMessage('')
    try {
      const res = await apiClient.post('/events/clone-week', {
        sourceWeekStart: toISO(weekStart),
        targetWeekStart: cloneTargetWeek,
        numberOfWeeks: cloneWeeks,
      })
      const count = Array.isArray(res.data) ? res.data.length : 0
      setCloneMessage(`${count} turno/i clonato/i.`)
      await fetchData()
    } catch {
      setCloneMessage('Errore durante la clonazione')
    } finally {
      setCloneLoading(false)
    }
  }

  const handleSaved = () => {
    setModalOpen(false)
    setSelectedEvent(null)
    fetchData()
  }

  const shiftLabel = (ev: ApiEvent, employeeId: number): string => {
    const p = ev.participants.find(x => x.employeeId === employeeId)
    const start = p?.startTimeOverride ?? ev.startTime
    const end = p?.endTimeOverride ?? ev.endTime
    if (ev.isAllDay || !start || !end) return ev.title
    return `${start.slice(0, 5)}-${end.slice(0, 5)}`
  }

  const formatWeekLabel = (): string => {
    const opts: Intl.DateTimeFormatOptions = { day: '2-digit', month: '2-digit' }
    return `${formatBrowserDate(weekStart, opts)} - ${formatBrowserDate(weekEnd, opts)}`
  }

  return (
    <div className="pianificazione-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Pianificazione</h1>
          <p className="page-subtitle">Vista settimanale turni per risorsa</p>
        </div>
      </div>

      <div className="pianif-toolbar">
        <div className="pianif-week-nav">
          <button className="btn-secondary" onClick={() => setWeekStart(addDays(weekStart, -7))}>‹ Settimana precedente</button>
          <button className="btn-secondary" onClick={() => setWeekStart(mondayOf(new Date()))}>Oggi</button>
          <button className="btn-secondary" onClick={() => setWeekStart(addDays(weekStart, 7))}>Settimana successiva ›</button>
          <span className="pianif-week-label">{formatWeekLabel()}</span>
        </div>

        <div className="pianif-clone">
          <label className="form-label">Clona a settimana (lun.)</label>
          <input
            type="date"
            className="form-input"
            value={cloneTargetWeek}
            onChange={e => setCloneTargetWeek(e.target.value)}
          />
          <input
            type="number"
            className="form-input pianif-clone-weeks"
            min={1}
            max={52}
            value={cloneWeeks}
            onChange={e => setCloneWeeks(Math.max(1, Math.min(52, Number(e.target.value) || 1)))}
            title="Numero di settimane consecutive"
          />
          <button className="btn-primary" disabled={cloneLoading} onClick={handleCloneWeek}>
            {cloneLoading ? 'Clonazione...' : 'Clona'}
          </button>
          {cloneMessage && <span className="pianif-clone-msg">{cloneMessage}</span>}
        </div>
      </div>

      {loading ? (
        <div className="pianif-loading">Caricamento...</div>
      ) : employees.length === 0 ? (
        <div className="pianif-empty">Nessun dipendente attivo.</div>
      ) : (
        <div className="pianif-table-wrap">
          <table className="pianif-table">
            <thead>
              <tr>
                <th className="pianif-th-name">Risorsa</th>
                {days.map((d, i) => (
                  <th key={i} className="pianif-th-day">
                    <div className="pianif-day-name">{DAY_NAMES[i]}</div>
                    <div className="pianif-day-date">{d.getDate()}/{d.getMonth() + 1}</div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {employees.map(emp => (
                <tr key={emp.id}>
                  <td className="pianif-td-name">
                    <div className="pianif-emp-name">{emp.firstName} {emp.lastName}</div>
                  </td>
                  {days.map(d => {
                    const iso = toISO(d)
                    const key = `${emp.id}-${iso}`
                    const shifts = eventsByCell.get(key) ?? []
                    return (
                      <td key={iso} className="pianif-cell" onClick={() => { if (shifts.length === 0) openNewShift(emp.id, iso) }}>
                        {shifts.length === 0 ? (
                          <div className="pianif-empty-cell">+</div>
                        ) : (
                          shifts.map(s => (
                            <div
                              key={s.id}
                              className="pianif-pill"
                              onClick={e => { e.stopPropagation(); openEditShift(s.id) }}
                            >
                              {shiftLabel(s, emp.id)}
                            </div>
                          ))
                        )}
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {modalOpen && (
        <EventModal
          event={selectedEvent}
          defaultDate={defaultDate}
          onClose={() => { setModalOpen(false); setSelectedEvent(null) }}
          onSaved={handleSaved}
        />
      )}
    </div>
  )
}
