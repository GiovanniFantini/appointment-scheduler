import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  EmptyState,
  PageHeader,
  Skeleton,
  SegmentedTabs,
  StatusChip,
  Avatar,
  type StatusChipVariant,
  useToast
} from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './MerchantsPage.css'
import '../styles/admin-cards.css'

interface Merchant {
  id: number
  companyName: string
  city?: string
  vatNumber?: string
  isApproved: boolean
  isActive: boolean
  createdAt: string
}

function getMerchantStatus(m: Merchant): string {
  if (!m.isActive) return 'inactive'
  if (!m.isApproved) return 'pending'
  return 'active'
}

type TabKey = 'all' | 'pending' | 'active' | 'inactive'

const TABS: { key: TabKey; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'pending', label: 'Pending' },
  { key: 'active', label: 'Active' },
  { key: 'inactive', label: 'Inactive' },
]

function statusVariant(status: string): StatusChipVariant {
  switch (status) {
    case 'active': return 'success'
    case 'pending': return 'warning'
    default: return 'neutral'
  }
}

function statusLabel(status: string): string {
  return status.charAt(0).toUpperCase() + status.slice(1)
}

export default function MerchantsPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const toast = useToast()

  const initialTab = (searchParams.get('tab') as TabKey) ?? 'all'
  const [tab, setTab] = useState<TabKey>(initialTab)
  const [merchants, setMerchants] = useState<Merchant[]>([])
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState<number | null>(null)

  const fetchMerchants = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/merchants?status=all')
      const data: Merchant[] = res.data?.data ?? res.data ?? []
      setMerchants(data)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchMerchants()
  }, [])

  const filtered = tab === 'all'
    ? merchants
    : merchants.filter((m) => getMerchantStatus(m) === tab)

  const handleApprove = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.patch(`/merchants/${id}/approve`)
      toast.success('Merchant approvato')
      await fetchMerchants()
    } catch {
      toast.error('Errore durante l\'approvazione')
    } finally {
      setActionLoading(null)
    }
  }

  const handleReject = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.patch(`/merchants/${id}/reject`)
      toast.success('Merchant disattivato')
      await fetchMerchants()
    } catch {
      toast.error('Errore durante l\'operazione')
    } finally {
      setActionLoading(null)
    }
  }

  return (
    <div className="merchants-page">
      <PageHeader title="Merchants" subtitle="Manage merchant accounts and approvals" />

      {/* Filter tabs */}
      <SegmentedTabs<TabKey>
        className="merchants-tabs"
        value={tab}
        onChange={setTab}
        options={TABS.map((t) => ({
          value: t.key,
          label: t.label,
          count: t.key === 'all'
            ? undefined
            : (loading ? undefined : merchants.filter((m) => getMerchantStatus(m) === t.key).length)
        }))}
      />

      {/* Table */}
      <div className="table-card">
        <div className="table-card-header">
          <span className="table-card-title">
            {TABS.find((t) => t.key === tab)?.label} Merchants
          </span>
          <span className="table-count">
            {loading ? '…' : `${filtered.length} result${filtered.length !== 1 ? 's' : ''}`}
          </span>
        </div>

        <div className="table-wrapper">
          {loading ? (
            <div style={{ padding: '0.75rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {[0, 1, 2, 3, 4].map(i => (
                <Skeleton key={i} variant="box" height={36} />
              ))}
            </div>
          ) : filtered.length === 0 ? (
            <EmptyState
              title="Nessun merchant"
              description={tab === 'all'
                ? 'Non ci sono ancora merchant registrati.'
                : `Nessun merchant con stato "${tab}".`}
            />
          ) : (
            <div className="admin-card-grid">
              {filtered.map((m) => {
                const status = getMerchantStatus(m)
                return (
                  <div
                    key={m.id}
                    className="admin-list-card"
                    role="button"
                    tabIndex={0}
                    onClick={() => navigate(`/merchants/${m.id}`)}
                    onKeyDown={(e) => { if (e.key === 'Enter') navigate(`/merchants/${m.id}`) }}
                  >
                    <div className="admin-list-card-head">
                      <Avatar name={m.companyName} size="lg" />
                      <div className="admin-list-card-id">
                        <span className="admin-list-card-title">{m.companyName}</span>
                        <span className="admin-list-card-sub">{m.city ?? '—'}{m.vatNumber ? ` · ${m.vatNumber}` : ''}</span>
                      </div>
                    </div>
                    <div className="admin-list-card-chips">
                      <StatusChip variant={statusVariant(status)}>{statusLabel(status)}</StatusChip>
                    </div>
                    <div className="admin-list-card-meta">
                      <span>Registrato</span>
                      <span className="admin-list-card-date">{formatBrowserDate(new Date(m.createdAt))}</span>
                    </div>
                    <div className="admin-list-card-actions" onClick={(e) => e.stopPropagation()}>
                        {status !== 'active' && (
                          <button
                            className="btn-icon btn-icon-approve"
                            title="Approve"
                            onClick={() => handleApprove(m.id)}
                            disabled={actionLoading === m.id}
                          >
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                              <polyline points="20 6 9 17 4 12" />
                            </svg>
                          </button>
                        )}
                        {status !== 'inactive' && (
                          <button
                            className="btn-icon btn-icon-reject"
                            title="Reject / Deactivate"
                            onClick={() => handleReject(m.id)}
                            disabled={actionLoading === m.id}
                          >
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                              <line x1="18" y1="6" x2="6" y2="18" />
                              <line x1="6" y1="6" x2="18" y2="18" />
                            </svg>
                          </button>
                        )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
