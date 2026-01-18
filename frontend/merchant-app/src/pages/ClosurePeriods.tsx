import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

interface ClosurePeriod {
  id: number
  merchantId: number
  startDate: string
  endDate: string
  reason: string
  description: string | null
  isActive: boolean
  createdAt: string
}

interface ClosurePeriodsProps {
  onLogout: () => void
}

function ClosurePeriodsPage({ onLogout }: ClosurePeriodsProps) {
  const [closurePeriods, setClosurePeriods] = useState<ClosurePeriod[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [formData, setFormData] = useState({
    startDate: '',
    endDate: '',
    reason: '',
    description: ''
  })

  useEffect(() => {
    fetchClosurePeriods()
  }, [])

  const fetchClosurePeriods = async () => {
    try {
      const res = await apiClient.get('/closureperiod/my-closures')
      setClosurePeriods(res.data)
    } catch (err: any) {
      console.error('Errore nel caricamento delle chiusure:', err)
      if (err.response?.status !== 404) {
        alert('Errore nel caricamento delle chiusure')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!formData.startDate || !formData.endDate || !formData.reason) {
      alert('Compila tutti i campi obbligatori')
      return
    }

    try {
      await apiClient.post('/closureperiod', {
        startDate: formData.startDate,
        endDate: formData.endDate,
        reason: formData.reason,
        description: formData.description || null
      })

      alert('Chiusura creata con successo!')
      setShowForm(false)
      setFormData({ startDate: '', endDate: '', reason: '', description: '' })
      fetchClosurePeriods()
    } catch (err: any) {
      console.error('Errore nella creazione:', err)
      const errorMessage = err.response?.data?.message || err.response?.data || 'Errore nella creazione'
      alert(`Errore: ${errorMessage}`)
    }
  }

  const deleteClosure = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questa chiusura?')) return

    try {
      await apiClient.delete(`/closureperiod/${id}`)
      alert('Chiusura eliminata!')
      fetchClosurePeriods()
    } catch (err: any) {
      console.error('Errore nell\'eliminazione:', err)
      alert('Errore nell\'eliminazione')
    }
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString('it-IT', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'UTC'
    })
  }

  const getDaysCount = (start: string, end: string) => {
    const startDate = new Date(start)
    const endDate = new Date(end)
    const diffTime = Math.abs(endDate.getTime() - startDate.getTime())
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1
    return diffDays
  }

  const isUpcoming = (startDate: string) => {
    return new Date(startDate) > new Date()
  }

  const isCurrent = (startDate: string, endDate: string) => {
    const now = new Date()
    return new Date(startDate) <= now && new Date(endDate) >= now
  }

  const isPast = (endDate: string) => {
    return new Date(endDate) < new Date()
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-dark">
        <nav className="bg-gradient-to-r from-neon-cyan to-neon-blue text-white p-4">
          <div className="container mx-auto flex justify-between items-center">
            <h1 className="text-2xl font-bold">Merchant Dashboard</h1>
          </div>
        </nav>
        <div className="container mx-auto p-4">
          <div className="text-center py-8">Caricamento...</div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-dark">
      <nav className="bg-gradient-to-r from-neon-cyan to-neon-blue text-white p-4">
        <div className="container mx-auto flex justify-between items-center">
          <h1 className="text-2xl font-bold">Merchant Dashboard</h1>
          <div className="space-x-4">
            <Link to="/dashboard" className="hover:underline">Dashboard</Link>
            <Link to="/services" className="hover:underline">Servizi</Link>
            <Link to="/business-hours" className="hover:underline">Orari</Link>
            <Link to="/closures" className="hover:underline font-semibold">Chiusure</Link>
            <Link to="/availabilities" className="hover:underline">Disponibilità</Link>
            <Link to="/employees" className="hover:underline">Dipendenti</Link>
            <Link to="/bookings" className="hover:underline">Prenotazioni</Link>
            <button onClick={onLogout} className="hover:underline">Logout</button>
          </div>
        </div>
      </nav>

      <div className="container mx-auto p-4">
        <div className="glass-card rounded-xl shadow-md p-6">
          <div className="flex justify-between items-center mb-6">
            <div>
              <h2 className="text-2xl font-bold">🚫 Chiusure Straordinarie</h2>
              <p className="text-gray-600">Gestisci ferie, festività e altre chiusure</p>
            </div>
            <button
              onClick={() => setShowForm(!showForm)}
              className="px-4 py-2 bg-gradient-to-r from-neon-cyan to-neon-blue text-white rounded hover:bg-indigo-700"
            >
              {showForm ? 'Annulla' : '+ Nuova Chiusura'}
            </button>
          </div>

          {/* Form */}
          {showForm && (
            <form onSubmit={handleSubmit} className="mb-6 p-4 bg-gray-50 rounded border">
              <div className="grid grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Data Inizio *</label>
                  <input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full px-3 py-2 border rounded"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Data Fine *</label>
                  <input
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full px-3 py-2 border rounded"
                    required
                  />
                </div>
              </div>

              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Motivo *</label>
                <input
                  type="text"
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  className="w-full px-3 py-2 border rounded"
                  placeholder="Es: Ferie estive, Natale, Ristrutturazione..."
                  required
                />
              </div>

              <div className="mb-4">
                <label className="block text-sm font-medium mb-1">Descrizione (opzionale)</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-3 py-2 border rounded"
                  rows={2}
                  placeholder="Dettagli aggiuntivi..."
                />
              </div>

              <button
                type="submit"
                className="w-full px-4 py-2 bg-gradient-to-r from-neon-cyan to-neon-blue text-white rounded hover:bg-indigo-700"
              >
                Crea Chiusura
              </button>
            </form>
          )}

          {/* Lista Chiusure */}
          {closurePeriods.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p>Nessuna chiusura programmata</p>
              <p className="text-sm mt-2">Clicca su "Nuova Chiusura" per aggiungerne una</p>
            </div>
          ) : (
            <div className="space-y-4">
              {closurePeriods
                .sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())
                .map((closure) => {
                  const upcoming = isUpcoming(closure.startDate)
                  const current = isCurrent(closure.startDate, closure.endDate)
                  const past = isPast(closure.endDate)

                  return (
                    <div
                      key={closure.id}
                      className={`border rounded-xl p-4 ${
                        current ? 'border-red-500 bg-red-50' :
                        upcoming ? 'border-orange-300 bg-orange-50' :
                        'border-gray-300 bg-gray-50'
                      }`}
                    >
                      <div className="flex justify-between items-start">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <h3 className="font-semibold text-lg">{closure.reason}</h3>
                            {current && (
                              <span className="px-2 py-1 bg-gradient-to-r from-red-500 to-pink-500 text-white text-xs rounded">
                                IN CORSO
                              </span>
                            )}
                            {upcoming && (
                              <span className="px-2 py-1 bg-gradient-to-r from-orange-500 to-red-500 text-white text-xs rounded">
                                PROGRAMMATA
                              </span>
                            )}
                            {past && (
                              <span className="px-2 py-1 bg-gray-400 text-white text-xs rounded">
                                PASSATA
                              </span>
                            )}
                          </div>

                          <div className="text-sm text-gray-600 space-y-1">
                            <p>
                              📅 Dal <strong>{formatDate(closure.startDate)}</strong> al{' '}
                              <strong>{formatDate(closure.endDate)}</strong>
                              {' '}({getDaysCount(closure.startDate, closure.endDate)} giorni)
                            </p>
                            {closure.description && (
                              <p className="text-gray-700">💬 {closure.description}</p>
                            )}
                          </div>
                        </div>

                        <button
                          onClick={() => deleteClosure(closure.id)}
                          className="px-3 py-1 bg-gradient-to-r from-red-500 to-pink-500 text-white rounded hover:bg-red-600 text-sm"
                        >
                          Elimina
                        </button>
                      </div>
                    </div>
                  )
                })}
            </div>
          )}

          <div className="mt-6 p-4 bg-blue-50 rounded">
            <h4 className="font-semibold mb-2">ℹ️ Informazioni:</h4>
            <ul className="text-sm text-gray-700 list-disc list-inside space-y-1">
              <li>Le chiusure straordinarie hanno priorità sugli orari standard</li>
              <li>Durante questi periodi non sarà possibile effettuare prenotazioni</li>
              <li>Puoi creare una lista indefinita di chiusure future</li>
              <li>Esempi: ferie estive, festività, ristrutturazioni, eventi speciali</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ClosurePeriodsPage
