import { useState, useEffect, useMemo } from 'react'
import apiClient from '../../lib/axios'
import { skillsApi, Skill, SuggestedEmployee } from '../../lib/api/skills'
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

export interface ParticipantSkillInput {
  employeeId: number
  skillId?: number | null
}

export interface RequiredSkillInput {
  skillId: number
  quantity: number
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
  participantSkills?: ParticipantSkillInput[]
  requiredSkills?: RequiredSkillInput[]
  recurrence?: RecurrenceType
  notificationEnabled?: boolean
  notes?: string
}

interface Employee {
  id: number
  firstName: string
  lastName: string
  skills?: { skillId: number; skillName: string; skillColor: string }[]
}

interface ShiftConflictDto {
  employeeId: number
  employeeFullName: string
  date: string
  kind: number
  kindName: 'LeaveOverlap' | 'ShiftOverlap' | 'SkillMismatch' | string
  requestId?: number
  requestType?: number
  conflictingEventId?: number
  conflictingEventTitle?: string
  conflictStart?: string
  conflictEnd?: string
  skillId?: number
  skillName?: string
  message: string
}

interface ApiErrorResponse {
  message?: string
  errors?: Record<string, string[]>
  title?: string
}

interface EventModalProps {
  event: Partial<CalEvent> | null
  defaultDate?: string
  onClose: () => void
  onSaved: () => void
}

const EVENT_TYPES: EventType[] = ['Turno', 'ChiusuraAziendale', 'Ferie', 'Permessi', 'Malattia']
const RECURRENCE_TYPES: RecurrenceType[] = ['Nessuna', 'Giornaliera', 'Settimanale', 'Mensile']

function toApiTime(value?: string): string | undefined {
  if (!value) return undefined
  // Browser time input returns HH:mm, while backend TimeOnly expects HH:mm:ss
  if (/^\d{2}:\d{2}$/.test(value)) return `${value}:00`
  return value
}

function getApiErrorMessage(data: ApiErrorResponse | undefined): string | undefined {
  if (!data) return undefined
  if (data.message) return data.message
  if (data.errors) {
    const first = Object.values(data.errors).flat()[0]
    if (first) return first
  }
  if (data.title) return data.title
  return undefined
}

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
  const [requiredSkills, setRequiredSkills] = useState<RequiredSkillInput[]>(event?.requiredSkills ?? [])
  const [participantSkills, setParticipantSkills] = useState<ParticipantSkillInput[]>(event?.participantSkills ?? [])
  const [step, setStep] = useState<1 | 2 | 3>(1)
  const [repeatWeekly, setRepeatWeekly] = useState(false)
  const [repeatUntil, setRepeatUntil] = useState('')
  const [suggestedBySkill, setSuggestedBySkill] = useState<Record<number, SuggestedEmployee[]>>({})

  const [employees, setEmployees] = useState<Employee[]>([])
  const [skills, setSkills] = useState<Skill[]>([])
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
    skillsApi.list().then(list => setSkills(list.filter(s => s.isActive))).catch(() => {})
  }, [])

  // Inizializza repeatWeekly da una recurrence preesistente (formato semplice: "WEEKLY;UNTIL=YYYY-MM-DD")
  useEffect(() => {
    const r = event?.recurrence
    if (typeof r === 'string' && r.startsWith('WEEKLY')) {
      setRepeatWeekly(true)
      const m = r.match(/UNTIL=(\d{4}-\d{2}-\d{2})/)
      if (m) setRepeatUntil(m[1])
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
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

  const setParticipantSkill = (employeeId: number, skillId: number | null) => {
    setParticipantSkills(prev => {
      const existing = prev.find(p => p.employeeId === employeeId)
      if (existing) {
        return prev.map(p => p.employeeId === employeeId ? { ...p, skillId } : p)
      }
      return [...prev, { employeeId, skillId }]
    })
  }

  const getParticipantSkillId = (employeeId: number): number | null => {
    const p = participantSkills.find(x => x.employeeId === employeeId)
    return p?.skillId ?? null
  }

  const addRequiredSkill = (skillId: number) => {
    setRequiredSkills(prev => {
      if (prev.some(r => r.skillId === skillId)) return prev
      return [...prev, { skillId, quantity: 1 }]
    })
  }

  const updateRequiredQuantity = (skillId: number, delta: number) => {
    setRequiredSkills(prev =>
      prev
        .map(r => r.skillId === skillId ? { ...r, quantity: Math.max(0, r.quantity + delta) } : r)
        .filter(r => r.quantity > 0)
    )
  }

  const removeRequiredSkill = (skillId: number) => {
    setRequiredSkills(prev => prev.filter(r => r.skillId !== skillId))
  }

  // Conteggio copertura per mansione (in tempo reale)
  const coverageBySkill = useMemo(() => {
    const map: Record<number, number> = {}
    for (const p of participantSkills) {
      if (p.skillId != null) map[p.skillId] = (map[p.skillId] ?? 0) + 1
    }
    return map
  }, [participantSkills])

  // Fetch suggerimenti per ogni mansione richiesta quando cambiano data/orari/fabbisogno
  useEffect(() => {
    if (eventType !== 'Turno' || step !== 3) return
    if (!startDate) return
    const target: Record<number, SuggestedEmployee[]> = {}
    let cancelled = false
    Promise.all(
      requiredSkills.map(rs =>
        skillsApi.getSuggested(rs.skillId, {
          date: startDate,
          startTime: isAllDay ? undefined : toApiTime(startTime),
          endTime: isAllDay ? undefined : toApiTime(endTime),
          excludeEventId: event?.id,
        }).then(list => { target[rs.skillId] = list })
         .catch(() => { target[rs.skillId] = [] })
      )
    ).then(() => {
      if (!cancelled) setSuggestedBySkill(target)
    })
    return () => { cancelled = true }
  }, [step, requiredSkills, startDate, startTime, endTime, isAllDay, eventType, event?.id])

  const computeRecurrence = (): string | undefined => {
    if (repeatWeekly && repeatUntil) return `WEEKLY;UNTIL=${repeatUntil}`
    return recurrence === 'Nessuna' ? undefined : recurrence
  }

  const buildPayload = () => ({
    title,
    eventType: EVENT_TYPE_VALUES[eventType],
    isAllDay,
    startDate,
    endDate: endDate || startDate,
    startTime: isAllDay ? undefined : toApiTime(startTime),
    endTime: isAllDay ? undefined : toApiTime(endTime),
    isOnCall: eventType === 'Turno' ? isOnCall : false,
    ownerEmployeeIds: selectedOwnerIds,
    coOwnerEmployeeIds: [],
    participantOverrides: participantOverrides
      .filter(o => o.startTimeOverride || o.endTimeOverride || o.participantNotes)
      .map(o => ({
        employeeId: o.employeeId,
        startTimeOverride: toApiTime(o.startTimeOverride),
        endTimeOverride: toApiTime(o.endTimeOverride),
        participantNotes: o.participantNotes || undefined,
      })),
    requiredSkills: eventType === 'Turno'
      ? requiredSkills.filter(r => r.quantity > 0)
      : [],
    participantSkills: eventType === 'Turno'
      ? participantSkills
          .filter(p => selectedOwnerIds.includes(p.employeeId) && p.skillId != null)
          .map(p => ({ employeeId: p.employeeId, skillId: p.skillId }))
      : [],
    recurrence: computeRecurrence(),
    notificationEnabled,
    notes,
  })

  const handleSave = async () => {
    if (!title.trim()) { setError('Il titolo è obbligatorio'); return }
    if (!startDate) { setError('La data di inizio è obbligatoria'); return }
    if (!endDate) { setError('La data di fine è obbligatoria'); return }
    if (endDate < startDate) { setError('La data di fine non può essere precedente alla data di inizio'); return }

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
      const e = err as { response?: { data?: ApiErrorResponse } }
      setError(getApiErrorMessage(e.response?.data) ?? 'Errore durante il salvataggio')
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
          <h2 className="modal-title">{isEdit ? 'Modifica Turno' : 'Nuovo Turno'}</h2>
          <button className="modal-close-btn" onClick={onClose}>✕</button>
        </div>

        {eventType === 'Turno' && (
          <div className="wizard-stepper">
            <div className={`step ${step === 1 ? 'active' : ''} ${step > 1 ? 'done' : ''}`} onClick={() => setStep(1)}>
              <span className="step-num">1</span><span className="step-label">Quando</span>
            </div>
            <div className="step-sep" />
            <div className={`step ${step === 2 ? 'active' : ''} ${step > 2 ? 'done' : ''}`} onClick={() => setStep(2)}>
              <span className="step-num">2</span><span className="step-label">Cosa serve</span>
            </div>
            <div className="step-sep" />
            <div className={`step ${step === 3 ? 'active' : ''}`} onClick={() => setStep(3)}>
              <span className="step-num">3</span><span className="step-label">Chi</span>
            </div>
          </div>
        )}

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

          {(eventType !== 'Turno' || step === 1) && (
            <>
              <div className="modal-form-group">
                <label className="modal-label">Titolo</label>
                <input
                  type="text"
                  className="modal-input"
                  placeholder={eventType === 'Turno' ? 'Es. Mattina' : 'Inserisci titolo'}
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
                  <label className="modal-label">Data fine *</label>
                  <input type="date" className="modal-input" value={endDate} min={startDate || undefined} onChange={e => setEndDate(e.target.value)} />
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
                <>
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
                  <div className="repeat-block">
                    <div className="checkbox-group">
                      <input
                        type="checkbox"
                        id="repeatWeekly"
                        className="modal-checkbox"
                        checked={repeatWeekly}
                        onChange={e => setRepeatWeekly(e.target.checked)}
                      />
                      <label htmlFor="repeatWeekly" className="checkbox-label">Ripeti ogni settimana</label>
                    </div>
                    {repeatWeekly && (
                      <div className="modal-form-group">
                        <label className="modal-label">Fino al</label>
                        <input
                          type="date"
                          className="modal-input"
                          min={startDate || undefined}
                          value={repeatUntil}
                          onChange={e => setRepeatUntil(e.target.value)}
                        />
                      </div>
                    )}
                  </div>
                </>
              )}
            </>
          )}

          {eventType === 'Turno' && step === 2 && (
            <div className="wizard-step-content">
              {skills.length === 0 ? (
                <div className="step-empty">
                  <div className="step-empty-icon">🏷</div>
                  <div className="step-empty-title">Nessuna mansione configurata</div>
                  <p className="step-empty-text">
                    Le mansioni servono a dichiarare di cosa ha bisogno il turno (es. 1 Cassiere + 1 Repartista).
                    Sono opzionali: se non ne hai bisogno, salta questo passaggio.
                  </p>
                  <a className="link-button" href="/mansioni" target="_blank" rel="noreferrer">+ Crea una mansione</a>
                </div>
              ) : (
                <>
                  <p className="wizard-hint">
                    Dichiara di cosa hai bisogno per questo turno. Se non ti serve, vai avanti senza scegliere nulla.
                  </p>
                  <div className="skill-picker">
                    {skills.map(s => {
                      const already = requiredSkills.some(r => r.skillId === s.id)
                      if (already) return null
                      return (
                        <button
                          key={s.id}
                          type="button"
                          className="skill-pick-chip"
                          style={{ borderColor: s.color + '88', color: s.color }}
                          onClick={() => addRequiredSkill(s.id)}
                        >+ {s.name}</button>
                      )
                    })}
                  </div>
                  {requiredSkills.length > 0 && (
                    <ul className="required-list">
                      {requiredSkills.map(rs => {
                        const s = skills.find(x => x.id === rs.skillId)
                        if (!s) return null
                        return (
                          <li key={rs.skillId} className="required-row" style={{ borderColor: s.color + '55' }}>
                            <span className="required-badge" style={{ background: s.color }}>{s.name.charAt(0)}</span>
                            <span className="required-name">{s.name}</span>
                            <div className="qty-control">
                              <button type="button" onClick={() => updateRequiredQuantity(rs.skillId, -1)}>−</button>
                              <span className="qty-value">{rs.quantity}</span>
                              <button type="button" onClick={() => updateRequiredQuantity(rs.skillId, +1)}>+</button>
                            </div>
                            <button type="button" className="required-remove" onClick={() => removeRequiredSkill(rs.skillId)}>✕</button>
                          </li>
                        )
                      })}
                    </ul>
                  )}
                </>
              )}
            </div>
          )}

          {eventType === 'Turno' && step === 3 && (
            <div className="wizard-step-content">
              {requiredSkills.length > 0 ? (
                <>
                  {requiredSkills.map(rs => {
                    const s = skills.find(x => x.id === rs.skillId)
                    if (!s) return null
                    const covered = coverageBySkill[rs.skillId] ?? 0
                    const isCovered = covered >= rs.quantity
                    const suggested = suggestedBySkill[rs.skillId] ?? []
                    return (
                      <div key={rs.skillId} className="coverage-group">
                        <div className="coverage-header" style={{ borderColor: s.color }}>
                          <span className="required-badge sm" style={{ background: s.color }}>{s.name.charAt(0)}</span>
                          <span className="coverage-name">{s.name}</span>
                          <span className={`coverage-status ${isCovered ? 'ok' : 'warn'}`}>
                            {covered}/{rs.quantity} {isCovered ? '✓' : '⚠'}
                          </span>
                        </div>
                        <ul className="suggested-list">
                          {suggested.length === 0 && (
                            <li className="suggested-empty">Nessun dipendente con questa mansione.</li>
                          )}
                          {suggested.map(emp => {
                            const isSelected = selectedOwnerIds.includes(emp.employeeId)
                            const currentSkill = getParticipantSkillId(emp.employeeId)
                            const isCoveringThis = isSelected && currentSkill === rs.skillId
                            return (
                              <li key={emp.employeeId} className={`suggested-row ${!emp.isAvailable ? 'unavailable' : ''}`}>
                                <div className="suggested-info">
                                  <span className="suggested-name">{emp.fullName}</span>
                                  {!emp.isAvailable && emp.unavailableReason && (
                                    <span className="suggested-reason">{emp.unavailableReason}</span>
                                  )}
                                </div>
                                <button
                                  type="button"
                                  className={`suggested-add ${isCoveringThis ? 'added' : ''}`}
                                  disabled={!emp.isAvailable && !isCoveringThis}
                                  onClick={() => {
                                    if (isCoveringThis) {
                                      // Toggle off: rimuovi mansione e dal partecipante se solo qui
                                      setParticipantSkill(emp.employeeId, null)
                                      setSelectedOwnerIds(prev => prev.filter(id => id !== emp.employeeId))
                                    } else {
                                      if (!isSelected) {
                                        setSelectedOwnerIds(prev => [...prev, emp.employeeId])
                                      }
                                      setParticipantSkill(emp.employeeId, rs.skillId)
                                    }
                                  }}
                                >{isCoveringThis ? '✓ Aggiunto' : 'Aggiungi'}</button>
                              </li>
                            )
                          })}
                        </ul>
                      </div>
                    )
                  })}
                </>
              ) : (
                <>
                  <p className="wizard-hint">Seleziona i partecipanti del turno.</p>
                </>
              )}
            </div>
          )}

          {(
            (eventType !== 'Turno' && employees.length > 0) ||
            (eventType === 'Turno' && step === 3 && requiredSkills.length === 0 && employees.length > 0)
          ) && (
            <div className="modal-form-group">
              <label className="modal-label">
                {eventType === 'Turno' ? 'Partecipanti' : 'Persone coinvolte'} (Ctrl+click per selezione multipla)
              </label>
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

          {eventType === 'Turno' && step === 3 && selectedOwnerIds.length > 0 && !isAllDay && (
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

          {eventType === 'Turno' && step === 3 && showOverrides && !isAllDay && selectedOwnerIds.length > 0 && (
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

          {(eventType !== 'Turno' || step === 3) && (
            <>
              {eventType !== 'Turno' && (
                <div className="modal-form-group">
                  <label className="modal-label">Ricorrenza</label>
                  <select className="modal-select" value={recurrence} onChange={e => setRecurrence(e.target.value as RecurrenceType)}>
                    {RECURRENCE_TYPES.map(r => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                </div>
              )}

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
            </>
          )}

          {isEdit && (eventType !== 'Turno' || step === 3) && (
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
            {eventType === 'Turno' && step > 1 && (
              <button className="btn-cancel" onClick={() => setStep(prev => (prev > 1 ? (prev - 1) as 1 | 2 | 3 : prev))}>
                ← Indietro
              </button>
            )}
            <button className="btn-cancel" onClick={onClose}>Annulla</button>
            {eventType === 'Turno' && step < 3 ? (
              <button
                className="btn-save"
                onClick={() => {
                  if (step === 1) {
                    if (!startDate) { setError('La data di inizio è obbligatoria'); return }
                    if (!endDate) setEndDate(startDate)
                  }
                  setError('')
                  setStep(prev => (prev + 1) as 1 | 2 | 3)
                }}
              >
                {step === 2 && requiredSkills.length === 0 ? 'Salta →' : 'Avanti →'}
              </button>
            ) : (
              <button className="btn-save" onClick={handleSave} disabled={loading}>
                {loading ? 'Salvataggio...' : 'Salva'}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
