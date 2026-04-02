import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import './DashboardPage.css'

interface Merchant {
  id: number
  companyName: string
  city?: string
  isApproved: boolean
  isActive: boolean
  createdAt: string
  employeeCount?: number
}

function getMerchantStatus(m: Merchant): string {
  if (!m.isActive) return 'inactive'
  if (!m.isApproved) return 'pending'
  return 'active'
}

interface Stats {
  total: number
  active: number
  pending: number
  totalEmployees: number
}

export default function DashboardPage() {
  const [merchants, setMerchants] = useState<Merchant[]>([])
  const [stats, setStats] = useState<Stats>({ total: 0, active: 0, pending: 0, totalEmployees: 0 })
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState<number | null>(null)

  const fetchData = async () => {
    try {
      const res = await apiClient.get('/merchants?status=all')
      const data: Merchant[] = res.data?.data ?? res.data ?? []
      setMerchants(data)
      setStats({
        total: data.length,
        active: data.filter((m) => getMerchantStatus(m) === 'active').length,
        pending: data.filter((m) => getMerchantStatus(m) === 'pending').length,
        totalEmployees: data.reduce((sum, m) => sum + (m.employeeCount ?? 0), 0),
      })
    } catch {
      // silently fail – stats stay at 0
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchData()
  }, [])

  const handleApprove = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.patch(`/merchants/${id}/approve`)
      await fetchData()
    } finally {
      setActionLoading(null)
    }
  }

  const handleReject = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.patch(`/merchants/${id}/reject`)
      await fetchData()
    } finally {
      setActionLoading(null)
    }
  }

  const pendingMerchants = merchants.filter((m) => getMerchantStatus(m) === 'pending').slice(0, 8)

  const statCards = [
    {
      label: 'Total Merchants',
      value: stats.total,
      desc: 'All registered merchants',
      iconColor: 'indigo',
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
          <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
        </svg>
      ),
    },
    {
      label: 'Active Merchants',
      value: stats.active,
      desc: 'Currently operating',
      iconColor: 'green',
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
          <polyline points="20 6 9 17 4 12" />
        </svg>
      ),
    },
    {
      label: 'Pending Approval',
      value: stats.pending,
      desc: 'Awaiting review',
      iconColor: 'amber',
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
      ),
    },
    {
      label: 'Total Employees',
      value: stats.totalEmployees,
      desc: 'Across all merchants',
      iconColor: 'red',
      icon: (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
          <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
        </svg>
      ),
    },
  ]

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Platform overview and pending actions</p>
      </div>

      {/* Stats */}
      <div className="stats-grid">
        {statCards.map((card) => (
          <div key={card.label} className="stat-card">
            <div className="stat-card-header">
              <span className="stat-card-label">{card.label}</span>
              <div className={`stat-card-icon ${card.iconColor}`}>{card.icon}</div>
            </div>
            <div className="stat-card-value">{loading ? '—' : card.value}</div>
            <div className="stat-card-desc">{card.desc}</div>
          </div>
        ))}
      </div>

      {/* Pending approvals */}
      <div className="section-card">
        <div className="section-card-header">
          <span className="section-card-title">Recent Pending Approvals</span>
          {stats.pending > 0 && (
            <span className="badge badge-amber">{stats.pending} pending</span>
          )}
        </div>

        {loading ? (
          <div className="loading-state">Loading merchants…</div>
        ) : pendingMerchants.length === 0 ? (
          <div className="empty-state">No merchants pending approval</div>
        ) : (
          <div className="pending-list">
            {pendingMerchants.map((m) => (
              <div key={m.id} className="pending-item">
                <div className="pending-info">
                  <span className="pending-company">{m.companyName}</span>
                  <span className="pending-meta">
                    {m.city ? `${m.city} · ` : ''}
                    Registered {new Date(m.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <div className="pending-actions">
                  <button
                    className="btn-sm btn-approve"
                    onClick={() => handleApprove(m.id)}
                    disabled={actionLoading === m.id}
                  >
                    {actionLoading === m.id ? '…' : 'Approve'}
                  </button>
                  <button
                    className="btn-sm btn-reject"
                    onClick={() => handleReject(m.id)}
                    disabled={actionLoading === m.id}
                  >
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        <Link to="/merchants?tab=pending" className="view-all-link">
          View all merchants →
        </Link>
      </div>
    </div>
  )
}
