import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Skeleton, Avatar, StatusChip } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './EmployeesTable.css'
import '../styles/admin-cards.css'

// EmployeeKind: Internal=0, External=1 (vedi backend/AppointmentScheduler.Shared/Enums/EmployeeKind.cs)
const KIND_LABEL: Record<number, string> = { 0: 'Interno', 1: 'Esterno' }

interface EmployeeItem {
  id: number
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string | null
  kind: number
  isActive: boolean
  createdAt: string
  merchantCount: number
  primaryMerchantId?: number | null
  primaryMerchantName?: string | null
  hasAccount: boolean
  userId?: number | null
}

interface ListResponse {
  items: EmployeeItem[]
  total: number
  page: number
  pageSize: number
}

const PAGE_SIZE = 25

export interface EmployeesTableProps {
  /** Se valorizzato, la lista è scoped a un solo merchant e nasconde il filtro merchant. */
  merchantId?: number
  /** Mostra/nasconde la barra filtri completa. Default: true. */
  showFilters?: boolean
}

export default function EmployeesTable({ merchantId, showFilters = true }: EmployeesTableProps) {
  const navigate = useNavigate()

  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [kind, setKind] = useState('')
  const [status, setStatus] = useState('')
  const [hasAccount, setHasAccount] = useState('')
  const [page, setPage] = useState(1)

  const [data, setData] = useState<ListResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(t)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, kind, status, hasAccount, merchantId])

  useEffect(() => {
    let cancelled = false
    const fetchEmployees = async () => {
      setLoading(true)
      setError('')
      try {
        const params: Record<string, string | number> = { page, pageSize: PAGE_SIZE }
        if (merchantId) params.merchantId = merchantId
        if (debouncedSearch.trim()) params.search = debouncedSearch.trim()
        if (kind) params.kind = kind
        if (status === 'active') params.isActive = 'true'
        if (status === 'inactive') params.isActive = 'false'
        if (hasAccount === 'true') params.hasAccount = 'true'
        if (hasAccount === 'false') params.hasAccount = 'false'

        const res = await apiClient.get<ListResponse>('/admin/employees', { params })
        if (!cancelled) setData(res.data)
      } catch {
        if (!cancelled) setError('Impossibile caricare gli employee.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    fetchEmployees()
    return () => { cancelled = true }
  }, [debouncedSearch, kind, status, hasAccount, merchantId, page])

  const total = data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const items = data?.items ?? []
  const hasFilters = !!(search || kind || status || hasAccount)

  const resetFilters = () => {
    setSearch('')
    setKind('')
    setStatus('')
    setHasAccount('')
  }

  return (
    <div className="employees-table-wrap">
      {showFilters && (
        <div className="employees-filters">
          <input
            type="search"
            className="employees-filter-input"
            placeholder="Cerca per email o nome…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select className="employees-filter-select" value={kind} onChange={(e) => setKind(e.target.value)}>
            <option value="">Tutti i tipi</option>
            <option value="0">Interno</option>
            <option value="1">Esterno</option>
          </select>
          <select className="employees-filter-select" value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Tutti gli stati</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
          <select className="employees-filter-select" value={hasAccount} onChange={(e) => setHasAccount(e.target.value)}>
            <option value="">Con/senza account</option>
            <option value="true">Con account</option>
            <option value="false">Senza account</option>
          </select>
          {hasFilters && (
            <button className="employees-filter-reset" onClick={resetFilters}>Reset</button>
          )}
        </div>
      )}

      <div className="employees-table-card">
        <div className="employees-table-header">
          <span className="employees-table-title">Employee</span>
          <span className="employees-table-count">{loading ? '…' : `${total} risultati`}</span>
        </div>

        <div className="employees-table-scroll">
          {loading ? (
            <div style={{ padding: '0.75rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {[0, 1, 2, 3, 4].map(i => <Skeleton key={i} variant="box" height={36} />)}
            </div>
          ) : error ? (
            <div className="employees-empty">{error}</div>
          ) : items.length === 0 ? (
            <div className="employees-empty">
              {hasFilters ? 'Nessun risultato per i filtri selezionati.' : 'Nessun employee.'}
            </div>
          ) : (
            <div className="admin-card-grid">
              {items.map((e) => (
                <button key={e.id} className="admin-list-card" onClick={() => navigate(`/employees/${e.id}`)}>
                  <div className="admin-list-card-head">
                    <Avatar name={`${e.firstName} ${e.lastName}`} size="lg" />
                    <div className="admin-list-card-id">
                      <span className="admin-list-card-title">{e.email}</span>
                      <span className="admin-list-card-sub">{e.firstName} {e.lastName}</span>
                    </div>
                  </div>
                  <div className="admin-list-card-chips">
                    <StatusChip variant={e.kind === 1 ? 'accent' : 'success'}>
                      {KIND_LABEL[e.kind] ?? '?'}
                    </StatusChip>
                    <StatusChip variant={e.hasAccount ? 'info' : 'neutral'}>
                      {e.hasAccount ? 'Registrato' : 'Pre-caricato'}
                    </StatusChip>
                    <StatusChip variant={e.isActive ? 'success' : 'neutral'}>
                      {e.isActive ? 'Active' : 'Inactive'}
                    </StatusChip>
                  </div>
                  {!merchantId && e.merchantCount > 0 && (
                    <div className="admin-list-card-meta">
                      {e.merchantCount === 1 ? e.primaryMerchantName : `${e.merchantCount} merchant`}
                      <span className="admin-list-card-date">{formatBrowserDate(new Date(e.createdAt))}</span>
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>

        {!loading && !error && items.length > 0 && (
          <div className="employees-pagination">
            <span>Pagina {page} di {totalPages}</span>
            <div className="employees-pagination-buttons">
              <button disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>← Indietro</button>
              <button disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>Avanti →</button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
