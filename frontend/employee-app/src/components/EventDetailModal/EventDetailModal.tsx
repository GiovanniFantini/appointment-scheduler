import './EventDetailModal.css'

export interface EventDetail {
  id: number | string
  title: string
  start: string
  end?: string
  eventType?: string
  allDay?: boolean
  isOnCall?: boolean
  participants?: Array<{ id: number; name: string }>
  notes?: string
  extendedProps?: Record<string, unknown>
}

interface Props {
  event: EventDetail
  onClose: () => void
}

function getEventTypeLabel(type?: string): string {
  const map: Record<string, string> = {
    Turno: 'Turno',
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
    ChiusuraAziendale: 'Chiusura aziendale',
  }
  return type ? (map[type] ?? type) : 'Evento'
}

function getEventTypeColor(type?: string): string {
  const map: Record<string, string> = {
    Turno: '#3b82f6',
    Ferie: '#ec4899',
    Permessi: '#8b5cf6',
    Malattia: '#f59e0b',
    ChiusuraAziendale: '#475569',
  }
  return type ? (map[type] ?? '#6366f1') : '#6366f1'
}

function formatDateTime(dateStr: string, allDay?: boolean): string {
  const d = new Date(dateStr)
  if (allDay) {
    return d.toLocaleDateString('it-IT', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
  }
  return d.toLocaleString('it-IT', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

export default function EventDetailModal({ event, onClose }: Props) {
  const color = getEventTypeColor(event.eventType)
  const isOnCall = event.isOnCall ?? (event.extendedProps?.isOnCall as boolean | undefined)
  const participants = event.participants ?? (event.extendedProps?.participants as EventDetail['participants'] | undefined)
  const notes = event.notes ?? (event.extendedProps?.notes as string | undefined)

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="event-detail-modal" onClick={e => e.stopPropagation()}>
        <div className="event-detail-header" style={{ borderLeftColor: color }}>
          <div className="event-detail-title-row">
            <h2 className="event-detail-title">{event.title}</h2>
            <button className="modal-close-btn" onClick={onClose}>
              <svg viewBox="0 0 24 24" fill="none">
                <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </button>
          </div>
          <div className="event-detail-badges">
            <span
              className="event-type-badge"
              style={{ backgroundColor: color + '22', color }}
            >
              {getEventTypeLabel(event.eventType)}
            </span>
            {isOnCall && (
              <span className="oncall-badge">Reperibile</span>
            )}
          </div>
        </div>

        <div className="event-detail-body">
          <div className="event-detail-row">
            <span className="event-detail-label">Inizio</span>
            <span className="event-detail-value">{formatDateTime(event.start, event.allDay)}</span>
          </div>

          {event.end && (
            <div className="event-detail-row">
              <span className="event-detail-label">Fine</span>
              <span className="event-detail-value">{formatDateTime(event.end, event.allDay)}</span>
            </div>
          )}

          {event.allDay && (
            <div className="event-detail-row">
              <span className="event-detail-label">Durata</span>
              <span className="event-detail-value">Tutto il giorno</span>
            </div>
          )}

          {participants && participants.length > 0 && (
            <div className="event-detail-row event-detail-row--column">
              <span className="event-detail-label">Partecipanti</span>
              <div className="participants-list">
                {participants.map(p => (
                  <span key={p.id} className="participant-chip">{p.name}</span>
                ))}
              </div>
            </div>
          )}

          {notes && (
            <div className="event-detail-row event-detail-row--column">
              <span className="event-detail-label">Note</span>
              <p className="event-detail-notes">{notes}</p>
            </div>
          )}
        </div>

        <div className="event-detail-footer">
          <button className="btn-secondary" onClick={onClose}>Chiudi</button>
        </div>
      </div>
    </div>
  )
}
