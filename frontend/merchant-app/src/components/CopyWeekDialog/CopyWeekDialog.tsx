import { useEffect, useState } from 'react'
import apiClient from '../../lib/axios'
import './CopyWeekDialog.css'

interface CopyWeekDialogProps {
  /** Lunedì della settimana attualmente in vista (target di default). */
  currentWeekStart: string
  onClose: () => void
  onCopied: () => void
}

function addDays(iso: string, days: number): string {
  const d = new Date(iso + 'T00:00:00')
  d.setDate(d.getDate() + days)
  return d.toISOString().split('T')[0]
}

function formatRange(weekStart: string): string {
  const start = new Date(weekStart + 'T00:00:00')
  const end = new Date(start)
  end.setDate(end.getDate() + 6)
  const fmt = (d: Date) => d.toLocaleDateString('it-IT', { day: '2-digit', month: 'short' })
  return `${fmt(start)} – ${fmt(end)}`
}

interface PreviewEvent {
  id: number
  title: string
  startDate: string
  startTime?: string
  endTime?: string
}

export default function CopyWeekDialog({ currentWeekStart, onClose, onCopied }: CopyWeekDialogProps) {
  const sourceWeekStart = addDays(currentWeekStart, -7)
  const targetWeekStart = currentWeekStart
  const [preview, setPreview] = useState<PreviewEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    const from = sourceWeekStart
    const to = addDays(sourceWeekStart, 6)
    apiClient.get(`/events?from=${from}&to=${to}`)
      .then(res => {
        const items = Array.isArray(res.data) ? (res.data as PreviewEvent[]) : []
        setPreview(items)
      })
      .catch(() => setPreview([]))
      .finally(() => setLoading(false))
  }, [sourceWeekStart])

  const handleConfirm = async () => {
    setSubmitting(true)
    setError('')
    try {
      await apiClient.post('/events/clone-week', {
        sourceWeekStart,
        targetWeekStart,
        numberOfWeeks: 1,
      })
      onCopied()
    } catch {
      setError('Errore durante la copia della settimana')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="copy-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="copy-dialog">
        <div className="copy-header">
          <h2>Copia settimana scorsa</h2>
          <button className="copy-close" onClick={onClose}>✕</button>
        </div>
        <div className="copy-body">
          <div className="copy-range">
            <div className="copy-arrow">
              <div className="copy-week">
                <div className="copy-week-label">Da</div>
                <div className="copy-week-range">{formatRange(sourceWeekStart)}</div>
              </div>
              <span className="copy-arrow-symbol">→</span>
              <div className="copy-week">
                <div className="copy-week-label">A</div>
                <div className="copy-week-range">{formatRange(targetWeekStart)}</div>
              </div>
            </div>
          </div>

          {error && <div className="copy-error">{error}</div>}

          <h3 className="copy-section-title">Turni che verranno copiati ({preview.length})</h3>
          {loading ? (
            <div className="copy-empty">Caricamento...</div>
          ) : preview.length === 0 ? (
            <div className="copy-empty">
              Nessun turno nella settimana scorsa da copiare.
            </div>
          ) : (
            <ul className="copy-list">
              {preview.map(e => (
                <li key={e.id} className="copy-item">
                  <span className="copy-item-title">{e.title}</span>
                  <span className="copy-item-meta">
                    {new Date(e.startDate + 'T00:00:00').toLocaleDateString('it-IT', { weekday: 'short', day: '2-digit', month: 'short' })}
                    {e.startTime && e.endTime && (
                      <> · {e.startTime.slice(0, 5)}-{e.endTime.slice(0, 5)}</>
                    )}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <div className="copy-footer">
          <button className="btn-cancel" onClick={onClose}>Annulla</button>
          <button
            className="btn-primary"
            onClick={handleConfirm}
            disabled={submitting || preview.length === 0}
          >
            {submitting ? 'Copia in corso...' : `Copia ${preview.length} turni`}
          </button>
        </div>
      </div>
    </div>
  )
}
