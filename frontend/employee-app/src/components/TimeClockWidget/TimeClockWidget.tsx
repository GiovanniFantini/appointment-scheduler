import { useState, useEffect, useCallback } from 'react'
import { timeClockApi, getBrowserLocation } from '../../lib/api/timeClock'
import type { CurrentClockStatusDto, ClockActionResultDto } from '../../types/timbratura'
import './TimeClockWidget.css'

interface Props {
  /** Compatto = versione ridotta per la Dashboard. */
  compact?: boolean
  /** Notifica al parent quando lo stato cambia (es. per ricaricare lo storico). */
  onStatusChange?: (status: CurrentClockStatusDto) => void
}

type ActionKind = 'clock-in' | 'clock-out' | 'break-start' | 'break-end'

function formatClock(iso?: string): string {
  if (!iso) return '--:--'
  return new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = Math.round(minutes % 60)
  if (h === 0) return `${m} min`
  return `${h}h ${m.toString().padStart(2, '0')}m`
}

/**
 * Widget di timbratura: mostra lo stato corrente e i pulsanti per timbrare.
 * Usato sia nella TimbraturaPage che (compatto) nella Dashboard.
 */
export default function TimeClockWidget({ compact = false, onStatusChange }: Props) {
  const [status, setStatus] = useState<CurrentClockStatusDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(false)
  const [error, setError] = useState('')
  const [feedback, setFeedback] = useState('')
  const [feedbackIsWarning, setFeedbackIsWarning] = useState(false)

  const loadStatus = useCallback(async () => {
    try {
      const data = await timeClockApi.getStatus()
      setStatus(data)
      onStatusChange?.(data)
    } catch (err: unknown) {
      const e = err as { response?: { status?: number } }
      if (e.response?.status === 403) setError('La timbratura non è abilitata per il tuo ruolo.')
      else setError('Impossibile caricare lo stato della timbratura.')
    } finally {
      setLoading(false)
    }
  }, [onStatusChange])

  useEffect(() => { loadStatus() }, [loadStatus])

  const handleAction = async (kind: ActionKind) => {
    if (!status) return
    setWorking(true)
    setError('')
    setFeedback('')
    setFeedbackIsWarning(false)
    try {
      // La posizione si chiede solo al momento dell'azione, non all'apertura.
      const location = status.requiresGeolocation ? await getBrowserLocation() : {}
      const payload = {
        eventParticipantId: status.currentShift?.eventParticipantId,
        ...location,
      }
      let result: ClockActionResultDto
      if (kind === 'clock-in') result = await timeClockApi.clockIn(payload)
      else if (kind === 'clock-out') result = await timeClockApi.clockOut(payload)
      else if (kind === 'break-start') result = await timeClockApi.startBreak(payload)
      else result = await timeClockApi.endBreak(payload)

      setStatus(result.status)
      onStatusChange?.(result.status)
      setFeedback(result.message)
      setFeedbackIsWarning(result.hasAnomaly)
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante la timbratura.')
    } finally {
      setWorking(false)
    }
  }

  if (loading) {
    // In Dashboard non mostrare lo skeleton: il widget potrebbe poi nascondersi.
    if (compact) return null
    return <div className="tcw-card tcw-card--loading"><div className="tcw-spinner" /></div>
  }

  if (error && !status) {
    // In Dashboard un errore di caricamento non deve lasciare un riquadro rosso.
    if (compact) return null
    return <div className="tcw-card tcw-error">{error}</div>
  }

  if (!status) return null

  if (!status.timeClockEnabled || !status.currentShift) {
    if (compact) return null
    return (
      <div className="tcw-card tcw-card--idle">
        <div className="tcw-idle-icon">⏱</div>
        <p className="tcw-idle-text">{status.statusMessage}</p>
      </div>
    )
  }

  const shift = status.currentShift
  const isCompleted = !status.isClockedIn && status.todayEntries.some(e => e.type === 2)

  // Dashboard: mostra il widget solo quando è davvero utile timbrare —
  // nell'intorno del turno (showClockPrompt) o con un turno già in corso.
  // Se il turno è lontano o già concluso, non ingombra la Dashboard.
  if (compact && !status.showClockPrompt && !status.isClockedIn) {
    return null
  }

  // Pulsante primario contestuale.
  let primaryKind: ActionKind | null = null
  let primaryLabel = ''
  if (!status.isClockedIn && !isCompleted) { primaryKind = 'clock-in'; primaryLabel = 'Timbra entrata' }
  else if (status.isClockedIn && !status.isOnBreak) { primaryKind = 'clock-out'; primaryLabel = 'Timbra uscita' }
  else if (status.isOnBreak) { primaryKind = 'break-end'; primaryLabel = 'Termina pausa' }

  return (
    <div className={`tcw-card ${compact ? 'tcw-card--compact' : ''} ${status.isOnBreak ? 'tcw-card--break' : status.isClockedIn ? 'tcw-card--active' : ''}`}>
      <div className="tcw-header">
        <div>
          <div className="tcw-shift-title">{shift.title}</div>
          <div className="tcw-shift-meta">
            {shift.branchName}
            {shift.startTime && ` · ${shift.startTime.substring(0, 5)}`}
            {shift.endTime && `–${shift.endTime.substring(0, 5)}`}
          </div>
        </div>
        <span className={`tcw-status-pill ${status.isOnBreak ? 'pill-break' : status.isClockedIn ? 'pill-active' : isCompleted ? 'pill-done' : 'pill-idle'}`}>
          {status.statusMessage}
        </span>
      </div>

      {!compact && (
        <div className="tcw-info-row">
          <div className="tcw-info">
            <span className="tcw-info-label">Entrata</span>
            <span className="tcw-info-value">{formatClock(status.clockInAtUtc)}</span>
          </div>
          <div className="tcw-info">
            <span className="tcw-info-label">Ore lavorate</span>
            <span className="tcw-info-value">{formatDuration(status.workedMinutesToday)}</span>
          </div>
        </div>
      )}

      {feedback && (
        <div className={`tcw-feedback ${feedbackIsWarning ? 'tcw-feedback--warning' : ''}`}>
          {feedback}
        </div>
      )}
      {error && <div className="tcw-feedback tcw-feedback--error">{error}</div>}

      <div className="tcw-actions">
        {primaryKind && (
          <button
            className={`tcw-btn tcw-btn--primary ${primaryKind === 'clock-out' ? 'tcw-btn--out' : ''}`}
            disabled={working}
            onClick={() => handleAction(primaryKind!)}
          >
            {working ? 'Attendere…' : primaryLabel}
          </button>
        )}
        {/* Pausa: solo mentre si è in turno e non già in pausa */}
        {status.isClockedIn && !status.isOnBreak && (
          <button
            className="tcw-btn tcw-btn--secondary"
            disabled={working}
            onClick={() => handleAction('break-start')}
          >
            Inizia pausa
          </button>
        )}
        {isCompleted && !primaryKind && (
          <div className="tcw-completed">Turno completato — {formatDuration(status.workedMinutesToday)}</div>
        )}
      </div>
    </div>
  )
}
