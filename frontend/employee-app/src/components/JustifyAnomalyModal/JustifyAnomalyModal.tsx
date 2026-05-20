import { useState } from 'react'
import { timeClockApi } from '../../lib/api/timeClock'
import { TimeClockAnomalyReason } from '../../types/timbratura'
import type { TimeClockAnomalyDto } from '../../types/timbratura'
import './JustifyAnomalyModal.css'

interface Props {
  anomaly: TimeClockAnomalyDto
  onClose: () => void
  onJustified: (updated: TimeClockAnomalyDto) => void
}

const REASON_OPTIONS: { value: TimeClockAnomalyReason; label: string }[] = [
  { value: TimeClockAnomalyReason.Traffic, label: 'Traffico / imprevisto di viaggio' },
  { value: TimeClockAnomalyReason.AuthorizedLeave, label: 'Permesso autorizzato' },
  { value: TimeClockAnomalyReason.TimeRecovery, label: 'Recupero ore' },
  { value: TimeClockAnomalyReason.PersonalEmergency, label: 'Emergenza personale' },
  { value: TimeClockAnomalyReason.Forgotten, label: 'Dimenticanza della timbratura' },
  { value: TimeClockAnomalyReason.TechnicalIssue, label: 'Problema tecnico' },
  { value: TimeClockAnomalyReason.SmartWorking, label: 'Lavoro da remoto' },
  { value: TimeClockAnomalyReason.Other, label: 'Altro' },
]

export default function JustifyAnomalyModal({ anomaly, onClose, onJustified }: Props) {
  const [reason, setReason] = useState<TimeClockAnomalyReason>(REASON_OPTIONS[0].value)
  const [notes, setNotes] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const updated = await timeClockApi.justifyAnomaly(anomaly.id, { reason, notes: notes || undefined })
      onJustified(updated)
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } }
      setError(e2.response?.data?.message ?? 'Errore durante l\'invio della giustificazione.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="jam-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="jam-box">
        <div className="jam-header">
          <h2 className="jam-title">Giustifica anomalia</h2>
          <button className="jam-close" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="jam-body">
            {error && <div className="jam-error">{error}</div>}
            <div className="jam-anomaly-info">
              <span className="jam-anomaly-type">{anomaly.typeName}</span>
              <span className="jam-anomaly-date">
                {new Date(anomaly.workDate).toLocaleDateString(undefined, {
                  weekday: 'long', day: '2-digit', month: 'long',
                })}
              </span>
            </div>
            <div className="jam-field">
              <label className="jam-label">Motivazione</label>
              <select
                className="jam-select"
                value={reason}
                onChange={e => setReason(Number(e.target.value) as TimeClockAnomalyReason)}
              >
                {REASON_OPTIONS.map(o => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </div>
            <div className="jam-field">
              <label className="jam-label">Note (facoltative)</label>
              <textarea
                className="jam-textarea"
                rows={3}
                placeholder="Aggiungi un dettaglio per il responsabile…"
                value={notes}
                onChange={e => setNotes(e.target.value)}
              />
            </div>
          </div>
          <div className="jam-footer">
            <button type="button" className="jam-btn-cancel" onClick={onClose}>Annulla</button>
            <button type="submit" className="jam-btn-primary" disabled={saving}>
              {saving ? 'Invio…' : 'Invia giustificazione'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
