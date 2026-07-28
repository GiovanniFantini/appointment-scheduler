import { useState } from 'react'
import { timeClockApi, type TimeClockAnomaly } from '../../lib/api/timeClockManagement'
import { TimeClockAnomalyType, anomalyReasonLabel, anomalyTypeLabel } from '../../types/timbratura'
import './ReviewJustificationModal.css'

interface Props {
  anomaly: TimeClockAnomaly
  onClose: () => void
  onReviewed: (updated: TimeClockAnomaly) => void
}

export default function ReviewJustificationModal({ anomaly, onClose, onReviewed }: Props) {
  const [notes, setNotes] = useState('')
  const [saving, setSaving] = useState<'approve' | 'reject' | null>(null)
  const [error, setError] = useState('')

  // Solo l'entrata mancante rappresenta una giornata intera non lavorata: è l'unico
  // caso in cui il rifiuto fa scattare l'inserimento automatico delle ferie.
  const rejectionCreatesLeave = anomaly.type === TimeClockAnomalyType.MissingClockIn

  const review = async (action: 'approve' | 'reject') => {
    setSaving(action)
    setError('')
    try {
      const trimmed = notes.trim()
      const updated = action === 'approve'
        ? await timeClockApi.approveAnomaly(anomaly.id, trimmed || undefined)
        : await timeClockApi.rejectAnomaly(anomaly.id, trimmed || undefined)
      onReviewed(updated)
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante la revisione del giustificativo.')
      setSaving(null)
    }
  }

  return (
    <div className="rjm-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="rjm-box">
        <div className="rjm-header">
          <h2 className="rjm-title">Giustificativo timbratura</h2>
          <button className="rjm-close" onClick={onClose}>✕</button>
        </div>

        <div className="rjm-body">
          {error && <div className="rjm-error">{error}</div>}

          <div className="rjm-summary">
            <span className="rjm-employee">{anomaly.employeeName}</span>
            <span className="rjm-anomaly">{anomalyTypeLabel(anomaly.type, anomaly.typeName)}</span>
            <span className="rjm-date">
              {new Date(anomaly.workDate).toLocaleDateString('it-IT', {
                weekday: 'long', day: '2-digit', month: 'long',
              })}
              {anomaly.eventTitle ? ` — ${anomaly.eventTitle}` : ''}
            </span>
          </div>

          <div className="rjm-field">
            <span className="rjm-label">Motivazione del dipendente</span>
            <p className="rjm-reason">
              {anomalyReasonLabel(anomaly.employeeReason ?? undefined, anomaly.employeeReasonName ?? undefined)}
            </p>
            {anomaly.employeeNotes && <p className="rjm-notes">"{anomaly.employeeNotes}"</p>}
          </div>

          <div className="rjm-field">
            <label className="rjm-label" htmlFor="rjm-review-notes">Note per il dipendente (facoltative)</label>
            <textarea
              id="rjm-review-notes"
              className="rjm-textarea"
              rows={3}
              placeholder="Motiva la decisione…"
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          {rejectionCreatesLeave && (
            <div className="rjm-warning">
              Respingendo il giustificativo la giornata verrà registrata come ferie approvate del dipendente.
            </div>
          )}
        </div>

        <div className="rjm-footer">
          <button
            type="button"
            className="rjm-btn-reject"
            disabled={saving !== null}
            onClick={() => review('reject')}
          >
            {saving === 'reject' ? 'Invio…' : 'Respingi'}
          </button>
          <button
            type="button"
            className="rjm-btn-approve"
            disabled={saving !== null}
            onClick={() => review('approve')}
          >
            {saving === 'approve' ? 'Invio…' : 'Approva'}
          </button>
        </div>
      </div>
    </div>
  )
}
