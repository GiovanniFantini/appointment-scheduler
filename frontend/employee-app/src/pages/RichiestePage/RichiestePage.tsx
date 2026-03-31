import { useState, useEffect, useCallback } from 'react'
import apiClient from '../../lib/axios'
import CreateRequestModal from '../../components/CreateRequestModal/CreateRequestModal'
import './RichiestePage.css'

interface RequestEvent {
  id: number | string
  title: string
  start: string
  end?: string
  eventType?: string
  allDay?: boolean
  notes?: string
}

function getEventTypeLabel(type?: string): string {
  const map: Record<string, string> = {
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
  }
  return type ? (map[type] ?? type) : 'Richiesta'
}

function getEventTypeColor(type?: string): string {
  const map: Record<string, string> = {
    Ferie: '#ec4899',
    Permessi: '#8b5cf6',
    Malattia: '#f59e0b',
  }
  return type ? (map[type] ?? '#6366f1') : '#6366f1'
}

function formatDate(dateStr: string, allDay?: boolean): string {
  const d = new Date(dateStr)
  if (allDay) {
    return d.toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' })
  }
  return d.toLocaleString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

export default function RichiestePage() {
  const [requests, setRequests] = useState<RequestEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [error, setError] = useState('')

  const fetchRequests = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await apiClient.get<RequestEvent[]>('/events/employee')
      const events = Array.isArray(data) ? data : []
      const filtered = events.filter(e =>
        ['Ferie', 'Permessi', 'Malattia'].includes(e.eventType ?? '')
      )
      // Sort by start date desc
      filtered.sort((a, b) => new Date(b.start).getTime() - new Date(a.start).getTime())
      setRequests(filtered)
    } catch {
      setError('Errore nel caricamento delle richieste')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchRequests()
  }, [fetchRequests])

  return (
    <div className="richieste-page">
      <div className="richieste-header">
        <h1 className="richieste-title">Le mie richieste</h1>
        <button className="btn-new-request" onClick={() => setShowModal(true)}>
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 5v14M5 12h14" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
          </svg>
          Nuova richiesta
        </button>
      </div>

      {error && <div className="richieste-error">{error}</div>}

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
            const color = getEventTypeColor(req.eventType)
            return (
              <div key={req.id} className="request-card" style={{ borderLeftColor: color }}>
                <div className="request-card-top">
                  <span
                    className="request-type-badge"
                    style={{ backgroundColor: color + '22', color }}
                  >
                    {getEventTypeLabel(req.eventType)}
                  </span>
                  <span className="request-status-badge">Inviata</span>
                </div>
                <div className="request-card-dates">
                  <div className="request-date">
                    <span className="request-date-label">Dal</span>
                    <span className="request-date-value">{formatDate(req.start, req.allDay)}</span>
                  </div>
                  {req.end && (
                    <div className="request-date">
                      <span className="request-date-label">Al</span>
                      <span className="request-date-value">{formatDate(req.end, req.allDay)}</span>
                    </div>
                  )}
                </div>
                {req.notes && (
                  <p className="request-notes">{req.notes}</p>
                )}
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
