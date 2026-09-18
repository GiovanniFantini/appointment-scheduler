import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button, EmptyState, PageHeader, Skeleton, StatusChip } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './ReportsPage.css'

interface Merchant {
  id: number
  companyName: string
  city: string | null
  isApproved: boolean
  isActive: boolean
  createdAt: string
  employeeCount: number
  branchCount: number
}

type Status = 'active' | 'pending' | 'inactive'
type Attention = 'all' | 'approval' | 'branches' | 'employees'
const labels: Record<Status, string> = { active: 'Attiva', pending: 'In attesa', inactive: 'Disattivata' }
const number = new Intl.NumberFormat('it-IT')

function statusOf(m: Merchant): Status {
  return !m.isActive ? 'inactive' : m.isApproved ? 'active' : 'pending'
}

function checksFor(m: Merchant): string[] {
  if (!m.isActive) return []
  if (!m.isApproved) return ['Approvazione da completare']
  return [
    ...(m.branchCount === 0 ? ['Nessuna filiale'] : []),
    ...(m.employeeCount === 0 ? ['Nessun dipendente interno attivo'] : []),
  ]
}

function csvCell(value: string | number): string {
  const text = String(value)
  // Le anagrafiche non devono diventare formule quando si apre il CSV.
  const safe = /^\s*[=+@-]|^[\t\r\n]/.test(text) ? `'${text}` : text
  return `"${safe.replace(/"/g, '""')}"`
}

export default function ReportsPage() {
  const [merchants, setMerchants] = useState<Merchant[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null)
  const [revision, setRevision] = useState(0)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<Status | 'all'>('all')
  const [attention, setAttention] = useState<Attention>('all')
  const [sort, setSort] = useState('newest')

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(false)
    apiClient.get<Merchant[]>('/merchants', { signal: controller.signal })
      .then(({ data }) => { setMerchants(data); setUpdatedAt(new Date()) })
      .catch(() => { if (!controller.signal.aborted) setError(true) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [revision])

  const query = search.trim().toLocaleLowerCase('it-IT')
  const filtered = merchants.filter(m => {
    const current = statusOf(m)
    return (status === 'all' || current === status)
      && (!query || `${m.companyName} ${m.city ?? ''}`.toLocaleLowerCase('it-IT').includes(query))
      && (attention === 'all'
        || (attention === 'approval' && current === 'pending')
        || (attention === 'branches' && current === 'active' && m.branchCount === 0)
        || (attention === 'employees' && current === 'active' && m.employeeCount === 0))
  }).sort((a, b) => {
    if (sort === 'name') return a.companyName.localeCompare(b.companyName, 'it-IT') || a.id - b.id
    if (sort === 'employees') return b.employeeCount - a.employeeCount || a.id - b.id
    return Date.parse(b.createdAt) - Date.parse(a.createdAt) || b.id - a.id
  })

  const metrics = [
    ['Aziende', filtered.length],
    ['In attesa di approvazione', filtered.filter(m => statusOf(m) === 'pending').length],
    ['Dipendenti interni attivi', filtered.reduce((sum, m) => sum + m.employeeCount, 0)],
    ['Filiali registrate', filtered.reduce((sum, m) => sum + m.branchCount, 0)],
  ] as const

  const exportCsv = () => {
    const rows = [
      ['ID', 'Azienda', 'Città', 'Stato', 'Registrata il (UTC)', 'Dipendenti interni attivi', 'Filiali registrate', 'Da verificare'],
      ...filtered.map(m => [m.id, m.companyName, m.city ?? '', labels[statusOf(m)],
        new Date(m.createdAt).toISOString(), m.employeeCount, m.branchCount, checksFor(m).join(' / ')]),
    ]
    const blob = new Blob(['\uFEFF', rows.map(row => row.map(csvCell).join(';')).join('\r\n')], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `report-aziende-${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  }

  return <div className="reports-page">
    <PageHeader title="Report amministrativo" subtitle="Aziende, persone e configurazioni da completare."
      actions={<><Button variant="secondary" loading={loading} onClick={() => setRevision(v => v + 1)}>Aggiorna</Button>
        <Button disabled={loading || error || filtered.length === 0} onClick={exportCsv}>Esporta CSV</Button></>} />
    <section className="reports-filters" aria-label="Filtri report">
      <label className="reports-search">Cerca azienda o città<input type="search" value={search} onChange={e => setSearch(e.target.value)} placeholder="Nome azienda o città…" /></label>
      <label>Stato<select aria-label="Stato" value={status} onChange={e => setStatus(e.target.value as typeof status)}>
        <option value="all">Tutti gli stati</option>{Object.entries(labels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
      </select></label>
      <label>Da verificare<select aria-label="Da verificare" value={attention} onChange={e => setAttention(e.target.value as Attention)}>
        <option value="all">Tutte le aziende</option><option value="approval">Da approvare</option>
        <option value="branches">Attive senza filiali</option><option value="employees">Attive senza dipendenti</option>
      </select></label>
      <label>Ordina per<select aria-label="Ordina per" value={sort} onChange={e => setSort(e.target.value)}>
        <option value="newest">Registrazione più recente</option><option value="name">Nome azienda</option><option value="employees">Numero dipendenti</option>
      </select></label>
      <Button variant="ghost" onClick={() => { setSearch(''); setStatus('all'); setAttention('all'); setSort('newest') }}>Azzera filtri</Button>
    </section>
    {error ? <section className="reports-error" role="alert"><h2>Report non disponibile</h2>
      <p>Non è stato possibile recuperare i dati aggiornati. Riprova per visualizzare ed esportare il report.</p>
      <Button variant="secondary" onClick={() => setRevision(v => v + 1)}>Riprova</Button>
    </section> : <>
      <div className="reports-metrics" aria-busy={loading}>{metrics.map(([label, value]) => <div className="reports-metric" key={label}>
        <span>{label}</span><strong>{loading ? <Skeleton variant="text" width="50%" /> : number.format(value)}</strong>
      </div>)}</div>
      <section className="reports-results" aria-busy={loading} aria-label="Aziende nel report">
        <div className="reports-results-heading"><h2>Dettaglio aziende</h2><span role="status">{loading ? 'Caricamento…' : `${number.format(filtered.length)} di ${number.format(merchants.length)} aziende`}</span></div>
        {loading ? <div className="reports-loading"><Skeleton variant="box" height={160} /></div>
          : filtered.length === 0 ? <EmptyState title="Nessuna azienda trovata" description="Non ci sono aziende corrispondenti ai filtri selezionati." />
          : <div className="reports-table-scroll" tabIndex={0} role="region" aria-label="Tabella aziende scorrevole"><table>
            <thead><tr><th scope="col">Azienda</th><th scope="col">Stato</th><th scope="col">Registrazione</th><th scope="col">Dipendenti</th><th scope="col">Filiali</th><th scope="col">Da verificare</th></tr></thead>
            <tbody>{filtered.map(m => <tr key={m.id}>
              <th scope="row"><Link to={`/merchants/${m.id}`}>{m.companyName}</Link><small>{m.city || 'Città non indicata'}</small></th>
              <td><StatusChip variant={statusOf(m) === 'active' ? 'success' : statusOf(m) === 'pending' ? 'warning' : 'neutral'}>{labels[statusOf(m)]}</StatusChip></td>
              <td>{formatBrowserDate(new Date(m.createdAt))}</td><td className="reports-number">{number.format(m.employeeCount)}</td><td className="reports-number">{number.format(m.branchCount)}</td>
              <td>{checksFor(m).length > 0 ? <ul>{checksFor(m).map(check => <li key={check}>{check}</li>)}</ul> : '—'}</td>
            </tr>)}</tbody>
          </table></div>}
      </section>
      <p className="reports-note">I totali e il CSV rispettano i filtri. I dipendenti sono le appartenenze interne attive presso le aziende selezionate: una persona presente in più aziende viene contata più volte. Le filiali includono anche quelle disattivate. Stato e conteggi rappresentano la situazione attuale.
        {!loading && updatedAt && <> Aggiornato alle {updatedAt.toLocaleTimeString('it-IT')}.</>}
      </p>
    </>}
  </div>
}
