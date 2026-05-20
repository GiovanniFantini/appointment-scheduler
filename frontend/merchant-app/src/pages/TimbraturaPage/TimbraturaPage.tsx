import { useState, useEffect, useCallback } from 'react'
import { useBranch } from '../../contexts/BranchContext'
import {
  timeClockApi,
  type BranchTimeClockSettings,
  type TimeEntry,
  type TimeClockAnomaly,
  type TimeClockReportRow,
} from '../../lib/api/timeClock'
import './TimbraturaPage.css'

/** Converte minuti in "Nh MMm". */
function formatMinutes(min: number): string {
  const h = Math.floor(min / 60)
  const m = Math.round(min % 60)
  return h > 0 ? `${h}h ${m.toString().padStart(2, '0')}m` : `${m}m`
}

/** Formatta una Date come "YYYY-MM-DD" nel fuso locale. */
function localDateStr(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

type Tab = 'config' | 'presenze' | 'anomalie' | 'report'

const ENTRY_TYPE_LABEL: Record<number, string> = {
  1: 'Entrata', 2: 'Uscita', 3: 'Inizio pausa', 4: 'Fine pausa',
}

const ANOMALY_TYPE_LABEL: Record<number, string> = {
  1: 'Ritardo entrata', 2: 'Entrata anticipata', 3: 'Uscita posticipata',
  4: 'Uscita anticipata', 5: 'Entrata mancante', 6: 'Uscita mancante',
  7: 'Pausa prolungata', 8: 'Fuori area', 9: 'Straordinario',
}

const ANOMALY_STATUS_LABEL: Record<number, string> = {
  1: 'Da giustificare', 2: 'In revisione', 3: 'Approvata', 4: 'Respinta',
}

export default function TimbraturaPage() {
  const { activeBranches, loading: branchesLoading } = useBranch()
  const [tab, setTab] = useState<Tab>('config')
  const [branchId, setBranchId] = useState<number | null>(null)

  // Pre-seleziona la prima filiale appena disponibili.
  useEffect(() => {
    if (branchId == null && activeBranches.length > 0) {
      setBranchId(activeBranches[0].id)
    }
  }, [activeBranches, branchId])

  return (
    <div className="timbratura-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Timbratura</h1>
          <p className="page-subtitle">
            Configura la rilevazione presenze e monitora le timbrature dei dipendenti.
          </p>
        </div>
      </div>

      <div className="tcm-tabs">
        <button
          className={`tcm-tab ${tab === 'config' ? 'tcm-tab--active' : ''}`}
          onClick={() => setTab('config')}
        >
          Configurazione
        </button>
        <button
          className={`tcm-tab ${tab === 'presenze' ? 'tcm-tab--active' : ''}`}
          onClick={() => setTab('presenze')}
        >
          Presenze
        </button>
        <button
          className={`tcm-tab ${tab === 'anomalie' ? 'tcm-tab--active' : ''}`}
          onClick={() => setTab('anomalie')}
        >
          Anomalie
        </button>
        <button
          className={`tcm-tab ${tab === 'report' ? 'tcm-tab--active' : ''}`}
          onClick={() => setTab('report')}
        >
          Report
        </button>
      </div>

      <div className="tcm-branch-row">
        <label className="tcm-branch-label">Filiale</label>
        <select
          className="tcm-branch-select"
          value={branchId ?? ''}
          onChange={e => setBranchId(e.target.value ? Number(e.target.value) : null)}
          disabled={branchesLoading || activeBranches.length === 0}
        >
          {activeBranches.map(b => (
            <option key={b.id} value={b.id}>
              {b.name}{b.isHeadquarters ? ' (sede principale)' : ''}
            </option>
          ))}
        </select>
      </div>

      {branchId == null ? (
        <div className="tcm-empty">Nessuna filiale disponibile.</div>
      ) : tab === 'config' ? (
        <ConfigTab branchId={branchId} />
      ) : tab === 'presenze' ? (
        <PresenzeTab branchId={branchId} />
      ) : tab === 'anomalie' ? (
        <AnomalieTab branchId={branchId} />
      ) : (
        <ReportTab branchId={branchId} />
      )}
    </div>
  )
}

// ── Tab Configurazione ───────────────────────────────────────────────────

function ConfigTab({ branchId }: { branchId: number }) {
  const [settings, setSettings] = useState<BranchTimeClockSettings | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await timeClockApi.getSettings(branchId)
      setSettings(data)
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore nel caricamento della configurazione.')
    } finally {
      setLoading(false)
    }
  }, [branchId])

  useEffect(() => { load() }, [load])

  const update = <K extends keyof BranchTimeClockSettings>(key: K, value: BranchTimeClockSettings[K]) => {
    setSettings(prev => (prev ? { ...prev, [key]: value } : prev))
    setSaved(false)
  }

  const handleSave = async () => {
    if (!settings) return
    setSaving(true)
    setError('')
    try {
      const updated = await timeClockApi.updateSettings(branchId, {
        isEnabled: settings.isEnabled,
        clockingRequired: settings.clockingRequired,
        graceInMinutes: settings.graceInMinutes,
        graceOutMinutes: settings.graceOutMinutes,
        earlyClockInToleranceMinutes: settings.earlyClockInToleranceMinutes,
        lateClockOutToleranceMinutes: settings.lateClockOutToleranceMinutes,
        geofencingEnabled: settings.geofencingEnabled,
        geofenceRadiusMeters: settings.geofenceRadiusMeters,
        breakTrackingEnabled: settings.breakTrackingEnabled,
        maxBreakMinutes: settings.maxBreakMinutes,
        roundingMinutes: settings.roundingMinutes,
        requirePhoto: settings.requirePhoto,
        branchLatitude: settings.branchLatitude ?? null,
        branchLongitude: settings.branchLongitude ?? null,
      })
      setSettings(updated)
      setSaved(true)
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore nel salvataggio.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="tcm-loading"><div className="tcm-spinner" /></div>
  if (error && !settings) return <div className="tcm-error">{error}</div>
  if (!settings) return null

  return (
    <div className="tcm-config">
      <div className="tcm-card">
        <ToggleRow
          label="Timbratura attiva"
          hint="Abilita la rilevazione presenze per i dipendenti di questa filiale."
          checked={settings.isEnabled}
          onChange={v => update('isEnabled', v)}
        />
        <ToggleRow
          label="Timbratura obbligatoria"
          hint="Se attiva, i dipendenti devono timbrare; altrimenti è facoltativa."
          checked={settings.clockingRequired}
          onChange={v => update('clockingRequired', v)}
          disabled={!settings.isEnabled}
        />
      </div>

      <div className="tcm-card">
        <div className="tcm-card-title">Tolleranze (minuti)</div>
        <div className="tcm-grid">
          <NumberField label="Tolleranza ritardo entrata" value={settings.graceInMinutes}
            onChange={v => update('graceInMinutes', v)} disabled={!settings.isEnabled} />
          <NumberField label="Tolleranza uscita" value={settings.graceOutMinutes}
            onChange={v => update('graceOutMinutes', v)} disabled={!settings.isEnabled} />
          <NumberField label="Anticipo massimo entrata" value={settings.earlyClockInToleranceMinutes}
            onChange={v => update('earlyClockInToleranceMinutes', v)} disabled={!settings.isEnabled} />
          <NumberField label="Ritardo massimo uscita" value={settings.lateClockOutToleranceMinutes}
            onChange={v => update('lateClockOutToleranceMinutes', v)} disabled={!settings.isEnabled} />
        </div>
      </div>

      <div className="tcm-card">
        <div className="tcm-card-title">Pause</div>
        <ToggleRow
          label="Registrazione pause"
          hint="Permette ai dipendenti di registrare le pause durante il turno."
          checked={settings.breakTrackingEnabled}
          onChange={v => update('breakTrackingEnabled', v)}
          disabled={!settings.isEnabled}
        />
        <div className="tcm-grid">
          <NumberField label="Durata massima pausa (min)" value={settings.maxBreakMinutes}
            onChange={v => update('maxBreakMinutes', v)}
            disabled={!settings.isEnabled || !settings.breakTrackingEnabled} />
        </div>
      </div>

      <div className="tcm-card">
        <div className="tcm-card-title">Geolocalizzazione</div>
        <ToggleRow
          label="Geofencing"
          hint="Verifica che la timbratura avvenga nei pressi della filiale. Una timbratura fuori area non viene bloccata, ma segnalata come anomalia."
          checked={settings.geofencingEnabled}
          onChange={v => update('geofencingEnabled', v)}
          disabled={!settings.isEnabled}
        />
        <div className="tcm-grid">
          <NumberField label="Raggio geofence (metri)" value={settings.geofenceRadiusMeters}
            onChange={v => update('geofenceRadiusMeters', v)}
            disabled={!settings.isEnabled || !settings.geofencingEnabled} />
          <div className="tcm-field">
            <label className="tcm-field-label">Latitudine filiale</label>
            <input
              type="number" step="any" className="tcm-field-input"
              value={settings.branchLatitude ?? ''}
              disabled={!settings.isEnabled || !settings.geofencingEnabled}
              onChange={e => update('branchLatitude', e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
          <div className="tcm-field">
            <label className="tcm-field-label">Longitudine filiale</label>
            <input
              type="number" step="any" className="tcm-field-input"
              value={settings.branchLongitude ?? ''}
              disabled={!settings.isEnabled || !settings.geofencingEnabled}
              onChange={e => update('branchLongitude', e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
        </div>
        {settings.isEnabled && settings.geofencingEnabled && (
          <button
            type="button"
            className="tcm-geo-btn"
            onClick={() => {
              if (!('geolocation' in navigator)) {
                setError('Geolocalizzazione non disponibile sul browser.')
                return
              }
              navigator.geolocation.getCurrentPosition(
                pos => {
                  update('branchLatitude', pos.coords.latitude)
                  update('branchLongitude', pos.coords.longitude)
                },
                () => setError('Impossibile ottenere la posizione attuale.'),
              )
            }}
          >
            📍 Usa la mia posizione attuale
          </button>
        )}
      </div>

      {error && <div className="tcm-error">{error}</div>}

      <div className="tcm-save-row">
        {saved && <span className="tcm-saved">Configurazione salvata.</span>}
        <button className="btn-primary" onClick={handleSave} disabled={saving}>
          {saving ? 'Salvataggio…' : 'Salva configurazione'}
        </button>
      </div>
    </div>
  )
}

function ToggleRow({ label, hint, checked, onChange, disabled }: {
  label: string; hint: string; checked: boolean
  onChange: (v: boolean) => void; disabled?: boolean
}) {
  return (
    <div className={`tcm-toggle-row ${disabled ? 'tcm-toggle-row--disabled' : ''}`}>
      <div className="tcm-toggle-text">
        <span className="tcm-toggle-label">{label}</span>
        <span className="tcm-toggle-hint">{hint}</span>
      </div>
      <label className="toggle-switch">
        <input type="checkbox" checked={checked} disabled={disabled}
          onChange={e => onChange(e.target.checked)} />
        <span className="toggle-slider" />
      </label>
    </div>
  )
}

function NumberField({ label, value, onChange, disabled }: {
  label: string; value: number; onChange: (v: number) => void; disabled?: boolean
}) {
  return (
    <div className="tcm-field">
      <label className="tcm-field-label">{label}</label>
      <input
        type="number"
        min={0}
        className="tcm-field-input"
        value={value}
        disabled={disabled}
        onChange={e => onChange(Math.max(0, Number(e.target.value) || 0))}
      />
    </div>
  )
}

// ── Tab Presenze ───────────────────────────────────────────────────────────

function PresenzeTab({ branchId }: { branchId: number }) {
  const [entries, setEntries] = useState<TimeEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [from, setFrom] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() - 7)
    return localDateStr(d)
  })
  const [to, setTo] = useState(() => localDateStr(new Date()))

  const load = useCallback(async (showSpinner = true) => {
    if (showSpinner) setLoading(true)
    try {
      const data = await timeClockApi.getEntries({ branchId, from, to })
      setEntries(Array.isArray(data) ? data : [])
    } catch {
      setEntries([])
    } finally {
      setLoading(false)
    }
  }, [branchId, from, to])

  useEffect(() => { load() }, [load])

  // Presenze live: se il periodo include oggi, aggiorna ogni 60s senza spinner.
  useEffect(() => {
    if (to !== localDateStr(new Date())) return
    const id = setInterval(() => load(false), 60000)
    return () => clearInterval(id)
  }, [to, load])

  return (
    <div className="tcm-presenze">
      <div className="tcm-filter-row">
        <div className="tcm-field">
          <label className="tcm-field-label">Da</label>
          <input type="date" className="tcm-field-input" value={from}
            onChange={e => setFrom(e.target.value)} />
        </div>
        <div className="tcm-field">
          <label className="tcm-field-label">A</label>
          <input type="date" className="tcm-field-input" value={to}
            onChange={e => setTo(e.target.value)} />
        </div>
      </div>

      {loading ? (
        <div className="tcm-loading"><div className="tcm-spinner" /></div>
      ) : entries.length === 0 ? (
        <div className="tcm-empty">Nessuna timbratura nel periodo selezionato.</div>
      ) : (
        <table className="tcm-table">
          <thead>
            <tr>
              <th>Dipendente</th>
              <th>Tipo</th>
              <th>Data/ora</th>
              <th>Turno</th>
              <th>Scarto</th>
              <th>Note</th>
            </tr>
          </thead>
          <tbody>
            {entries.map(e => (
              <tr key={e.id}>
                <td>{e.employeeName}</td>
                <td>
                  <span className="tcm-type-badge" data-type={e.type}>
                    {ENTRY_TYPE_LABEL[e.type] ?? e.typeName}
                  </span>
                </td>
                <td>{new Date(e.actualTimestampUtc).toLocaleString(undefined, {
                  day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit',
                })}</td>
                <td>{e.eventTitle}</td>
                <td>
                  {e.deviationMinutes != null
                    ? <span className={e.deviationMinutes > 0 ? 'tcm-dev-late' : 'tcm-dev-early'}>
                        {e.deviationMinutes > 0 ? '+' : ''}{e.deviationMinutes} min
                      </span>
                    : '—'}
                </td>
                <td>
                  {e.isManualCorrection && <span className="tcm-tag">Correzione</span>}
                  {e.geofenceOk === false && <span className="tcm-tag tcm-tag--warn">Fuori area</span>}
                  {e.notes}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ── Tab Anomalie ───────────────────────────────────────────────────────────

function AnomalieTab({ branchId }: { branchId: number }) {
  const [anomalies, setAnomalies] = useState<TimeClockAnomaly[]>([])
  const [loading, setLoading] = useState(true)
  const [statusFilter, setStatusFilter] = useState<number | ''>('')
  const [actingId, setActingId] = useState<number | null>(null)
  const [detecting, setDetecting] = useState(false)
  const [info, setInfo] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await timeClockApi.getAnomalies({
        branchId,
        status: statusFilter === '' ? undefined : statusFilter,
      })
      setAnomalies(Array.isArray(data) ? data : [])
    } catch {
      setAnomalies([])
    } finally {
      setLoading(false)
    }
  }, [branchId, statusFilter])

  useEffect(() => { load() }, [load])

  const review = async (id: number, approve: boolean) => {
    const notes = window.prompt(approve
      ? 'Note di approvazione (facoltative):'
      : 'Motivo del rifiuto (facoltativo):') ?? undefined
    setActingId(id)
    try {
      const updated = approve
        ? await timeClockApi.approveAnomaly(id, notes)
        : await timeClockApi.rejectAnomaly(id, notes)
      setAnomalies(prev => prev.map(a => (a.id === id ? updated : a)))
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      alert(e.response?.data?.message ?? 'Errore durante la revisione.')
    } finally {
      setActingId(null)
    }
  }

  const runDetection = async () => {
    setDetecting(true)
    setInfo('')
    try {
      const { created } = await timeClockApi.runDetection(branchId)
      setInfo(created > 0
        ? `Rilevate ${created} nuove anomalie di timbratura mancante.`
        : 'Nessuna nuova anomalia rilevata.')
      await load()
    } catch {
      setInfo('Errore durante il controllo.')
    } finally {
      setDetecting(false)
    }
  }

  return (
    <div className="tcm-presenze">
      <div className="tcm-filter-row">
        <div className="tcm-field">
          <label className="tcm-field-label">Stato</label>
          <select
            className="tcm-field-input"
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="">Tutti</option>
            <option value={1}>Da giustificare</option>
            <option value={2}>In revisione</option>
            <option value={3}>Approvate</option>
            <option value={4}>Respinte</option>
          </select>
        </div>
        <button className="tcm-geo-btn" onClick={runDetection} disabled={detecting}
          style={{ marginTop: 'auto' }}>
          {detecting ? 'Controllo…' : 'Rileva timbrature mancanti'}
        </button>
      </div>

      {info && <div className="tcm-saved">{info}</div>}

      {loading ? (
        <div className="tcm-loading"><div className="tcm-spinner" /></div>
      ) : anomalies.length === 0 ? (
        <div className="tcm-empty">Nessuna anomalia per i filtri selezionati.</div>
      ) : (
        <table className="tcm-table">
          <thead>
            <tr>
              <th>Dipendente</th>
              <th>Anomalia</th>
              <th>Data</th>
              <th>Stato</th>
              <th>Giustificazione</th>
              <th>Azioni</th>
            </tr>
          </thead>
          <tbody>
            {anomalies.map(a => (
              <tr key={a.id}>
                <td>{a.employeeName}</td>
                <td>{ANOMALY_TYPE_LABEL[a.type] ?? a.typeName}</td>
                <td>{new Date(a.workDate).toLocaleDateString(undefined, {
                  day: '2-digit', month: '2-digit',
                })}</td>
                <td>
                  <span className="tcm-type-badge" data-anomaly-status={a.status}>
                    {ANOMALY_STATUS_LABEL[a.status] ?? a.statusName}
                  </span>
                </td>
                <td>
                  {a.employeeReasonName
                    ? <span>{a.employeeReasonName}{a.employeeNotes ? ` — ${a.employeeNotes}` : ''}</span>
                    : <span className="tcm-muted">—</span>}
                </td>
                <td>
                  {a.status === 2 ? (
                    <div className="tcm-action-buttons">
                      <button className="tcm-btn-approve" disabled={actingId === a.id}
                        onClick={() => review(a.id, true)}>Approva</button>
                      <button className="tcm-btn-reject" disabled={actingId === a.id}
                        onClick={() => review(a.id, false)}>Respingi</button>
                    </div>
                  ) : (
                    <span className="tcm-muted">—</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ── Tab Report ─────────────────────────────────────────────────────────────

function ReportTab({ branchId }: { branchId: number }) {
  const [rows, setRows] = useState<TimeClockReportRow[]>([])
  const [loading, setLoading] = useState(true)
  const [from, setFrom] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() - 30)
    return localDateStr(d)
  })
  const [to, setTo] = useState(() => localDateStr(new Date()))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await timeClockApi.getReport({ branchId, from, to })
      setRows(Array.isArray(data) ? data : [])
    } catch {
      setRows([])
    } finally {
      setLoading(false)
    }
  }, [branchId, from, to])

  useEffect(() => { load() }, [load])

  const totalWorked = rows.reduce((s, r) => s + r.workedMinutes, 0)
  const totalOvertime = rows.reduce((s, r) => s + r.overtimeMinutes, 0)

  return (
    <div className="tcm-presenze">
      <div className="tcm-filter-row">
        <div className="tcm-field">
          <label className="tcm-field-label">Da</label>
          <input type="date" className="tcm-field-input" value={from}
            onChange={e => setFrom(e.target.value)} />
        </div>
        <div className="tcm-field">
          <label className="tcm-field-label">A</label>
          <input type="date" className="tcm-field-input" value={to}
            onChange={e => setTo(e.target.value)} />
        </div>
      </div>

      {loading ? (
        <div className="tcm-loading"><div className="tcm-spinner" /></div>
      ) : rows.length === 0 ? (
        <div className="tcm-empty">Nessun dato di presenza nel periodo selezionato.</div>
      ) : (
        <>
          <div className="tcm-report-summary">
            <div className="tcm-summary-card">
              <span className="tcm-summary-label">Ore lavorate totali</span>
              <span className="tcm-summary-value">{formatMinutes(totalWorked)}</span>
            </div>
            <div className="tcm-summary-card">
              <span className="tcm-summary-label">Straordinari totali</span>
              <span className="tcm-summary-value">{formatMinutes(totalOvertime)}</span>
            </div>
          </div>
          <table className="tcm-table">
            <thead>
              <tr>
                <th>Dipendente</th>
                <th>Data</th>
                <th>Turno</th>
                <th>Lavorate</th>
                <th>Pause</th>
                <th>Pianificate</th>
                <th>Straordinario</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={`${r.employeeId}-${r.workDate}-${i}`}>
                  <td>
                    {r.employeeName}
                    {r.hasOpenAnomaly && <span className="tcm-tag tcm-tag--warn">Anomalia</span>}
                  </td>
                  <td>{new Date(r.workDate).toLocaleDateString(undefined, {
                    day: '2-digit', month: '2-digit',
                  })}</td>
                  <td>{r.eventTitle}</td>
                  <td>{formatMinutes(r.workedMinutes)}</td>
                  <td>{formatMinutes(r.breakMinutes)}</td>
                  <td>{r.scheduledMinutes != null ? formatMinutes(r.scheduledMinutes) : '—'}</td>
                  <td>
                    {r.overtimeMinutes > 0
                      ? <span className="tcm-dev-late">+{formatMinutes(r.overtimeMinutes)}</span>
                      : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  )
}
