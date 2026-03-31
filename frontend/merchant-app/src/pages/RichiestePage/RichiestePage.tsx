import { useState } from 'react'
import './RichiestePage.css'

interface Richiesta {
  id: number
  employeeName: string
  initials: string
  type: 'ferie' | 'permessi' | 'malattia'
  dateRange: string
  days: number
  status: 'pending' | 'approved' | 'rejected'
}

const MOCK_RICHIESTE: Richiesta[] = [
  { id: 1, employeeName: 'Marco Bianchi', initials: 'MB', type: 'ferie', dateRange: '15 Apr - 22 Apr', days: 7, status: 'pending' },
  { id: 2, employeeName: 'Sara Verdi', initials: 'SV', type: 'permessi', dateRange: '3 Apr 14:00 - 18:00', days: 0, status: 'pending' },
  { id: 3, employeeName: 'Luigi Russo', initials: 'LR', type: 'malattia', dateRange: '1 Apr - 2 Apr', days: 2, status: 'pending' },
]

const TYPE_LABELS: Record<string, string> = {
  ferie: 'Ferie',
  permessi: 'Permessi',
  malattia: 'Malattia',
}

export default function RichiestePage() {
  const [richieste, setRichieste] = useState<Richiesta[]>(MOCK_RICHIESTE)

  const handleApprove = (id: number) => {
    setRichieste(prev => prev.map(r => r.id === id ? { ...r, status: 'approved' as const } : r))
  }

  const handleReject = (id: number) => {
    setRichieste(prev => prev.map(r => r.id === id ? { ...r, status: 'rejected' as const } : r))
  }

  const pending = richieste.filter(r => r.status === 'pending')

  return (
    <div className="richieste-page">
      <div className="page-header">
        <h1 className="page-title">Richieste</h1>
        <p className="page-subtitle">Gestisci le richieste dei dipendenti</p>
      </div>

      <div className="dev-notice">
        ⚠️ Gestione richieste in sviluppo — i dati mostrati sono di esempio
      </div>

      <div className="richieste-card">
        <div className="card-header">
          <h2 className="card-title">Richieste in attesa</h2>
          <span className="badge">{pending.length}</span>
        </div>

        {pending.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">✅</div>
            <div className="empty-text">Nessuna richiesta in attesa</div>
          </div>
        ) : (
          pending.map(r => (
            <div key={r.id} className="richiesta-item">
              <div className="richiesta-avatar">{r.initials}</div>
              <div className="richiesta-info">
                <div className="richiesta-name">{r.employeeName}</div>
                <div className="richiesta-detail">
                  {r.dateRange}{r.days > 0 ? ` • ${r.days} giorni` : ''}
                </div>
              </div>
              <span className={`richiesta-type ${r.type}`}>{TYPE_LABELS[r.type]}</span>
              <div className="richiesta-actions">
                <button className="btn-approve" onClick={() => handleApprove(r.id)}>Approva</button>
                <button className="btn-reject" onClick={() => handleReject(r.id)}>Rifiuta</button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
