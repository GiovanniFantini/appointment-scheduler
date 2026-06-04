import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader, Skeleton, Avatar, StatusChip, type StatusChipVariant } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './UsersPage.css'
import '../styles/admin-cards.css'

// Allineato a backend/AppointmentScheduler.Shared/Enums/AccountType.cs
const ACCOUNT_TYPE: Record<number, string> = {
  1: 'Admin',
  2: 'Merchant',
  3: 'Employee',
}

function accountTypeVariant(t: number): StatusChipVariant {
  switch (t) {
    case 1: return 'accent'
    case 2: return 'info'
    default: return 'success'
  }
}

interface UserItem {
  id: number
  email: string
  firstName: string
  lastName: string
  accountType: number
  isActive: boolean
  merchantId?: number
  merchantName?: string
  employeeId?: number
  createdAt: string
}

interface UserListResponse {
  items: UserItem[]
  total: number
  page: number
  pageSize: number
}

const PAGE_SIZE = 25

export default function UsersPage() {
  const navigate = useNavigate()

  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [accountType, setAccountType] = useState<string>('')
  const [status, setStatus] = useState<string>('')
  const [page, setPage] = useState(1)

  const [data, setData] = useState<UserListResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // Debounce search input
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(t)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, accountType, status])

  useEffect(() => {
    let cancelled = false
    const fetchUsers = async () => {
      setLoading(true)
      setError('')
      try {
        const params: Record<string, string | number> = { page, pageSize: PAGE_SIZE }
        if (debouncedSearch.trim()) params.search = debouncedSearch.trim()
        if (accountType) params.accountType = accountType
        if (status === 'active') params.isActive = 'true'
        if (status === 'inactive') params.isActive = 'false'

        const res = await apiClient.get<UserListResponse>('/admin/users', { params })
        if (!cancelled) setData(res.data)
      } catch {
        if (!cancelled) setError('Impossibile caricare gli utenti.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    fetchUsers()
    return () => { cancelled = true }
  }, [debouncedSearch, accountType, status, page])

  const total = data?.total ?? 0
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const items = data?.items ?? []
  const hasFilters = !!(search || accountType || status)

  const resetFilters = () => {
    setSearch('')
    setAccountType('')
    setStatus('')
  }

  return (
    <div className="users-page">
      <PageHeader title="Users" subtitle="Gestione utenti della piattaforma" />

      {/* Filters */}
      <div className="users-filters">
        <input
          type="search"
          className="users-filter-input"
          placeholder="Cerca per email o nome…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="users-filter-select"
          value={accountType}
          onChange={(e) => setAccountType(e.target.value)}
        >
          <option value="">Tutti i tipi</option>
          <option value="1">Admin</option>
          <option value="2">Merchant</option>
          <option value="3">Employee</option>
        </select>
        <select
          className="users-filter-select"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">Tutti gli stati</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
        {hasFilters && (
          <button className="users-filter-reset" onClick={resetFilters}>Reset</button>
        )}
      </div>

      {/* Table */}
      <div className="users-table-card">
        <div className="users-table-header">
          <span className="users-table-title">Utenti</span>
          <span className="users-table-count">
            {loading ? '…' : `${total} risultati`}
          </span>
        </div>

        <div className="users-table-wrapper">
          {loading ? (
            <div style={{ padding: '0.75rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {[0, 1, 2, 3, 4].map(i => (
                <Skeleton key={i} variant="box" height={36} />
              ))}
            </div>
          ) : error ? (
            <div className="users-empty">{error}</div>
          ) : items.length === 0 ? (
            <div className="users-empty">
              {hasFilters ? 'Nessun risultato per i filtri selezionati.' : 'Nessun utente.'}
            </div>
          ) : (
            <div className="admin-card-grid">
              {items.map((u) => (
                <button key={u.id} className="admin-list-card" onClick={() => navigate(`/users/${u.id}`)}>
                  <div className="admin-list-card-head">
                    <Avatar name={`${u.firstName} ${u.lastName}`} size="lg" />
                    <div className="admin-list-card-id">
                      <span className="admin-list-card-title">{u.email}</span>
                      <span className="admin-list-card-sub">{u.firstName} {u.lastName}</span>
                    </div>
                  </div>
                  <div className="admin-list-card-chips">
                    <StatusChip variant={accountTypeVariant(u.accountType)}>
                      {ACCOUNT_TYPE[u.accountType] ?? '?'}
                    </StatusChip>
                    <StatusChip variant={u.isActive ? 'success' : 'neutral'}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </StatusChip>
                  </div>
                  <div className="admin-list-card-meta">
                    <span>{u.merchantName ?? '—'}</span>
                    <span className="admin-list-card-date">{formatBrowserDate(new Date(u.createdAt))}</span>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {!loading && !error && items.length > 0 && (
          <div className="users-pagination">
            <span>Pagina {page} di {totalPages}</span>
            <div className="users-pagination-buttons">
              <button disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>← Indietro</button>
              <button disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>Avanti →</button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
