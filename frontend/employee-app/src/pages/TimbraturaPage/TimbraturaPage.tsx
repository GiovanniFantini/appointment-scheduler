import { useState, useEffect, useCallback } from 'react'
import TimeClockWidget from '../../components/TimeClockWidget/TimeClockWidget'
import JustifyAnomalyModal from '../../components/JustifyAnomalyModal/JustifyAnomalyModal'
import { timeClockApi } from '../../lib/api/timeClock'
import { TimeEntryType, TimeClockAnomalyStatus } from '../../types/timbratura'
import type { TimeEntryDto, TimeClockAnomalyDto, WellbeingStatsDto } from '../../types/timbratura'
import './TimbraturaPage.css'

function formatHours(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = Math.round(minutes % 60)
  return h > 0 ? `${h}h ${m.toString().padStart(2, '0')}m` : `${m}m`
}

const ANOMALY_STATUS_LABEL: Record<number, string> = {
  [TimeClockAnomalyStatus.Open]: 'Da giustificare',
  [TimeClockAnomalyStatus.Justified]: 'In revisione',
  [TimeClockAnomalyStatus.Approved]: 'Approvata',
  [TimeClockAnomalyStatus.Rejected]: 'Respinta',
}

function entryLabel(type: TimeEntryType): string {
  switch (type) {
    case TimeEntryType.ClockIn: return 'Entrata'
    case TimeEntryType.ClockOut: return 'Uscita'
    case TimeEntryType.BreakStart: return 'Inizio pausa'
    case TimeEntryType.BreakEnd: return 'Fine pausa'
    default: return '—'
  }
}

function entryColor(type: TimeEntryType): string {
  switch (type) {
    case TimeEntryType.ClockIn: return '#22c55e'
    case TimeEntryType.ClockOut: return '#ef4444'
    case TimeEntryType.BreakStart:
    case TimeEntryType.BreakEnd: return '#f59e0b'
    default: return '#6366f1'
  }
}

function formatTimestamp(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    day: '2-digit', month: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

export default function TimbraturaPage() {
  const [entries, setEntries] = useState<TimeEntryDto[]>([])
  const [loadingHistory, setLoadingHistory] = useState(true)
  const [anomalies, setAnomalies] = useState<TimeClockAnomalyDto[]>([])
  const [justifying, setJustifying] = useState<TimeClockAnomalyDto | null>(null)
  const [wellbeing, setWellbeing] = useState<WellbeingStatsDto | null>(null)

  const loadHistory = useCallback(async () => {
    setLoadingHistory(true)
    try {
      const [entriesData, anomaliesData, wellbeingData] = await Promise.all([
        timeClockApi.getMyEntries(),
        timeClockApi.getMyAnomalies(),
        timeClockApi.getWellbeing(),
      ])
      setEntries(Array.isArray(entriesData) ? entriesData : [])
      setAnomalies(Array.isArray(anomaliesData) ? anomaliesData : [])
      setWellbeing(wellbeingData ?? null)
    } catch {
      setEntries([])
      setAnomalies([])
      setWellbeing(null)
    } finally {
      setLoadingHistory(false)
    }
  }, [])

  useEffect(() => { loadHistory() }, [loadHistory])

  const handleJustified = (updated: TimeClockAnomalyDto) => {
    setAnomalies(prev => prev.map(a => (a.id === updated.id ? updated : a)))
    setJustifying(null)
  }

  // Raggruppa le timbrature per giornata di lavoro.
  const grouped = entries.reduce<Record<string, TimeEntryDto[]>>((acc, e) => {
    (acc[e.workDate] ??= []).push(e)
    return acc
  }, {})
  const days = Object.keys(grouped).sort((a, b) => b.localeCompare(a))

  return (
    <div className="timbratura-page">
      <div className="tp-header">
        <h1 className="tp-title">Timbratura</h1>
        <p className="tp-subtitle">Registra entrata, uscita e pause del tuo turno</p>
      </div>

      <TimeClockWidget onStatusChange={loadHistory} />

      {wellbeing && (
        <div className="tp-wellbeing">
          {wellbeing.hasWellbeingAlert && wellbeing.wellbeingMessage && (
            <div className="tp-wellbeing-alert">⚠ {wellbeing.wellbeingMessage}</div>
          )}
          <div className="tp-wellbeing-stats">
            <div className="tp-stat">
              <span className="tp-stat-value">{formatHours(wellbeing.workedMinutesThisWeek)}</span>
              <span className="tp-stat-label">Questa settimana</span>
            </div>
            <div className="tp-stat">
              <span className="tp-stat-value">{formatHours(wellbeing.workedMinutesThisMonth)}</span>
              <span className="tp-stat-label">Questo mese</span>
            </div>
            <div className="tp-stat">
              <span className="tp-stat-value">{formatHours(wellbeing.overtimeMinutesThisMonth)}</span>
              <span className="tp-stat-label">Straordinari mese</span>
            </div>
          </div>
        </div>
      )}

      {anomalies.length > 0 && (
        <div className="tp-anomalies">
          <h2 className="tp-history-title">Le mie anomalie</h2>
          <div className="tp-anomaly-list">
            {anomalies.map(a => (
              <div key={a.id} className="tp-anomaly" data-status={a.status}>
                <div className="tp-anomaly-main">
                  <span className="tp-anomaly-type">{a.typeName}</span>
                  <span className={`tp-anomaly-status status-${a.status}`}>
                    {ANOMALY_STATUS_LABEL[a.status] ?? a.statusName}
                  </span>
                </div>
                <div className="tp-anomaly-meta">
                  <span>
                    {new Date(a.workDate).toLocaleDateString(undefined, {
                      day: '2-digit', month: 'long',
                    })}
                  </span>
                  {a.deviationMinutes != null && (
                    <span>{a.deviationMinutes > 0 ? '+' : ''}{a.deviationMinutes} min</span>
                  )}
                </div>
                {a.employeeNotes && <div className="tp-anomaly-notes">"{a.employeeNotes}"</div>}
                {a.reviewNotes && (
                  <div className="tp-anomaly-review">Risposta responsabile: {a.reviewNotes}</div>
                )}
                {a.status === TimeClockAnomalyStatus.Open && (
                  <button className="tp-justify-btn" onClick={() => setJustifying(a)}>
                    Giustifica
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="tp-history">
        <h2 className="tp-history-title">Le mie timbrature</h2>
        {loadingHistory ? (
          <div className="tp-loading"><div className="tp-spinner" /></div>
        ) : days.length === 0 ? (
          <div className="tp-empty">Nessuna timbratura registrata negli ultimi 30 giorni.</div>
        ) : (
          <div className="tp-day-list">
            {days.map(day => (
              <div key={day} className="tp-day">
                <div className="tp-day-label">
                  {new Date(day).toLocaleDateString(undefined, {
                    weekday: 'long', day: '2-digit', month: 'long',
                  })}
                </div>
                <div className="tp-entry-list">
                  {grouped[day]
                    .slice()
                    .sort((a, b) => a.actualTimestampUtc.localeCompare(b.actualTimestampUtc))
                    .map(e => (
                      <div key={e.id} className="tp-entry" style={{ borderLeftColor: entryColor(e.type) }}>
                        <div className="tp-entry-main">
                          <span className="tp-entry-type" style={{ color: entryColor(e.type) }}>
                            {entryLabel(e.type)}
                          </span>
                          <span className="tp-entry-time">{formatTimestamp(e.actualTimestampUtc)}</span>
                        </div>
                        <div className="tp-entry-meta">
                          <span>{e.eventTitle}</span>
                          {e.isManualCorrection && <span className="tp-badge tp-badge--manual">Correzione</span>}
                          {e.geofenceOk === false && <span className="tp-badge tp-badge--geo">Fuori area</span>}
                        </div>
                      </div>
                    ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {justifying && (
        <JustifyAnomalyModal
          anomaly={justifying}
          onClose={() => setJustifying(null)}
          onJustified={handleJustified}
        />
      )}
    </div>
  )
}
