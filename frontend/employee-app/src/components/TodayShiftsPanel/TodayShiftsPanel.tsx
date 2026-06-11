import { useState, useEffect, useCallback } from 'react'
import { timeClockApi, getBrowserLocation } from '../../lib/api/timeClock'
import type { TodayShiftsDto, ShiftClockStatusDto } from '../../types/timbratura'
import './TodayShiftsPanel.css'

interface Props {
  /** Notifica al parent dopo una timbratura (es. per ricaricare storico/anomalie). */
  onChange?: () => void
}

type ActionKind = 'clock-in' | 'clock-out' | 'break-start' | 'break-end'

function formatClock(iso?: string): string {
  if (!iso) return '--:--'
  return new Date(iso).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })
}

function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = Math.round(minutes % 60)
  if (h === 0) return `${m} min`
  return `${h}h ${m.toString().padStart(2, '0')}m`
}

function shiftHours(s: ShiftClockStatusDto): string {
  const { startTime, endTime } = s.shift
  if (!startTime) return ''
  const start = startTime.substring(0, 5)
  return endTime ? `${start}–${endTime.substring(0, 5)}` : start
}

/**
 * Pagina Timbratura: elenca tutti i turni timbrabili della giornata. Una sola
 * card è "attiva" (con i pulsanti) — il turno su cui ha senso agire ora — così
 * con più turni nello stesso giorno l'utente vede sempre su quale sta timbrando
 * e non può sbagliare turno. L'azione invia sempre l'eventParticipantId del
 * turno della card, mai un turno indovinato dal server.
 */
export default function TodayShiftsPanel({ onChange }: Props) {
  const [data, setData] = useState<TodayShiftsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [workingId, setWorkingId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [feedback, setFeedback] = useState<{ text: string; warning: boolean } | null>(null)

  const load = useCallback(async () => {
    try {
      const res = await timeClockApi.getTodayShifts()
      setData(res)
    } catch (err: unknown) {
      const e = err as { response?: { status?: number } }
      if (e.response?.status === 403) setError('La timbratura non è abilitata per il tuo ruolo.')
      else setError('Impossibile caricare i turni di oggi.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const handleAction = async (s: ShiftClockStatusDto, kind: ActionKind) => {
    if (!data) return
    setWorkingId(s.shift.eventParticipantId)
    setError('')
    setFeedback(null)
    try {
      const location = data.requiresGeolocation ? await getBrowserLocation() : {}
      const payload = { eventParticipantId: s.shift.eventParticipantId, ...location }

      let result
      if (kind === 'clock-in') result = await timeClockApi.clockIn(payload)
      else if (kind === 'clock-out') result = await timeClockApi.clockOut(payload)
      else if (kind === 'break-start') result = await timeClockApi.startBreak(payload)
      else result = await timeClockApi.endBreak(payload)

      setFeedback({ text: result.message, warning: result.hasAnomaly })
      await load()
      onChange?.()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante la timbratura.')
    } finally {
      setWorkingId(null)
    }
  }

  if (loading) {
    return <div className="tsp-card tsp-card--loading"><div className="tcw-spinner" /></div>
  }

  if (error && !data) {
    return <div className="tsp-card tsp-error">{error}</div>
  }

  if (!data || data.shifts.length === 0) {
    return (
      <div className="tsp-card tsp-card--idle">
        <div className="tsp-idle-icon">⏱</div>
        <p className="tsp-idle-text">Nessun turno in programma per oggi.</p>
      </div>
    )
  }

  if (!data.timeClockEnabled) {
    return (
      <div className="tsp-card tsp-card--idle">
        <div className="tsp-idle-icon">⏱</div>
        <p className="tsp-idle-text">La timbratura non è attiva per la tua filiale.</p>
      </div>
    )
  }

  return (
    <div className="tsp-list">
      {feedback && (
        <div className={`tcw-feedback ${feedback.warning ? 'tcw-feedback--warning' : ''}`}>
          {feedback.text}
        </div>
      )}
      {error && <div className="tcw-feedback tcw-feedback--error">{error}</div>}

      {data.shifts.map(s => (
        <ShiftCard
          key={s.shift.eventParticipantId}
          status={s}
          busy={workingId === s.shift.eventParticipantId}
          anyBusy={workingId !== null}
          onAction={kind => handleAction(s, kind)}
        />
      ))}
    </div>
  )
}

function ShiftCard({
  status, busy, anyBusy, onAction,
}: {
  status: ShiftClockStatusDto
  busy: boolean
  anyBusy: boolean
  onAction: (kind: ActionKind) => void
}) {
  const { shift } = status
  const pillClass = status.isOnBreak ? 'pill-break'
    : status.isClockedIn ? 'pill-active'
    : status.isCompleted ? 'pill-done'
    : status.isExpired ? 'pill-expired' : 'pill-idle'

  const cardClass = status.isExpired ? 'tsp-shift--expired'
    : !status.isActive ? 'tsp-shift--muted'
    : status.isOnBreak ? 'tsp-shift--break'
    : status.isClockedIn ? 'tsp-shift--active' : ''

  return (
    <div className={`tsp-shift ${cardClass}`}>
      <div className="tsp-shift-head">
        <div>
          <div className="tsp-shift-title">{shift.title}</div>
          <div className="tsp-shift-meta">
            {shift.branchName}{shiftHours(status) && ` · ${shiftHours(status)}`}
          </div>
        </div>
        <span className={`tcw-status-pill ${pillClass}`}>{status.statusMessage}</span>
      </div>

      {/* Dettaglio orari solo per il turno attivo o già avviato. */}
      {(status.isActive || status.isClockedIn || status.isCompleted) && (
        <div className="tsp-info-row">
          <div className="tsp-info">
            <span className="tsp-info-label">Entrata</span>
            <span className="tsp-info-value">{formatClock(status.clockInAtUtc)}</span>
          </div>
          <div className="tsp-info">
            <span className="tsp-info-label">Ore lavorate</span>
            <span className="tsp-info-value">{formatDuration(status.workedMinutes)}</span>
          </div>
        </div>
      )}

      {status.isActive ? (
        <div className="tsp-actions">
          {!status.isClockedIn && !status.isCompleted && (
            <button className="tcw-btn tcw-btn--primary" disabled={anyBusy} onClick={() => onAction('clock-in')}>
              {busy ? 'Attendere…' : 'Timbra entrata'}
            </button>
          )}
          {status.isClockedIn && !status.isOnBreak && (
            <>
              <button className="tcw-btn tcw-btn--primary tcw-btn--out" disabled={anyBusy} onClick={() => onAction('clock-out')}>
                {busy ? 'Attendere…' : 'Timbra uscita'}
              </button>
              <button className="tcw-btn tcw-btn--secondary" disabled={anyBusy} onClick={() => onAction('break-start')}>
                Inizia pausa
              </button>
            </>
          )}
          {status.isOnBreak && (
            <button className="tcw-btn tcw-btn--primary" disabled={anyBusy} onClick={() => onAction('break-end')}>
              {busy ? 'Attendere…' : 'Termina pausa'}
            </button>
          )}
        </div>
      ) : status.isCompleted ? (
        <div className="tsp-completed">Turno completato — {formatDuration(status.workedMinutes)}</div>
      ) : status.isExpired ? (
        <div className="tsp-expired">Mancata entrata — verrà segnalata come anomalia.</div>
      ) : null}
    </div>
  )
}
