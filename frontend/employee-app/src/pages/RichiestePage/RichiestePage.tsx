import { useState, useEffect, useCallback } from 'react'
import {
  useConfirm,
  useToast,
  SegmentedTabs,
  StatusChip,
  type StatusChipVariant,
  TiBeach,
  TiClock,
  TiPlus
} from '@scheduler/ui'
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

// Subset di ShiftConflictDto restituito dal backend in caso di turno sovrapposto (HTTP 409).
interface ShiftConflict {
  message: string
  employeeFullName?: string
  conflictingEventTitle?: string
}

type StatusFilter = 'Pending' | 'Approved' | 'Rejected'

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

// Variante chip per tipo richiesta (mockup: ferie=blu, permesso=accent, malattia=ambra).
function getRequestTypeVariant(type?: string): StatusChipVariant {
  const map: Record<string, StatusChipVariant> = {
    Ferie: 'info',
    Permessi: 'accent',
    Malattia: 'warning',
  }
  return type ? (map[type] ?? 'accent') : 'accent'
}

function getRequestTypeIcon(type?: string) {
  if (type === 'Malattia' || type === 'Permessi') return <TiClock size={12} />
  return <TiBeach size={12} />
}

function getStatusLabel(status?: string): string {
  const map: Record<string, string> = {
    Pending: 'In attesa',
    Approved: 'Approvata',
    Rejected: 'Rifiutata',
  }
  return status ? (map[status] ?? status) : 'In attesa'
}

function getStatusVariant(status?: string): StatusChipVariant {
  const map: Record<string, StatusChipVariant> = {
    Pending: 'neutral',
    Approved: 'success',
    Rejected: 'danger',
  }
  return status ? (map[status] ?? 'neutral') : 'neutral'
}

function formatDate(dateStr: string): string {
  return formatBrowserDate(parseDateOnly(dateStr))
}

export default function RichiestePage() {
  const toast = useToast()
  const confirm = useConfirm()
  const [requests, setRequests] = useState<ApiEmployeeRequest[]>([])
  const [approvals, setApprovals] = useState<ApiEmployeeRequest[]>([])
  const [loading, setLoading] = useState(true)
  const [loadingApprovals, setLoadingApprovals] = useState(false)
  const [showApprovalsSection, setShowApprovalsSection] = useState(false)
  const [showModal, setShowModal] = useState(false)
  const [error, setError] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Pending')
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

  const handleApprove = async (id: number, force = false) => {
    try {
      await apiClient.post(`/employee-requests/${id}/approve`, { force })
      toast.success('Richiesta approvata')
      await fetchApprovals()
    } catch (err) {
      // 409: il dipendente ha già un turno sovrapposto. Avvisa e, su conferma, riprova con force.
      const e = err as {
        response?: { status?: number; data?: { message?: string; conflicts?: ShiftConflict[] } }
      }
      const conflicts = e.response?.status === 409 ? e.response.data?.conflicts : undefined
      if (conflicts && conflicts.length > 0) {
        const ok = await confirm({
          title: 'Turno sovrapposto',
          message: (
            <div>
              <p>{e.response?.data?.message ?? 'Il dipendente è già assegnato a un turno nelle date richieste.'}</p>
              <ul className="approval-conflict-list">
                {conflicts.map((c, idx) => (
                  <li key={idx}>{c.message}</li>
                ))}
              </ul>
              <p>Approvare comunque? Il turno resterà scoperto.</p>
            </div>
          ),
          variant: 'warning',
          confirmLabel: 'Approva comunque',
        })
        if (ok) await handleApprove(id, true)
        return
      }
      toast.error('Errore durante l\'approvazione')
    }
  }

  const handleReject = async (id: number) => {
    const ok = await confirm({
      title: 'Rifiutare richiesta',
      message: 'Vuoi davvero rifiutare questa richiesta?',
      variant: 'warning',
      confirmLabel: 'Rifiuta',
    })
    if (!ok) return
    try {
      await apiClient.post(`/employee-requests/${id}/reject`)
      toast.success('Richiesta rifiutata')
      await fetchApprovals()
    } catch {
      toast.error('Errore durante il rifiuto')
    }
  }

  const handleDelete = async (id: number) => {
    const ok = await confirm({
      title: 'Eliminare richiesta',
      message: 'Eliminare questa richiesta?',
      variant: 'danger',
      confirmLabel: 'Elimina',
    })
    if (!ok) return
    try {
      await apiClient.delete(`/employee-requests/${id}`)
      toast.success('Richiesta eliminata')
      await fetchRequests()
      if (showApprovalsSection) await fetchApprovals()
    } catch {
      toast.error('Errore durante l\'eliminazione della richiesta')
    }
  }

  // Conteggi e filtro per le tab segmentate (puro filtro client).
  const counts = {
    Pending: requests.filter(r => r.statusName === 'Pending').length,
    Approved: requests.filter(r => r.statusName === 'Approved').length,
    Rejected: requests.filter(r => r.statusName === 'Rejected').length,
  }
  const visibleRequests = requests.filter(r => r.statusName === statusFilter)

  const renderCard = (req: ApiEmployeeRequest, actions: React.ReactNode) => (
    <div key={req.id} className="request-card">
      <div className="request-card-top">
        <StatusChip variant={getRequestTypeVariant(req.typeName)} icon={getRequestTypeIcon(req.typeName)}>
          {getRequestTypeLabel(req.typeName)}
        </StatusChip>
        <StatusChip variant={getStatusVariant(req.statusName)}>{getStatusLabel(req.statusName)}</StatusChip>
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
      {req.notes && <p className="request-notes">{req.notes}</p>}
      {actions}
    </div>
  )

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
            <TiPlus size={18} />
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
              {approvals.map(req =>
                renderCard(
                  req,
                  <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
                    <button className="btn-new-request" onClick={() => handleApprove(req.id)}>Approva</button>
                    <button className="btn-new-request-empty" onClick={() => handleReject(req.id)}>Rifiuta</button>
                  </div>
                )
              )}
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
        <>
          <SegmentedTabs<StatusFilter>
            className="richieste-tabs"
            value={statusFilter}
            onChange={setStatusFilter}
            options={[
              { value: 'Pending', label: 'In attesa', count: counts.Pending },
              { value: 'Approved', label: 'Approvate', count: counts.Approved },
              { value: 'Rejected', label: 'Rifiutate', count: counts.Rejected },
            ]}
          />
          {visibleRequests.length === 0 ? (
            <div className="richieste-empty" style={{ marginTop: '1rem' }}>
              <p className="empty-title">Nessuna richiesta {getStatusLabel(statusFilter).toLowerCase()}</p>
            </div>
          ) : (
            <div className="requests-list">
              {visibleRequests.map(req =>
                renderCard(
                  req,
                  req.statusName === 'Pending' ? (
                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.75rem' }}>
                      <button className="btn-new-request-empty" onClick={() => handleDelete(req.id)}>
                        Elimina richiesta
                      </button>
                    </div>
                  ) : null
                )
              )}
            </div>
          )}
        </>
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
