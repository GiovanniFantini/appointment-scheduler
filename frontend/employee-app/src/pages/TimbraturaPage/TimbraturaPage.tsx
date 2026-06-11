import { useState, useEffect, useCallback } from 'react'
import { Link } from 'react-router-dom'
import TodayShiftsPanel from '../../components/TodayShiftsPanel/TodayShiftsPanel'
import JustifyAnomalyModal from '../../components/JustifyAnomalyModal/JustifyAnomalyModal'
import DayCarousel, { type DaySlide } from '../../components/DayCarousel/DayCarousel'
import { timeClockApi } from '../../lib/api/timeClock'
import { TimeEntryType, TimeClockAnomalyStatus, anomalyTypeLabel } from '../../types/timbratura'
import type { TimeEntryDto, TimeClockAnomalyDto, WellbeingStatsDto } from '../../types/timbratura'
import type { FeatureAccessLevel } from '../../App'
import './TimbraturaPage.css'

const LEVEL_RANK: Record<FeatureAccessLevel, number> = {
  ReadOnly: 1,
  Operator: 2,
  Manager: 3,
}

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
    default: return '#7F77DD'
  }
}

function formatTimestamp(iso: string): string {
  return new Date(iso).toLocaleString('it-IT', {
    day: '2-digit', month: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

/** "giovedì 11 giugno" da una data ISO di giornata (YYYY-MM-DD). */
function formatDayLabel(isoDay: string): string {
  return new Date(isoDay).toLocaleDateString('it-IT', {
    weekday: 'long', day: '2-digit', month: 'long',
  })
}

/** Raggruppa per workDate e ordina i giorni dal più recente. */
function groupByDay<T extends { workDate: string }>(items: T[]): [string, T[]][] {
  const map = items.reduce<Record<string, T[]>>((acc, it) => {
    (acc[it.workDate] ??= []).push(it)
    return acc
  }, {})
  return Object.keys(map)
    .sort((a, b) => b.localeCompare(a))
    .map(day => [day, map[day]])
}

interface Props {
  accessLevel?: FeatureAccessLevel
}

export default function TimbraturaPage({ accessLevel = 'ReadOnly' }: Props) {
  const [entries, setEntries] = useState<TimeEntryDto[]>([])
  const [loadingHistory, setLoadingHistory] = useState(true)
  const [anomalies, setAnomalies] = useState<TimeClockAnomalyDto[]>([])
  const [justifying, setJustifying] = useState<TimeClockAnomalyDto | null>(null)
  const [wellbeing, setWellbeing] = useState<WellbeingStatsDto | null>(null)
  const canManageTeamClock = LEVEL_RANK[accessLevel] >= LEVEL_RANK.Manager

  const loadHistory = useCallback(async () => {
    setLoadingHistory(true)
    try {
      // getTodayShifts innesca il rilevamento lazy delle mancate timbrature lato
      // server: va atteso PRIMA di leggere le anomalie, altrimenti quelle appena
      // generate non comparirebbero in questo stesso caricamento (race).
      await timeClockApi.getTodayShifts()
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

  // Storico timbrature aggregato per giorno → una slide per giorno nel carousel.
  const entryDays = groupByDay(entries)
  const entrySlides: DaySlide[] = entryDays.map(([day, dayEntries]) => ({
    key: day,
    label: formatDayLabel(day),
    content: (
      <div className="tp-entry-list">
        {dayEntries
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
    ),
  }))

  // Le anomalie aperte sono un'azione che il dipendente DEVE fare (giustificare):
  // vanno in cima come banner. Le altre (in revisione/risolte) sono consultazione
  // e finiscono nella sezione collassabile più in basso, aggregate per giorno.
  const openAnomalies = anomalies.filter(a => a.status === TimeClockAnomalyStatus.Open)
  const reviewedAnomalies = anomalies.filter(a => a.status !== TimeClockAnomalyStatus.Open)
  const reviewedSlides: DaySlide[] = groupByDay(reviewedAnomalies).map(([day, dayAnomalies]) => ({
    key: day,
    label: formatDayLabel(day),
    content: (
      <div className="tp-anomaly-list">
        {dayAnomalies.map(a => (
          <div key={a.id} className="tp-anomaly" data-status={a.status}>
            <div className="tp-anomaly-main">
              <span className="tp-anomaly-type">{anomalyTypeLabel(a.type, a.typeName)}</span>
              <span className={`tp-anomaly-status status-${a.status}`}>
                {ANOMALY_STATUS_LABEL[a.status] ?? a.statusName}
              </span>
            </div>
            <div className="tp-anomaly-meta">
              {a.deviationMinutes != null && (
                <span>{a.deviationMinutes > 0 ? '+' : ''}{a.deviationMinutes} min</span>
              )}
            </div>
            {a.employeeNotes && <div className="tp-anomaly-notes">"{a.employeeNotes}"</div>}
            {a.reviewNotes && (
              <div className="tp-anomaly-review">Risposta responsabile: {a.reviewNotes}</div>
            )}
          </div>
        ))}
      </div>
    ),
  }))

  return (
    <div className="timbratura-page">
      <div className="tp-header">
        <div className="tp-header-top">
          <h1 className="tp-title">Timbratura</h1>
          {canManageTeamClock && (
            <Link to="/timbratura-gestione" className="tp-manage-btn">
              Gestisci timbrature
            </Link>
          )}
        </div>
        <p className="tp-subtitle">Registra entrata, uscita e pause del tuo turno</p>
      </div>

      {/* Avviso prioritario: timbrature da giustificare. Tap → giustifica la prima. */}
      {openAnomalies.length > 0 && (
        <button
          type="button"
          className="tp-alert-banner"
          onClick={() => setJustifying(openAnomalies[0])}
        >
          <span className="tp-alert-icon">⚠</span>
          <span className="tp-alert-text">
            {openAnomalies.length === 1
              ? '1 timbratura da giustificare'
              : `${openAnomalies.length} timbrature da giustificare`}
          </span>
          <span className="tp-alert-chevron">›</span>
        </button>
      )}

      {/* Azione primaria: timbra. Resta sempre in cima, sopra la piega. */}
      <TodayShiftsPanel onChange={loadHistory} />

      {/* Andamento ore — consultazione, collassato di default. */}
      {wellbeing && (
        <details className="tp-section">
          <summary className="tp-section-head">
            <span className="tp-section-title">Andamento ore</span>
            {wellbeing.hasWellbeingAlert && <span className="tp-section-flag">⚠</span>}
            <span className="tp-section-chevron">›</span>
          </summary>
          <div className="tp-section-body">
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
        </details>
      )}

      {/* Storico timbrature — collassato di default, aggregato per giorno e sfogliabile. */}
      <details className="tp-section">
        <summary className="tp-section-head">
          <span className="tp-section-title">Le mie timbrature</span>
          {entryDays.length > 0 && <span className="tp-section-count">{entryDays.length} gg</span>}
          <span className="tp-section-chevron">›</span>
        </summary>
        <div className="tp-section-body">
          {loadingHistory ? (
            <div className="tp-loading"><div className="tp-spinner" /></div>
          ) : entrySlides.length === 0 ? (
            <div className="tp-empty">Nessuna timbratura registrata negli ultimi 30 giorni.</div>
          ) : (
            <DayCarousel slides={entrySlides} />
          )}
        </div>
      </details>

      {/* Anomalie già giustificate o risolte — collassate, aggregate per giorno e sfogliabili. */}
      {reviewedSlides.length > 0 && (
        <details className="tp-section">
          <summary className="tp-section-head">
            <span className="tp-section-title">Anomalie in revisione</span>
            <span className="tp-section-count">{reviewedAnomalies.length}</span>
            <span className="tp-section-chevron">›</span>
          </summary>
          <div className="tp-section-body">
            <DayCarousel slides={reviewedSlides} />
          </div>
        </details>
      )}

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
