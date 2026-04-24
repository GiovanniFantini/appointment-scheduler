import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './MerchantsPage.css'

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

function statusClass(status: string) {
  switch (status) {
    case 'active': return 'status-badge status-active'
    case 'pending': return 'status-badge status-pending'
    default: return 'status-badge status-inactive'
  }
}

export default function MerchantsPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

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
      await fetchMerchants()
    } finally {
      setActionLoading(null)
    }
  }

  const handleReject = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.patch(`/merchants/${id}/reject`)
      await fetchMerchants()
    } finally {
      setActionLoading(null)
    }
  }

  return (
    <div className="merchants-page">
      <div className="page-header">
        <h1 className="page-title">Merchants</h1>
        <p className="page-subtitle">Manage merchant accounts and approvals</p>
      </div>

      {/* Filter tabs */}
      <div className="filter-tabs">
        {TABS.map((t) => (
          <button
            key={t.key}
            className={`filter-tab${tab === t.key ? ' active' : ''}`}
            onClick={() => setTab(t.key)}
          >
            {t.label}
            {t.key !== 'all' && !loading && (
              <> ({merchants.filter((m) => getMerchantStatus(m) === t.key).length})</>
            )}
          </button>
        ))}
      </div>

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
            <div className="table-loading">Loading merchants…</div>
          ) : filtered.length === 0 ? (
            <div className="table-empty">No merchants found for this filter.</div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Company Name</th>
                  <th>City</th>
                  <th>VAT Number</th>
                  <th>Status</th>
                  <th>Registered</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((m) => (
                  <tr key={m.id}>
                    <td style={{ fontWeight: 600 }}>{m.companyName}</td>
                    <td className="td-secondary">{m.city ?? '—'}</td>
                    <td className="td-secondary">{m.vatNumber ?? '—'}</td>
                    <td>
                        <span className={statusClass(getMerchantStatus(m))}>
                          {getMerchantStatus(m).charAt(0).toUpperCase() + getMerchantStatus(m).slice(1)}
                        </span>
                    </td>
                    <td className="td-secondary">
                      {formatBrowserDate(new Date(m.createdAt))}
                    </td>
                    <td>
                      <div className="action-buttons">
                        {/* View */}
                        <button
                          className="btn-icon btn-icon-view"
                          title="View details"
                          onClick={() => navigate(`/merchants/${m.id}`)}
                        >
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                            <circle cx="12" cy="12" r="3" />
                          </svg>
                        </button>

                        {/* Approve (show only if pending/inactive) */}
                        {getMerchantStatus(m) !== 'active' && (
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

                        {/* Reject (show only if pending/active) */}
                        {getMerchantStatus(m) !== 'inactive' && (
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
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}
