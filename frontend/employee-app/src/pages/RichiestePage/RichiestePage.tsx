import { useState, useEffect, useCallback } from 'react'
import apiClient from '../../lib/axios'
import CreateRequestModal from '../../components/CreateRequestModal/CreateRequestModal'
import { formatBrowserDate, parseDateOnly } from '../../lib/dateUtils'
import './RichiestePage.css'

// Matches EmployeeRequestDto from server
interface ApiEmployeeRequest {
  id: number
  typeName: string         // "Ferie" | "Permessi" | "Malattia"
  statusName: string       // "Pending" | "Approved" | "Rejected"
  startDate: string        // "2024-01-15"
  endDate?: string
  startTime?: string       // "12:00:00"
  endTime?: string
  eventId?: number | null
  notes?: string
}

function formatTime(t?: string): string {
  if (!t) return ''
  return t.slice(0, 5)
}

function getRequestTypeLabel(type?: string): string {
  const map: Record<string, string> = {
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
  }
  return type ? (map[type] ?? type) : 'Richiesta'
}

function getRequestTypeColor(type?: string): string {
  const map: Record<string, string> = {
    Ferie: '#ec4899',
    Permessi: '#8b5cf6',
    Malattia: '#f59e0b',
  }
  return type ? (map[type] ?? '#6366f1') : '#6366f1'
}

function getStatusLabel(status?: string): string {
  const map: Record<string, string> = {
    Pending: 'In attesa',
    Approved: 'Approvata',
    Rejected: 'Rifiutata',
  }
  return status ? (map[status] ?? status) : 'In attesa'
}

function formatDate(dateStr: string): string {
  return formatBrowserDate(parseDateOnly(dateStr))
}

export default function RichiestePage() {
  const [requests, setRequests] = useState<ApiEmployeeRequest[]>([])
  const [approvals, setApprovals] = useState<ApiEmployeeRequest[]>([])
  const [loading, setLoading] = useState(true)
  const [loadingApprovals, setLoadingApprovals] = useState(false)
  const [showApprovalsSection, setShowApprovalsSection] = useState(false)
  const [showModal, setShowModal] = useState(false)
  const [error, setError] = useState('')
  const approvalLevels = new Set(['Operator', 'Manager'])

  const currentFeatureLevel = (() => {
    try {
      const raw = localStorage.getItem('user')
      if (!raw) return undefined
      const parsed = JSON.parse(raw) as { featureLevels?: Record<string, string> }
      return parsed.featureLevels?.Richieste
    } catch {
      return undefined
    }
  })()

  const canApproveRequests = approvalLevels.has(currentFeatureLevel ?? '')

  const fetchRequests = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await apiClient.get<ApiEmployeeRequest[]>('/employee-requests/my')
      const items = Array.isArray(data) ? data : []
      items.sort((a, b) => a.startDate < b.startDate ? 1 : -1)
      setRequests(items)
    } catch {
      setError('Errore nel caricamento delle richieste')
    } finally {
      setLoading(false)
    }
  }, [])

  const fetchApprovals = useCallback(async () => {
    if (!canApproveRequests || !showApprovalsSection) {
      setApprovals([])
      return
    }

    setLoadingApprovals(true)
    try {
      const { data } = await apiClient.get<ApiEmployeeRequest[]>('/employee-requests/approvals')
      const items = Array.isArray(data) ? data : []
      items.sort((a, b) => a.startDate < b.startDate ? 1 : -1)
      setApprovals(items)
    } catch {
      setApprovals([])
    } finally {
      setLoadingApprovals(false)
    }
  }, [canApproveRequests, showApprovalsSection])

  useEffect(() => {
    fetchRequests()
    fetchApprovals()
  }, [fetchApprovals, fetchRequests])

  useEffect(() => {
    if (!canApproveRequests) {
      setShowApprovalsSection(false)
    }
  }, [canApproveRequests])

  const handleApprove = async (id: number) => {
    try {
      await apiClient.post(`/employee-requests/${id}/approve`)
      await fetchApprovals()
    } catch {
      alert('Errore durante l\'approvazione')
    }
  }

  const handleReject = async (id: number) => {
    try {
      await apiClient.post(`/employee-requests/${id}/reject`)
      await fetchApprovals()
    } catch {
      alert('Errore durante il rifiuto')
    }
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Eliminare questa richiesta?')) return
    try {
      await apiClient.delete(`/employee-requests/${id}`)
      await fetchRequests()
      if (showApprovalsSection) await fetchApprovals()
    } catch {
      alert('Errore durante l\'eliminazione della richiesta')
    }
  }

  return (
    <div className="richieste-page">
      <div className="richieste-header">
        <h1 className="richieste-title">Le mie richieste</h1>
        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
          {canApproveRequests && (
            <button
              className="btn-new-request-empty"
              onClick={() => setShowApprovalsSection(prev => !prev)}
            >
              {showApprovalsSection ? 'Nascondi gestione richieste' : 'Gestione richieste'}
            </button>
          )}
          <button className="btn-new-request" onClick={() => setShowModal(true)}>
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M12 5v14M5 12h14" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
            Nuova richiesta
          </button>
        </div>
      </div>

      {error && <div className="richieste-error">{error}</div>}

      {canApproveRequests && showApprovalsSection && (
        <div className="richieste-approvals-section">
          <div className="richieste-header" style={{ marginTop: 0 }}>
            <h2 className="richieste-title" style={{ fontSize: '1.4rem' }}>Gestione richieste dipendenti</h2>
            <p className="richieste-subtitle">Area visibile a operatori e manager per approvare o rifiutare richieste</p>
          </div>

          {loadingApprovals ? (
            <div className="richieste-loading">
              <div className="spinner" />
            </div>
          ) : approvals.length === 0 ? (
            <div className="richieste-empty" style={{ marginTop: 0 }}>
              <p className="empty-title">Nessuna richiesta in attesa</p>
              <p className="empty-subtitle">Le richieste approvabili appariranno qui quando saranno presenti.</p>
            </div>
          ) : (
            <div className="requests-list">
              {approvals.map(req => {
                const color = getRequestTypeColor(req.typeName)
                return (
                  <div key={req.id} className="request-card" style={{ borderLeftColor: color }}>
                    <div className="request-card-top">
                      <span
                        className="request-type-badge"
                        style={{ backgroundColor: color + '22', color }}
                      >
                        {getRequestTypeLabel(req.typeName)}
                      </span>
                      <span className="request-status-badge">{getStatusLabel(req.statusName)}</span>
                    </div>
                    <div className="request-card-dates">
                      <div className="request-date">
                        <span className="request-date-label">Dal</span>
                        <span className="request-date-value">{formatDate(req.startDate)}</span>
                      </div>
                      {req.endDate && (
                        <div className="request-date">
                          <span className="request-date-label">Al</span>
                          <span className="request-date-value">{formatDate(req.endDate)}</span>
                        </div>
                      )}
                      {req.startTime && req.endTime && (
                        <div className="request-date">
                          <span className="request-date-label">Orario</span>
                          <span className="request-date-value">{formatTime(req.startTime)} - {formatTime(req.endTime)}</span>
                        </div>
                      )}
                    </div>
                    {req.eventId != null && (
                      <p className="request-notes"><strong>Collegato al turno #{req.eventId}</strong></p>
                    )}
                    {req.notes && (
                      <p className="request-notes">{req.notes}</p>
                    )}
                    <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
                      <button className="btn-new-request" onClick={() => handleApprove(req.id)}>Approva</button>
                      <button className="btn-new-request-empty" onClick={() => handleReject(req.id)}>Rifiuta</button>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      )}

      {loading ? (
        <div className="richieste-loading">
          <div className="spinner" />
        </div>
      ) : requests.length === 0 ? (
        <div className="richieste-empty">
          <div className="empty-icon">
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
          </div>
          <p className="empty-title">Nessuna richiesta</p>
          <p className="empty-subtitle">Le tue richieste di ferie, permessi e malattia appariranno qui.</p>
          <button className="btn-new-request-empty" onClick={() => setShowModal(true)}>
            Crea la prima richiesta
          </button>
        </div>
      ) : (
        <div className="requests-list">
          {requests.map(req => {
            const color = getRequestTypeColor(req.typeName)
            return (
              <div key={req.id} className="request-card" style={{ borderLeftColor: color }}>
                <div className="request-card-top">
                  <span
                    className="request-type-badge"
                    style={{ backgroundColor: color + '22', color }}
                  >
                    {getRequestTypeLabel(req.typeName)}
                  </span>
                  <span className="request-status-badge">{getStatusLabel(req.statusName)}</span>
                </div>
                <div className="request-card-dates">
                  <div className="request-date">
                    <span className="request-date-label">Dal</span>
                    <span className="request-date-value">{formatDate(req.startDate)}</span>
                  </div>
                  {req.endDate && (
                    <div className="request-date">
                      <span className="request-date-label">Al</span>
                      <span className="request-date-value">{formatDate(req.endDate)}</span>
                    </div>
                  )}
                  {req.startTime && req.endTime && (
                    <div className="request-date">
                      <span className="request-date-label">Orario</span>
                      <span className="request-date-value">{formatTime(req.startTime)} - {formatTime(req.endTime)}</span>
                    </div>
                  )}
                </div>
                {req.eventId != null && (
                  <p className="request-notes"><strong>Collegato al turno #{req.eventId}</strong></p>
                )}
                {req.notes && (
                  <p className="request-notes">{req.notes}</p>
                )}
                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.75rem' }}>
                  <button className="btn-new-request-empty" onClick={() => handleDelete(req.id)}>
                    Elimina richiesta
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {showModal && (
        <CreateRequestModal
          onClose={() => setShowModal(false)}
          onCreated={fetchRequests}
        />
      )}
    </div>
  )
}
