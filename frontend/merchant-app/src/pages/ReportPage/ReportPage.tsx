import { useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import apiClient from '../../lib/axios'
import type { MerchantReport } from '../../lib/api/reports'
import { localDateStr, nativeDateInputProps } from '../../lib/dateUtils'
import { useBranch } from '../../contexts/BranchContext'
import './ReportPage.css'

const dateLabel = (value: string) => value.split('-').reverse().join('/')
const hours = (minutes: number) => new Intl.NumberFormat('it-IT', { maximumFractionDigits: 1 }).format(minutes / 60)
// I timestamp della timbratura rappresentano orari locali: nessuna conversione di fuso.
const clockLabel = (value: string | null) => value?.slice(11, 16) ?? '—'

function exportCsv(name: string, rows: (string | number)[][]) {
  const csv = rows.map(row => row.map(value => {
    const text = String(value)
    const safe = /^\s*[=+@-]/.test(text) ? `'${text}` : text
    return `"${safe.replace(/"/g, '""')}"`
  }).join(';')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8;' }))
  const link = document.createElement('a')
  link.href = url
  link.download = name
  link.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export default function ReportPage() {
  const { branches, activeBranchId } = useBranch()
  const [from, setFrom] = useState(() => localDateStr(new Date(new Date().getFullYear(), new Date().getMonth(), 1)))
  const [to, setTo] = useState(() => localDateStr(new Date()))
  const [branchId, setBranchId] = useState<number | null>(activeBranchId)
  const [report, setReport] = useState<MerchantReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const rangeDays = (Date.parse(to) - Date.parse(from)) / 86400000
  const valid = !!from && !!to && rangeDays >= 0 && rangeDays <= 365

  useEffect(() => {
    setReport(null)
    setPage(1)
    setError('')
    if (!valid) { setLoading(false); return }
    const controller = new AbortController()
    setLoading(true)
    apiClient.get<MerchantReport>('/merchant-reports', {
      params: { from, to, branchId }, signal: controller.signal,
    }).then(response => {
      if (!controller.signal.aborted) setReport(response.data)
    }).catch((err: unknown) => {
      if (!controller.signal.aborted) setError(axios.isAxiosError(err)
        ? err.response?.data?.message ?? 'Impossibile caricare i report. Riprova.'
        : 'Impossibile caricare i report. Riprova.')
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false)
    })
    return () => controller.abort()
  }, [from, to, branchId, valid, reload])

  const entries = useMemo(() => (report?.timeEntries ?? []).filter(row =>
    `${row.employeeName} ${row.eventTitle} ${row.branchName}`.toLocaleLowerCase('it').includes(search.trim().toLocaleLowerCase('it'))), [report, search])
  const totals = useMemo(() => (report?.timeEntries ?? []).reduce((sum, row) => ({
    worked: sum.worked + row.workedMinutes,
    overtime: sum.overtime + row.overtimeMinutes,
    anomalies: sum.anomalies + Number(row.hasOpenAnomaly),
  }), { worked: 0, overtime: 0, anomalies: 0 }), [report])
  const pages = Math.max(1, Math.ceil(entries.length / 20))
  const currentPage = Math.min(page, pages)
  const maxShifts = Math.max(...(report?.days.map(day => day.shiftCount) ?? []), 1)

  function preset(previous: boolean) {
    const now = new Date()
    const month = now.getMonth() - Number(previous)
    setFrom(localDateStr(new Date(now.getFullYear(), month, 1)))
    setTo(localDateStr(previous ? new Date(now.getFullYear(), month + 1, 0) : now))
  }
  function downloadSummary() {
    if (!report) return
    exportCsv(`riepilogo-${from}-${to}.csv`, [
      ['Dal', 'Al', 'Filiale', 'Turni', 'Assegnazioni', 'Ore lavorate', 'Ore straordinario', 'Richieste in attesa', 'Approvate', 'Respinte'],
      [from, to, branches.find(b => b.id === branchId)?.name ?? 'Tutte le filiali', report.shiftCount,
        report.assignedShiftCount, hours(totals.worked), hours(totals.overtime), report.pendingRequests, report.approvedRequests, report.rejectedRequests],
      [], ['Data', 'Turni', 'Assegnazioni'], ...report.days.map(day => [day.date, day.shiftCount, day.assignedShiftCount]),
    ])
  }
  function downloadEntries() {
    exportCsv(`ore-lavorate-${from}-${to}.csv`, [
      ['Data', 'Dipendente', 'Filiale', 'Turno', 'Entrata', 'Uscita', 'Minuti lavorati', 'Minuti pausa', 'Minuti pianificati', 'Minuti straordinario', 'Anomalie aperte'],
      ...entries.map(row => [row.workDate, row.employeeName, row.branchName, row.eventTitle,
        clockLabel(row.clockInUtc), clockLabel(row.clockOutUtc), row.workedMinutes, row.breakMinutes,
        row.scheduledMinutes ?? '', row.overtimeMinutes, row.hasOpenAnomaly ? 'Sì' : 'No']),
    ])
  }

  return (
    <div className="report-page">
      <header className="report-heading">
        <div><span className="report-eyebrow">ANALISI AZIENDALE</span><h1>Report</h1><p>Turni, richieste e ore lavorate in un unico riepilogo.</p></div>
        <button data-activity="merchant.pages.ReportPage.ReportPage.1" type="button" onClick={downloadSummary} disabled={!report || loading || !valid}>Esporta riepilogo CSV</button>
      </header>
      <section className="report-filters" aria-label="Filtri report">
        <label>Dal<input data-activity="merchant.pages.ReportPage.ReportPage.2" type="date" value={from} max={to || undefined} onChange={e => setFrom(e.target.value)} {...nativeDateInputProps} /></label>
        <label>Al<input data-activity="merchant.pages.ReportPage.ReportPage.3" type="date" value={to} min={from || undefined} onChange={e => setTo(e.target.value)} {...nativeDateInputProps} /></label>
        <label>Filiale<select data-activity="merchant.pages.ReportPage.ReportPage.4" aria-label="Filiale" value={branchId ?? ''} onChange={e => setBranchId(e.target.value ? Number(e.target.value) : null)}><option value="">Tutte le filiali</option>{branches.map(b => <option key={b.id} value={b.id}>{b.name}{!b.isActive ? ' (inattiva)' : ''}</option>)}</select></label>
        <div className="report-presets"><button data-activity="merchant.pages.ReportPage.ReportPage.5" type="button" onClick={() => preset(false)}>Questo mese</button><button data-activity="merchant.pages.ReportPage.ReportPage.6" type="button" onClick={() => preset(true)}>Mese scorso</button><button data-activity="merchant.pages.ReportPage.ReportPage.7" type="button" onClick={() => setReload(n => n + 1)} disabled={loading || !valid}>Aggiorna</button></div>
      </section>
      {!valid && <p role="alert" className="report-message">Seleziona un intervallo valido di massimo 366 giorni.</p>}
      {error && <div role="alert" className="report-message">{error} <button data-activity="merchant.pages.ReportPage.ReportPage.8" type="button" onClick={() => setReload(n => n + 1)}>Riprova</button></div>}
      {loading && <p role="status" className="report-message">Caricamento dei report…</p>}
      {report && valid && !loading && <>
        <div className="report-period">{dateLabel(report.from)} — {dateLabel(report.to)} · {branches.find(b => b.id === branchId)?.name ?? 'Tutte le filiali'}</div>
        <div className="report-stats">
          <article><span>Turni pianificati</span><strong>{report.shiftCount}</strong><small>{report.assignedShiftCount} assegnazioni ai dipendenti</small></article>
          <article><span>Ore lavorate</span><strong>{hours(totals.worked)} <em>h</em></strong><small>Pause escluse · da timbrature registrate</small></article>
          <article><span>Straordinario rilevato</span><strong>{hours(totals.overtime)} <em>h</em></strong><small>{totals.anomalies} turni timbrati con anomalie aperte</small></article>
          <article><span>Richieste gestite</span><strong>{report.approvedRequests + report.rejectedRequests}</strong><small>{report.pendingRequests} in attesa di approvazione</small></article>
        </div>
        <div className="report-overview">
          <section className="report-panel"><h2>Distribuzione dei turni</h2><p>Numero di turni per data di inizio.</p>
            {report.shiftCount === 0 ? <p className="report-empty">Nessun turno pianificato nel periodo.</p> : <div className="report-chart" style={{ gap: report.days.length > 60 ? 0 : 2 }} role="img" aria-label={`${report.shiftCount} turni nel periodo. Dettaglio giornaliero disponibile nel CSV di riepilogo.`}>
              {report.days.map(day => <div key={day.date} className="report-bar-slot" title={`${dateLabel(day.date)}: ${day.shiftCount} turni, ${day.assignedShiftCount} assegnazioni`}><div className="report-bar" style={{ height: `${day.shiftCount / maxShifts * 100}%` }} /></div>)}
            </div>}
            <div className="report-chart-labels"><span>{dateLabel(report.from)}</span><span>{dateLabel(report.to)}</span></div>
          </section>
          <section className="report-panel"><h2>Stato delle richieste</h2><p>Richieste che interessano il periodo selezionato.</p><dl className="report-requests"><div><dt>Approvate</dt><dd>{report.approvedRequests}</dd></div><div><dt>Respinte</dt><dd>{report.rejectedRequests}</dd></div><div><dt>In attesa</dt><dd>{report.pendingRequests}</dd></div></dl></section>
        </div>
        <section className="report-panel">
          <div className="report-table-heading"><div><h2>Dettaglio ore lavorate</h2><p>Una riga per dipendente, turno e giornata di lavoro.</p></div><button data-activity="merchant.pages.ReportPage.ReportPage.9" type="button" onClick={downloadEntries} disabled={!entries.length}>Esporta dettaglio CSV</button></div>
          <label className="report-search">Cerca nel dettaglio<input data-activity="merchant.pages.ReportPage.ReportPage.10" type="search" placeholder="Dipendente, turno o filiale" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /></label>
          {entries.length === 0 ? <p className="report-empty">{search ? 'Nessun risultato per la ricerca.' : 'Nessuna timbratura registrata nel periodo selezionato.'}</p> : <>
            <div className="report-table-scroll"><table><caption className="report-sr-only">Ore lavorate dal {dateLabel(from)} al {dateLabel(to)}</caption><thead><tr>{['Data / turno', 'Dipendente', 'Filiale', 'Entrata', 'Uscita', 'Ore lavorate', 'Pausa', 'Straordinario', 'Stato'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead><tbody>
              {entries.slice((currentPage - 1) * 20, currentPage * 20).map((row, index) => <tr key={`${row.employeeId}-${row.workDate}-${index}`}><td>{dateLabel(row.workDate)}<small>{row.eventTitle}</small></td><td>{row.employeeName}</td><td>{row.branchName}</td><td>{clockLabel(row.clockInUtc)}</td><td>{clockLabel(row.clockOutUtc)}</td><td>{hours(row.workedMinutes)} h</td><td>{Math.round(row.breakMinutes)} min</td><td>{hours(row.overtimeMinutes)} h</td><td><span className={`report-status${row.hasOpenAnomaly ? ' report-status-warning' : ''}`}>{row.hasOpenAnomaly ? 'Da verificare' : !row.clockInUtc || !row.clockOutUtc ? 'Incompleto' : 'Completo'}</span></td></tr>)}
            </tbody></table></div>
            <div className="report-pagination"><span>{entries.length} risultati · Pagina {currentPage} di {pages}</span><div><button data-activity="merchant.pages.ReportPage.ReportPage.11" type="button" disabled={currentPage === 1} onClick={() => setPage(currentPage - 1)}>Precedente</button><button data-activity="merchant.pages.ReportPage.ReportPage.12" type="button" disabled={currentPage === pages} onClick={() => setPage(currentPage + 1)}>Successiva</button></div></div>
          </>}
        </section>
        <p className="report-footnote">I turni sono conteggiati per data di inizio, anche se notturni. Le ore includono solo i turni con timbrature; le sessioni aperte possono essere parziali. Le richieste senza turno sono attribuite alla filiale primaria attuale del dipendente. Lo straordinario indica il tempo oltre la durata pianificata.</p>
      </>}
    </div>
  )
}
