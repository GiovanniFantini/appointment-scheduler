import { useState, useEffect } from 'react'
import apiClient from '../../lib/axios'
import './EventModal.css'

export type EventType = 'Turno' | 'ChiusuraAziendale' | 'Ferie' | 'Permessi' | 'Malattia'
export type RecurrenceType = 'Nessuna' | 'Giornaliera' | 'Settimanale' | 'Mensile'

export interface CalEvent {
  id?: number
  title: string
  eventType: EventType
  isAllDay: boolean
  startDate: string
  endDate?: string
  startTime?: string
  endTime?: string
  isOnCall?: boolean
  employeeIds?: number[]
  recurrence?: RecurrenceType
  notify?: boolean
  notes?: string
}

interface Employee {
  id: number
  firstName: string
  lastName: string
}

interface EventModalProps {
  event: Partial<CalEvent> | null
  defaultDate?: string
  onClose: () => void
  onSaved: () => void
}

const EVENT_TYPES: EventType[] = ['Turno', 'ChiusuraAziendale', 'Ferie', 'Permessi', 'Malattia']
const RECURRENCE_TYPES: RecurrenceType[] = ['Nessuna', 'Giornaliera', 'Settimanale', 'Mensile']

export default function EventModal({ event, defaultDate, onClose, onSaved }: EventModalProps) {
  const isEdit = !!event?.id

  const [title, setTitle] = useState(event?.title ?? '')
  const [eventType, setEventType] = useState<EventType>(event?.eventType ?? 'Turno')
  const [isAllDay, setIsAllDay] = useState(event?.isAllDay ?? false)
  const [startDate, setStartDate] = useState(event?.startDate ?? defaultDate ?? '')
  const [endDate, setEndDate] = useState(event?.endDate ?? '')
  const [startTime, setStartTime] = useState(event?.startTime ?? '09:00')
  const [endTime, setEndTime] = useState(event?.endTime ?? '17:00')
  const [isOnCall, setIsOnCall] = useState(event?.isOnCall ?? false)
  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<number[]>(event?.employeeIds ?? [])
  const [recurrence, setRecurrence] = useState<RecurrenceType>(event?.recurrence ?? 'Nessuna')
  const [notify, setNotify] = useState(event?.notify ?? false)
  const [notes, setNotes] = useState(event?.notes ?? '')
  const [showClone, setShowClone] = useState(false)
  const [cloneFrom, setCloneFrom] = useState('')
  const [cloneTo, setCloneTo] = useState('')

  const [employees, setEmployees] = useState<Employee[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    apiClient.get('/employees').then(res => {
      if (Array.isArray(res.data)) setEmployees(res.data as Employee[])
    }).catch(() => {})
  }, [])

  const buildPayload = () => ({
    title,
    eventType,
    isAllDay,
    startDate,
    endDate: endDate || startDate,
    startTime: isAllDay ? undefined : startTime,
    endTime: isAllDay ? undefined : endTime,
    isOnCall: eventType === 'Turno' ? isOnCall : false,
    employeeIds: selectedEmployeeIds,
    recurrence,
    notify,
    notes,
  })

  const handleSave = async () => {
    if (!title.trim()) { setError('Il titolo è obbligatorio'); return }
    if (!startDate) { setError('La data di inizio è obbligatoria'); return }
    setError('')
    setLoading(true)
    try {
      if (isEdit) {
        await apiClient.put(`/events/${event!.id}`, buildPayload())
      } else {
        await apiClient.post('/events', buildPayload())
      }
      onSaved()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!confirm('Eliminare questo evento?')) return
    setLoading(true)
    try {
      await apiClient.delete(`/events/${event!.id}`)
      onSaved()
    } catch {
      setError('Errore durante l\'eliminazione')
    } finally {
      setLoading(false)
    }
  }

  const handleClone = async () => {
    if (!cloneFrom || !cloneTo) { setError('Seleziona date per la clonazione'); return }
    setLoading(true)
    try {
      await apiClient.post(`/events/${event!.id}/clone`, { fromDate: cloneFrom, toDate: cloneTo })
      onSaved()
    } catch {
      setError('Errore durante la clonazione')
    } finally {
      setLoading(false)
    }
  }

  const handleEmployeeSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selected = Array.from(e.target.selectedOptions).map(o => Number(o.value))
    setSelectedEmployeeIds(selected)
  }

  return (
    <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="modal-container">
        <div className="modal-header">
          <h2 className="modal-title">{isEdit ? 'Modifica Evento' : 'Nuovo Evento'}</h2>
          <button className="modal-close-btn" onClick={onClose}>✕</button>
        </div>

        <div className="modal-body">
          {error && <div className="modal-error">{error}</div>}

          <div className="modal-form-group">
            <label className="modal-label">Titolo *</label>
            <input
              type="text"
              className="modal-input"
              placeholder="Inserisci titolo evento"
              value={title}
              onChange={e => setTitle(e.target.value)}
            />
          </div>

          <div className="modal-form-group">
            <label className="modal-label">Tipologia *</label>
            <select className="modal-select" value={eventType} onChange={e => setEventType(e.target.value as EventType)}>
              {EVENT_TYPES.map(t => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>

          <div className="toggle-group">
            <label className="toggle-switch">
              <input type="checkbox" checked={isAllDay} onChange={e => setIsAllDay(e.target.checked)} />
              <span className="toggle-slider" />
            </label>
            <span className="toggle-label">Tutto il giorno</span>
          </div>

          <div className="modal-row">
            <div className="modal-form-group">
              <label className="modal-label">Data inizio *</label>
              <input type="date" className="modal-input" value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div className="modal-form-group">
              <label className="modal-label">Data fine</label>
              <input type="date" className="modal-input" value={endDate} onChange={e => setEndDate(e.target.value)} />
            </div>
          </div>

          {!isAllDay && (
            <div className="modal-row">
              <div className="modal-form-group">
                <label className="modal-label">Orario da</label>
                <input type="time" className="modal-input" value={startTime} onChange={e => setStartTime(e.target.value)} />
              </div>
              <div className="modal-form-group">
                <label className="modal-label">Orario a</label>
                <input type="time" className="modal-input" value={endTime} onChange={e => setEndTime(e.target.value)} />
              </div>
            </div>
          )}

          {eventType === 'Turno' && (
            <div className="checkbox-group">
              <input
                type="checkbox"
                id="isOnCall"
                className="modal-checkbox"
                checked={isOnCall}
                onChange={e => setIsOnCall(e.target.checked)}
              />
              <label htmlFor="isOnCall" className="checkbox-label">Reperibilità</label>
            </div>
          )}

          {employees.length > 0 && (
            <div className="modal-form-group">
              <label className="modal-label">Titolari (Ctrl+click per selezione multipla)</label>
              <select
                multiple
                className="multi-select"
                value={selectedEmployeeIds.map(String)}
                onChange={handleEmployeeSelect}
              >
                {employees.map(emp => (
                  <option key={emp.id} value={emp.id}>
                    {emp.firstName} {emp.lastName}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="modal-form-group">
            <label className="modal-label">Ricorrenza</label>
            <select className="modal-select" value={recurrence} onChange={e => setRecurrence(e.target.value as RecurrenceType)}>
              {RECURRENCE_TYPES.map(r => (
                <option key={r} value={r}>{r}</option>
              ))}
            </select>
          </div>

          <div className="checkbox-group">
            <input
              type="checkbox"
              id="notify"
              className="modal-checkbox"
              checked={notify}
              onChange={e => setNotify(e.target.checked)}
            />
            <label htmlFor="notify" className="checkbox-label">Invia notifica</label>
          </div>

          <div className="modal-form-group">
            <label className="modal-label">Note</label>
            <textarea
              className="modal-textarea"
              placeholder="Note aggiuntive..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          {isEdit && (
            <>
              <hr className="section-sep" />
              <div className="checkbox-group">
                <input
                  type="checkbox"
                  id="showClone"
                  className="modal-checkbox"
                  checked={showClone}
                  onChange={e => setShowClone(e.target.checked)}
                />
                <label htmlFor="showClone" className="checkbox-label">Clona evento in un intervallo</label>
              </div>
              {showClone && (
                <div className="clone-section">
                  <div className="clone-section-title">Clonazione</div>
                  <div className="modal-row">
                    <div className="modal-form-group">
                      <label className="modal-label">Da data</label>
                      <input type="date" className="modal-input" value={cloneFrom} onChange={e => setCloneFrom(e.target.value)} />
                    </div>
                    <div className="modal-form-group">
                      <label className="modal-label">A data</label>
                      <input type="date" className="modal-input" value={cloneTo} onChange={e => setCloneTo(e.target.value)} />
                    </div>
                  </div>
                  <button className="btn-clone" onClick={handleClone} disabled={loading}>
                    Clona
                  </button>
                </div>
              )}
            </>
          )}
        </div>

        <div className="modal-footer">
          {isEdit && (
            <button className="btn-delete" onClick={handleDelete} disabled={loading}>Elimina</button>
          )}
          <div className="modal-footer-right">
            <button className="btn-cancel" onClick={onClose}>Annulla</button>
            <button className="btn-save" onClick={handleSave} disabled={loading}>
              {loading ? 'Salvataggio...' : 'Salva'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
