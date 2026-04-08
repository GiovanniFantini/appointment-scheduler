import { useState, FormEvent } from 'react'
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

  const tipoLabels: Record<RequestType, string> = {
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!dataInizio) {
      setError('Inserisci la data di inizio')
      return
    }

    setLoading(true)
    try {
      await apiClient.post('/employee-requests', {
        type: REQUEST_TYPE_VALUES[tipo],
        startDate: dataInizio,
        endDate: dataFine || undefined,
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

          <div className="form-group">
            <label className="toggle-row">
              <span className="form-label">Tutto il giorno</span>
              <div className={`toggle ${tuttoIlGiorno ? 'toggle--on' : ''}`} onClick={() => setTuttoIlGiorno(v => !v)}>
                <div className="toggle-thumb" />
              </div>
            </label>
          </div>

          {!tuttoIlGiorno && (
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
