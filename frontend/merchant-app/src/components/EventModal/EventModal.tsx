import { useState, useEffect } from 'react'
import apiClient from '../../lib/axios'
import './EventModal.css'

export type EventType = 'Turno' | 'ChiusuraAziendale' | 'Ferie' | 'Permessi' | 'Malattia'
export type RecurrenceType = 'Nessuna' | 'Giornaliera' | 'Settimanale' | 'Mensile'

// Mapping from EventType string to server enum value
const EVENT_TYPE_VALUES: Record<EventType, number> = {
  Turno: 1,
  ChiusuraAziendale: 2,
  Ferie: 3,
  Permessi: 4,
  Malattia: 5,
}

export interface ParticipantOverrideInput {
  employeeId: number
  startTimeOverride?: string
  endTimeOverride?: string
  participantNotes?: string
}

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
  ownerEmployeeIds?: number[]
  coOwnerEmployeeIds?: number[]
  participantOverrides?: ParticipantOverrideInput[]
  recurrence?: RecurrenceType
  notificationEnabled?: boolean
  notes?: string
}

interface Employee {
  id: number
  firstName: string
  lastName: string
}

interface ShiftConflictDto {
  employeeId: number
  employeeFullName: string
  date: string
  kind: number
  kindName: string
  requestId?: number
  requestType?: number
  conflictingEventId?: number
  conflictingEventTitle?: string
  conflictStart?: string
  conflictEnd?: string
  message: string
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
  const [selectedOwnerIds, setSelectedOwnerIds] = useState<number[]>(event?.ownerEmployeeIds ?? [])
  const [recurrence, setRecurrence] = useState<RecurrenceType>(event?.recurrence ?? 'Nessuna')
  const [notificationEnabled, setNotificationEnabled] = useState(event?.notificationEnabled ?? false)
  const [notes, setNotes] = useState(event?.notes ?? '')
  const [showClone, setShowClone] = useState(false)
  const [cloneFrom, setCloneFrom] = useState('')
  const [cloneTo, setCloneTo] = useState('')
  const [cloneMode, setCloneMode] = useState<'daily' | 'weekly'>('daily')
  const [cloneTargetWeek, setCloneTargetWeek] = useState('')
  const [cloneWeeks, setCloneWeeks] = useState(1)
  const [participantOverrides, setParticipantOverrides] = useState<ParticipantOverrideInput[]>(event?.participantOverrides ?? [])
  const [showOverrides, setShowOverrides] = useState((event?.participantOverrides ?? []).length > 0)

  const [employees, setEmployees] = useState<Employee[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [warnings, setWarnings] = useState<ShiftConflictDto[]>([])
  const [warningsAcknowledged, setWarningsAcknowledged] = useState(false)

  const initialStartTime = event?.startTime ?? ''
  const initialEndTime = event?.endTime ?? ''
  const hasOverrides = participantOverrides.some(o => o.startTimeOverride || o.endTimeOverride || o.participantNotes)
  const timeChanged = isEdit && (startTime !== initialStartTime || endTime !== initialEndTime)

  useEffect(() => {
    apiClient.get('/employees').then(res => {
      if (Array.isArray(res.data)) setEmployees(res.data as Employee[])
    }).catch(() => {})
  }, [])

  // Prune stale overrides when a participant is deselected
  useEffect(() => {
    setParticipantOverrides(prev => prev.filter(o => selectedOwnerIds.includes(o.employeeId)))
  }, [selectedOwnerIds])

  const upsertOverride = (employeeId: number, patch: Partial<ParticipantOverrideInput>) => {
    setParticipantOverrides(prev => {
      const existing = prev.find(o => o.employeeId === employeeId)
      if (existing) {
        return prev.map(o => o.employeeId === employeeId ? { ...o, ...patch } : o)
      }
      return [...prev, { employeeId, ...patch }]
    })
  }

  const getOverride = (employeeId: number): ParticipantOverrideInput | undefined =>
    participantOverrides.find(o => o.employeeId === employeeId)

  const buildPayload = () => ({
    title,
    eventType: EVENT_TYPE_VALUES[eventType],
    isAllDay,
    startDate,
    endDate: endDate || startDate,
    startTime: isAllDay ? undefined : startTime,
    endTime: isAllDay ? undefined : endTime,
    isOnCall: eventType === 'Turno' ? isOnCall : false,
    ownerEmployeeIds: selectedOwnerIds,
    coOwnerEmployeeIds: [],
    participantOverrides: participantOverrides
      .filter(o => o.startTimeOverride || o.endTimeOverride || o.participantNotes)
      .map(o => ({
        employeeId: o.employeeId,
        startTimeOverride: o.startTimeOverride || undefined,
        endTimeOverride: o.endTimeOverride || undefined,
        participantNotes: o.participantNotes || undefined,
      })),
    recurrence: recurrence === 'Nessuna' ? null : recurrence,
    notificationEnabled,
    notes,
  })

  const handleSave = async () => {
    if (!title.trim()) { setError('Il titolo è obbligatorio'); return }
    if (!startDate) { setError('La data di inizio è obbligatoria'); return }

    // Warn before persisting time changes when participant overrides exist
    if (timeChanged && hasOverrides) {
      const proceed = confirm(
        'Hai modificato gli orari del turno, ma alcuni dipendenti hanno orari personalizzati. ' +
        'Vuoi mantenerli? Premi OK per mantenere gli override, Annulla per abbandonare il salvataggio.'
      )
      if (!proceed) return
    }

    setError('')
    setLoading(true)
    try {
      const response = isEdit
        ? await apiClient.put(`/events/${event!.id}`, buildPayload())
        : await apiClient.post('/events', buildPayload())

      const returned = (response?.data ?? {}) as { warnings?: ShiftConflictDto[] }
      const w = Array.isArray(returned.warnings) ? returned.warnings : []
      if (w.length > 0) {
        setWarnings(w)
        setWarningsAcknowledged(false)
        // Don't close: let the user read the warnings and confirm
      } else {
        onSaved()
      }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setLoading(false)
    }
  }

  const dismissWarnings = () => {
    setWarnings([])
    onSaved()
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
    if (cloneMode === 'daily') {
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
      return
    }

    // weekly: requires the event's own startDate as source week anchor
    if (!cloneTargetWeek) { setError('Seleziona la settimana target'); return }
    if (!startDate) { setError('Il turno non ha una data di partenza'); return }
    setLoading(true)
    try {
      // Compute Monday of the event's week as source anchor (ISO week)
      const d = new Date(startDate + 'T00:00:00')
      const dow = d.getDay() === 0 ? 7 : d.getDay()
      d.setDate(d.getDate() - (dow - 1))
      const sourceWeekStart = d.toISOString().split('T')[0]
      await apiClient.post('/events/clone-week', {
        sourceWeekStart,
        targetWeekStart: cloneTargetWeek,
        numberOfWeeks: cloneWeeks,
      })
      onSaved()
    } catch {
      setError('Errore durante la clonazione settimanale')
    } finally {
      setLoading(false)
    }
  }

  const handleEmployeeSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selected = Array.from(e.target.selectedOptions).map(o => Number(o.value))
    setSelectedOwnerIds(selected)
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

          {warnings.length > 0 && (
            <div className="modal-warning">
              <div className="modal-warning-title">
                Conflitti rilevati ({warnings.length}):
              </div>
              <ul className="modal-warning-list">
                {warnings.map((w, idx) => (
                  <li key={idx}>
                    <strong>{w.employeeFullName}</strong> — {w.message}
                  </li>
                ))}
              </ul>
              <label className="checkbox-group">
                <input
                  type="checkbox"
                  className="modal-checkbox"
                  checked={warningsAcknowledged}
                  onChange={e => setWarningsAcknowledged(e.target.checked)}
                />
                <span className="checkbox-label">Ho letto gli avvisi, procedo comunque</span>
              </label>
              <button
                className="btn-save"
                disabled={!warningsAcknowledged}
                onClick={dismissWarnings}
              >
                Chiudi
              </button>
            </div>
          )}

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
                value={selectedOwnerIds.map(String)}
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

          {eventType === 'Turno' && selectedOwnerIds.length > 0 && !isAllDay && (
            <div className="checkbox-group">
              <input
                type="checkbox"
                id="showOverrides"
                className="modal-checkbox"
                checked={showOverrides}
                onChange={e => setShowOverrides(e.target.checked)}
              />
              <label htmlFor="showOverrides" className="checkbox-label">
                Orari personalizzati per partecipante
              </label>
            </div>
          )}

          {eventType === 'Turno' && showOverrides && !isAllDay && selectedOwnerIds.length > 0 && (
            <div className="overrides-section">
              <div className="overrides-hint">
                Lascia vuoto per usare gli orari del turno ({startTime}-{endTime}).
              </div>
              {selectedOwnerIds.map(empId => {
                const emp = employees.find(e => e.id === empId)
                if (!emp) return null
                const ov = getOverride(empId)
                return (
                  <div key={empId} className="override-row">
                    <div className="override-name">{emp.firstName} {emp.lastName}</div>
                    <div className="modal-row">
                      <div className="modal-form-group">
                        <label className="modal-label">Dalle</label>
                        <input
                          type="time"
                          className="modal-input"
                          value={ov?.startTimeOverride ?? ''}
                          onChange={e => upsertOverride(empId, { startTimeOverride: e.target.value || undefined })}
                        />
                      </div>
                      <div className="modal-form-group">
                        <label className="modal-label">Alle</label>
                        <input
                          type="time"
                          className="modal-input"
                          value={ov?.endTimeOverride ?? ''}
                          onChange={e => upsertOverride(empId, { endTimeOverride: e.target.value || undefined })}
                        />
                      </div>
                    </div>
                    <input
                      type="text"
                      className="modal-input"
                      placeholder="Nota (es. rientra dopo visita)"
                      value={ov?.participantNotes ?? ''}
                      onChange={e => upsertOverride(empId, { participantNotes: e.target.value || undefined })}
                    />
                  </div>
                )
              })}
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
              id="notificationEnabled"
              className="modal-checkbox"
              checked={notificationEnabled}
              onChange={e => setNotificationEnabled(e.target.checked)}
            />
            <label htmlFor="notificationEnabled" className="checkbox-label">Invia notifica</label>
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
                  <div className="modal-form-group">
                    <label className="modal-label">Modalità</label>
                    <select
                      className="modal-select"
                      value={cloneMode}
                      onChange={e => setCloneMode(e.target.value as 'daily' | 'weekly')}
                    >
                      <option value="daily">Giornaliera (un clone per ogni giorno nel range)</option>
                      <option value="weekly">Settimanale (clona intera settimana del turno su N settimane)</option>
                    </select>
                  </div>
                  {cloneMode === 'daily' && (
                    <>
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
                    </>
                  )}
                  {cloneMode === 'weekly' && (
                    <div className="modal-row">
                      <div className="modal-form-group">
                        <label className="modal-label">Settimana target (lun.)</label>
                        <input
                          type="date"
                          className="modal-input"
                          value={cloneTargetWeek}
                          onChange={e => setCloneTargetWeek(e.target.value)}
                        />
                      </div>
                      <div className="modal-form-group">
                        <label className="modal-label">N. settimane</label>
                        <input
                          type="number"
                          className="modal-input"
                          min={1}
                          max={52}
                          value={cloneWeeks}
                          onChange={e => setCloneWeeks(Math.max(1, Math.min(52, Number(e.target.value) || 1)))}
                        />
                      </div>
                    </div>
                  )}
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
