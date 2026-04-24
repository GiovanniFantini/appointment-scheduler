import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import './CreateRequestModal.css'

type RequestType = 'Ferie' | 'Permessi' | 'Malattia'

// Mapping from RequestType string to server EmployeeRequestType enum value
// (see backend/AppointmentScheduler.Shared/Enums/EmployeeRequestType.cs)
const REQUEST_TYPE_VALUES: Record<RequestType, number> = {
  Ferie: 1,
  Permessi: 3,
  Malattia: 4,
}

interface Props {
  onClose: () => void
  onCreated: () => void
}

interface ShiftOption {
  id: number
  title: string
  startDate: string
  startTime?: string
  endTime?: string
}

export default function CreateRequestModal({ onClose, onCreated }: Props) {
  const [tipo, setTipo] = useState<RequestType>('Ferie')
  const [dataInizio, setDataInizio] = useState('')
  const [dataFine, setDataFine] = useState('')
  const [tuttoIlGiorno, setTuttoIlGiorno] = useState(true)
  const [orarioDa, setOrarioDa] = useState('09:00')
  const [orarioA, setOrarioA] = useState('18:00')
  const [note, setNote] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [linkedEventId, setLinkedEventId] = useState<number | null>(null)
  const [availableShifts, setAvailableShifts] = useState<ShiftOption[]>([])

  const tipoLabels: Record<RequestType, string> = {
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
  }

  // Hourly toggle + shift linking only for Permessi
  const supportsHourly = tipo === 'Permessi'

  // Load shifts for the selected start date when the user wants to link a permesso to a specific shift.
  useEffect(() => {
    if (tipo !== 'Permessi' || !dataInizio) {
      setAvailableShifts([])
      return
    }
    const ctrl = new AbortController()
    apiClient.get<{ id: number; title: string; eventTypeName: string; startDate: string; startTime?: string; endTime?: string }[]>('/events/employee', {
      params: { from: dataInizio, to: dataInizio },
      signal: ctrl.signal,
    }).then(res => {
      const shifts = Array.isArray(res.data) ? res.data.filter(e => e.eventTypeName === 'Turno') : []
      setAvailableShifts(shifts.map(s => ({
        id: s.id,
        title: s.title,
        startDate: s.startDate,
        startTime: s.startTime,
        endTime: s.endTime,
      })))
    }).catch(() => {
      setAvailableShifts([])
    })
    return () => ctrl.abort()
  }, [tipo, dataInizio])

  // Reset irrelevant fields when type changes
  useEffect(() => {
    if (!supportsHourly) {
      setTuttoIlGiorno(true)
      setLinkedEventId(null)
    }
  }, [supportsHourly])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!dataInizio) {
      setError('Inserisci la data di inizio')
      return
    }

    const sendHourly = supportsHourly && !tuttoIlGiorno
    if (sendHourly && orarioA <= orarioDa) {
      setError('L\'orario di fine deve essere successivo a quello di inizio')
      return
    }

    setLoading(true)
    try {
      await apiClient.post('/employee-requests', {
        type: REQUEST_TYPE_VALUES[tipo],
        startDate: dataInizio,
        endDate: dataFine || undefined,
        startTime: sendHourly ? orarioDa : undefined,
        endTime: sendHourly ? orarioA : undefined,
        eventId: linkedEventId ?? undefined,
        notes: note || undefined,
      })

      onCreated()
      onClose()
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr.response?.data?.message ?? 'Errore nella creazione della richiesta')
    } finally {
      setLoading(false)
    }
  }

  const formatShiftLabel = (s: ShiftOption): string => {
    if (s.startTime && s.endTime) {
      return `${s.title} (${s.startTime.slice(0, 5)}-${s.endTime.slice(0, 5)})`
    }
    return s.title
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="create-request-modal" onClick={e => e.stopPropagation()}>
        <div className="create-request-header">
          <h2 className="create-request-title">Nuova richiesta</h2>
          <button className="modal-close-btn" onClick={onClose}>
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
          </button>
        </div>

        <form className="create-request-form" onSubmit={handleSubmit}>
          {error && <div className="form-error">{error}</div>}

          <div className="form-group">
            <label className="form-label">Tipo richiesta</label>
            <div className="tipo-selector">
              {(['Ferie', 'Permessi', 'Malattia'] as RequestType[]).map(t => (
                <button
                  key={t}
                  type="button"
                  className={`tipo-btn ${tipo === t ? 'tipo-btn--active' : ''}`}
                  onClick={() => setTipo(t)}
                >
                  {tipoLabels[t]}
                </button>
              ))}
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Data inizio *</label>
              <input
                type="date"
                className="form-input"
                value={dataInizio}
                onChange={e => setDataInizio(e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label className="form-label">Data fine</label>
              <input
                type="date"
                className="form-input"
                value={dataFine}
                min={dataInizio}
                onChange={e => setDataFine(e.target.value)}
              />
            </div>
          </div>

          {supportsHourly && (
            <div className="form-group">
              <label className="toggle-row">
                <span className="form-label">Tutto il giorno</span>
                <div className={`toggle ${tuttoIlGiorno ? 'toggle--on' : ''}`} onClick={() => setTuttoIlGiorno(v => !v)}>
                  <div className="toggle-thumb" />
                </div>
              </label>
            </div>
          )}

          {supportsHourly && !tuttoIlGiorno && (
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Dalle</label>
                <input
                  type="time"
                  className="form-input"
                  value={orarioDa}
                  onChange={e => setOrarioDa(e.target.value)}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Alle</label>
                <input
                  type="time"
                  className="form-input"
                  value={orarioA}
                  onChange={e => setOrarioA(e.target.value)}
                />
              </div>
            </div>
          )}

          {supportsHourly && availableShifts.length > 0 && (
            <div className="form-group">
              <label className="form-label">Collega a un turno (opzionale)</label>
              <select
                className="form-input"
                value={linkedEventId ?? ''}
                onChange={e => setLinkedEventId(e.target.value ? Number(e.target.value) : null)}
              >
                <option value="">Nessuno (tutti i turni del giorno)</option>
                {availableShifts.map(s => (
                  <option key={s.id} value={s.id}>{formatShiftLabel(s)}</option>
                ))}
              </select>
            </div>
          )}

          <div className="form-group">
            <label className="form-label">Note</label>
            <textarea
              className="form-input form-textarea"
              placeholder="Eventuali note..."
              value={note}
              onChange={e => setNote(e.target.value)}
              rows={3}
            />
          </div>

          <div className="create-request-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>Annulla</button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? <span className="btn-spinner" /> : 'Invia richiesta'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
