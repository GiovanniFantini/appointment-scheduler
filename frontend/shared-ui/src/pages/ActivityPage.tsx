import { useEffect, useState } from 'react'
import './activity.css'

interface Row {
  id: number; receivedAt: string; action: string; category: string; outcome: string; source: string
  app: string; userId: number | null; merchantId: number | null; entityType: string | null; entityId: string | null
  operationId: string; details: string; statusCode: number | null
}
interface Result { items: Row[]; nextCursor: number | null }
interface Api { get<T>(url: string, config?: { params: Record<string, string | number> }): Promise<{ data: T }> }
const outcomes: Record<string, string> = { committed: 'Salvato', completed: 'Completato', observed: 'Osservato', rejected: 'Rifiutato', error: 'Errore', cancelled: 'Interrotto', started: 'Avviato', unknown: 'Esito incerto' }

export function ActivityPage({ apiClient }: { apiClient: Api }) {
  const [rows, setRows] = useState<Row[]>([])
  const [cursor, setCursor] = useState<number | null>(null)
  const [next, setNext] = useState<number | null>(null)
  const [category, setCategory] = useState('')
  const [outcome, setOutcome] = useState('')
  const [operation, setOperation] = useState('')
  const [refresh, setRefresh] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [filters, setFilters] = useState<Record<string, string>>({})
  const [exporting, setExporting] = useState(false)
  useEffect(() => {
    let disposed = false
    setLoading(true); setError('')
    const params: Record<string, string | number> = { ...filters }
    if (cursor) params.before = cursor
    if (category) params.category = category
    if (outcome) params.outcome = outcome
    if (operation) params.operationId = operation
    apiClient.get<Result>('/activity', { params }).then(({ data }) => {
      if (!disposed) { setRows(data.items); setNext(data.nextCursor) }
    }).catch(() => { if (!disposed) setError('Impossibile caricare il registro attività.') })
      .finally(() => { if (!disposed) setLoading(false) })
    return () => { disposed = true }
  }, [apiClient, cursor, category, outcome, operation, refresh, filters])
  async function exportPage() {
    setExporting(true); setError('')
    try {
      const params: Record<string, string | number> = { ...filters, export: 'true' }
      if (cursor) params.before = cursor
      if (category) params.category = category
      if (outcome) params.outcome = outcome
      if (operation) params.operationId = operation
      const { data } = await apiClient.get<Row[]>('/activity', { params })
      const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }))
      const link = document.createElement('a'); link.href = url; link.download = 'activity-page.json'; link.click()
      setTimeout(() => URL.revokeObjectURL(url), 1000)
    } catch { setError('Esportazione non riuscita.') }
    finally { setExporting(false) }
  }
  return <section className="activity-page">
    <header><p className="activity-eyebrow">TRACCIABILITÀ</p><h1>Registro attività</h1>
      <p>Richieste, modifiche salvate e interazioni delle applicazioni.</p></header>
    <div className="activity-controls">
      <label>Categoria<select data-activity="shared-ui.pages.ActivityPage.1" value={category} onChange={e => { setCategory(e.target.value); setCursor(null) }}>
        <option value="">Tutte</option><option value="change">Modifiche</option><option value="request">Richieste</option><option value="interaction">Interazioni</option><option value="external">Servizi esterni</option><option value="maintenance">Manutenzione</option>
      </select></label>
      <label>Esito<select data-activity="shared-ui.pages.ActivityPage.2" value={outcome} onChange={e => { setOutcome(e.target.value); setCursor(null) }}>
        <option value="">Tutti</option>{Object.entries(outcomes).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
      </select></label>
      <button data-activity="shared-ui.pages.ActivityPage.3" onClick={() => { setCursor(null); setRefresh(v => v + 1) }}>Aggiorna</button>
      <button data-activity="activity.export" disabled={loading || exporting || !rows.length} onClick={() => void exportPage()}>{exporting ? 'Esportazione…' : 'Esporta pagina JSON'}</button>
      {operation && <button data-activity="shared-ui.pages.ActivityPage.4" onClick={() => { setOperation(''); setCursor(null) }}>Rimuovi filtro operazione</button>}
    </div>
    <details className="activity-advanced"><summary data-activity="activity.filters.open">Filtri avanzati{Object.keys(filters).length ? ` (${Object.keys(filters).length})` : ''}</summary>
    <form data-activity="activity.filters" className="activity-controls" onSubmit={event => {
      event.preventDefault()
      const data = new FormData(event.currentTarget)
      const nextFilters: Record<string, string> = {}
      for (const [key, value] of data) {
        if (typeof value === 'string' && value.trim())
          nextFilters[key] = key === 'from' || key === 'to' ? new Date(value).toISOString() : value.trim()
      }
      setFilters(nextFilters); setCursor(null)
    }}>
      <label>Utente ID<input data-activity="activity.filter.user" name="userId" type="number" min="1" /></label>
      <label>Azienda ID<input data-activity="activity.filter.merchant" name="merchantId" type="number" min="1" /></label>
      <label>App<select data-activity="activity.filter.app" name="app"><option value="">Tutte</option><option>admin</option><option>merchant</option><option>employee</option><option>api</option><option>system</option></select></label>
      <label>Dal<input data-activity="activity.filter.from" name="from" type="datetime-local" /></label>
      <label>Al<input data-activity="activity.filter.to" name="to" type="datetime-local" /></label>
      <label>Azione<input data-activity="activity.filter.action" name="action" placeholder="Events.Create" /></label>
      <label>Tipo oggetto<input data-activity="activity.filter.entity" name="entityType" placeholder="Event" /></label>
      <label>Oggetto ID<input data-activity="activity.filter.entity-id" name="entityId" /></label>
      <button data-activity="activity.filter.apply" type="submit">Applica filtri</button>
      <button data-activity="activity.filter.reset" type="reset" onClick={() => { setFilters({}); setCursor(null); setCategory(''); setOutcome(''); setOperation('') }}>Azzera</button>
    </form>
    </details>
    <p className="activity-note">Le interazioni sono osservazioni del browser. “Salvato” identifica una modifica persistita dal server.</p>
    {error && <p role="alert">{error}</p>}
    {loading ? <p role="status">Caricamento attività…</p> : !error && <>
      <div className="activity-table"><table><thead><tr><th>Quando</th><th>Azione</th><th>Utente / azienda</th><th>Esito</th><th>Dettagli</th></tr></thead>
        <tbody>{rows.map(row => <tr key={row.id}>
          <td>{new Date(row.receivedAt).toLocaleString('it-IT')}<small>{row.app} · {row.source}</small></td>
          <td>{row.action}<small>{row.entityType ? `${row.entityType} #${row.entityId}` : row.category}</small></td>
          <td>{row.userId ? `Utente #${row.userId}` : 'Sistema / anonimo'}<small>{row.merchantId ? `Azienda #${row.merchantId}` : 'Piattaforma'}</small></td>
          <td><span className={`activity-status activity-${row.outcome}`}>{outcomes[row.outcome] ?? row.outcome}</span>{row.statusCode && <small>HTTP {row.statusCode}</small>}</td>
          <td><details><summary data-activity="shared-ui.pages.ActivityPage.5">Apri dettaglio</summary><pre>{JSON.stringify(JSON.parse(row.details), null, 2)}</pre>
            <button data-activity="shared-ui.pages.ActivityPage.6" onClick={() => { setOperation(row.operationId); setCursor(null) }}>Collega eventi dell’operazione</button></details></td>
        </tr>)}</tbody></table></div>
      {!rows.length && <p>Nessuna attività corrisponde ai filtri.</p>}
      <footer><button data-activity="shared-ui.pages.ActivityPage.7" disabled={!cursor} onClick={() => setCursor(null)}>Più recenti</button>
        <span>{rows.length} eventi</span><button data-activity="shared-ui.pages.ActivityPage.8" disabled={!next} onClick={() => setCursor(next)}>Precedenti</button></footer>
    </>}
  </section>
}
