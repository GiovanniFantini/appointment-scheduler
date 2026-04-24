import { useState, useEffect } from 'react'
import apiClient from '../../lib/axios'
import { formatBrowserDate, parseDateOnly } from '../../lib/dateUtils'
import './RichiestePage.css'

interface EmployeeRequestDto {
  id: number
  employeeId: number
  employeeFullName: string
  employeeInitials: string
  type: number
  typeName: string
  status: number
  statusName: string
  startDate: string
  endDate?: string
  startTime?: string
  endTime?: string
  eventId?: number | null
  notes?: string
  reviewNotes?: string
  reviewedAt?: string
  createdAt: string
}

function formatTimeRange(startTime?: string, endTime?: string): string {
  if (!startTime || !endTime) return ''
  return `${startTime.slice(0, 5)} - ${endTime.slice(0, 5)}`
}

const TYPE_CSS: Record<string, string> = {
  Ferie: 'ferie',
  CambioTurno: 'cambioturno',
  Permessi: 'permessi',
  Malattia: 'malattia',
}

const TYPE_LABELS: Record<string, string> = {
  Ferie: 'Ferie',
  CambioTurno: 'Cambio Turno',
  Permessi: 'Permessi',
  Malattia: 'Malattia',
}

function formatDateRange(startDate: string, endDate?: string): string {
  const fmt = (d: string) => formatBrowserDate(parseDateOnly(d))
  if (!endDate || endDate === startDate) return fmt(startDate)
  return `${fmt(startDate)} - ${fmt(endDate)}`
}

export default function RichiestePage() {
  const [richieste, setRichieste] = useState<EmployeeRequestDto[]>([])
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState<'pending' | 'approved' | 'rejected'>('pending')
  const [actionLoading, setActionLoading] = useState<number | null>(null)
  const [error, setError] = useState('')

  const fetchRichieste = async () => {
    setLoading(true)
    setError('')
    try {
      const res = await apiClient.get('/employee-requests')
      if (Array.isArray(res.data)) {
        setRichieste(res.data as EmployeeRequestDto[])
      }
    } catch {
      setError('Errore nel caricamento delle richieste')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchRichieste() }, [])

  const handleApprove = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.post(`/employee-requests/${id}/approve`)
      setRichieste(prev => prev.map(r => r.id === id ? { ...r, status: 1, statusName: 'Approved' } : r))
    } catch {
      alert('Errore durante l\'approvazione')
    } finally {
      setActionLoading(null)
    }
  }

  const handleReject = async (id: number) => {
    setActionLoading(id)
    try {
      await apiClient.post(`/employee-requests/${id}/reject`)
      setRichieste(prev => prev.map(r => r.id === id ? { ...r, status: 2, statusName: 'Rejected' } : r))
    } catch {
      alert('Errore durante il rifiuto')
    } finally {
      setActionLoading(null)
    }
  }

  const filtered = richieste.filter(r => {
    if (activeTab === 'pending') return r.status === 0
    if (activeTab === 'approved') return r.status === 1
    if (activeTab === 'rejected') return r.status === 2
    return true
  })

  const pendingCount = richieste.filter(r => r.status === 0).length
  const approvedCount = richieste.filter(r => r.status === 1).length
  const rejectedCount = richieste.filter(r => r.status === 2).length

  return (
    <div className="richieste-page">
      <div className="page-header">
        <h1 className="page-title">Richieste</h1>
        <p className="page-subtitle">Gestisci le richieste dei dipendenti</p>
      </div>

      {error && <div className="dev-notice">{error}</div>}

      <div className="richieste-tabs">
        <button
          className={`tab-btn ${activeTab === 'pending' ? 'active' : ''}`}
          onClick={() => setActiveTab('pending')}
        >
          In attesa {pendingCount > 0 && <span className="tab-badge">{pendingCount}</span>}
        </button>
        <button
          className={`tab-btn ${activeTab === 'approved' ? 'active' : ''}`}
          onClick={() => setActiveTab('approved')}
        >
          Approvate {approvedCount > 0 && <span className="tab-badge approved">{approvedCount}</span>}
        </button>
        <button
          className={`tab-btn ${activeTab === 'rejected' ? 'active' : ''}`}
          onClick={() => setActiveTab('rejected')}
        >
          Rifiutate {rejectedCount > 0 && <span className="tab-badge rejected">{rejectedCount}</span>}
        </button>
      </div>

      <div className="richieste-card">
        {loading ? (
          <div className="empty-state">
            <div className="empty-text">Caricamento...</div>
          </div>
        ) : filtered.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">
              {activeTab === 'pending' ? '✅' : activeTab === 'approved' ? '📋' : '❌'}
            </div>
            <div className="empty-text">
              {activeTab === 'pending' ? 'Nessuna richiesta in attesa' :
               activeTab === 'approved' ? 'Nessuna richiesta approvata' :
               'Nessuna richiesta rifiutata'}
            </div>
          </div>
        ) : (
          filtered.map(r => (
            <div key={r.id} className="richiesta-item">
              <div className="richiesta-avatar">{r.employeeInitials}</div>
              <div className="richiesta-info">
                <div className="richiesta-name">{r.employeeFullName}</div>
                <div className="richiesta-detail">
                  {formatDateRange(r.startDate, r.endDate)}
                  {r.startTime && r.endTime && <> · {formatTimeRange(r.startTime, r.endTime)}</>}
                  {r.eventId != null && <> · turno #{r.eventId}</>}
                  {r.notes && <> · {r.notes}</>}
                </div>
              </div>
              <span className={`richiesta-type ${TYPE_CSS[r.typeName] ?? ''}`}>
                {TYPE_LABELS[r.typeName] ?? r.typeName}
              </span>
              {activeTab === 'pending' && (
                <div className="richiesta-actions">
                  <button
                    className="btn-approve"
                    onClick={() => handleApprove(r.id)}
                    disabled={actionLoading === r.id}
                  >
                    {actionLoading === r.id ? '...' : 'Approva'}
                  </button>
                  <button
                    className="btn-reject"
                    onClick={() => handleReject(r.id)}
                    disabled={actionLoading === r.id}
                  >
                    {actionLoading === r.id ? '...' : 'Rifiuta'}
                  </button>
                </div>
              )}
              {activeTab !== 'pending' && (
                <span className={`status-chip ${activeTab}`}>
                  {activeTab === 'approved' ? 'Approvata' : 'Rifiutata'}
                </span>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  )
}
