import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

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
  user: any
  onLogout: () => void
}

function ClosurePeriodsPage({ user, onLogout }: ClosurePeriodsProps) {
  const [closurePeriods, setClosurePeriods] = useState<ClosurePeriod[]>([])
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

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Chiusure">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => setShowForm(!showForm)}
            className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            {showForm ? '❌ Annulla' : '➕ Nuova Chiusura'}
          </button>
        </div>

        {/* Form */}
        {showForm && (
          <div className="glass-card rounded-3xl shadow-lg p-6 mb-6 border border-white/10 animate-scale-in">
            <h2 className="text-2xl font-bold gradient-text mb-6">✨ Nuova Chiusura</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Data Inizio *</label>
                  <input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Data Fine *</label>
                  <input
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-bold text-white/90">Motivo *</label>
                <input
                  type="text"
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  placeholder="Es: Ferie estive, Natale, Ristrutturazione..."
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-bold text-white/90">Descrizione (opzionale)</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  rows={2}
                  placeholder="Dettagli aggiuntivi..."
                />
              </div>

              <button
                type="submit"
                className="w-full bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
              >
                💾 Crea Chiusura
              </button>
            </form>
          </div>
        )}

        {/* Lista Chiusure */}
        {closurePeriods.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
            <p className="text-2xl text-gray-400 mb-2">Nessuna chiusura programmata</p>
            <p className="text-gray-500">Clicca su "Nuova Chiusura" per aggiungerne una</p>
          </div>
        ) : (
          <div className="space-y-6">
            {closurePeriods
              .sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())
              .map((closure, index) => {
                const upcoming = isUpcoming(closure.startDate)
                const current = isCurrent(closure.startDate, closure.endDate)
                const past = isPast(closure.endDate)

                return (
                  <div
                    key={closure.id}
                    className={`glass-card rounded-3xl shadow-lg p-6 transition-all hover:shadow-glow-pink animate-slide-up ${
                      current ? 'border border-red-500/50' :
                      upcoming ? 'border border-neon-pink/50' :
                      'border border-white/10'
                    }`}
                    style={{ animationDelay: `${index * 0.1}s` }}
                  >
                    <div className="flex justify-between items-start">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-3">
                          <h3 className="text-2xl font-bold gradient-text">{closure.reason}</h3>
                          {current && (
                            <span className="px-3 py-1.5 bg-gradient-to-r from-red-500/20 to-neon-pink/20 text-red-400 text-xs font-bold rounded-xl border border-red-500/50">
                              🔴 IN CORSO
                            </span>
                          )}
                          {upcoming && (
                            <span className="px-3 py-1.5 bg-gradient-to-r from-neon-pink/20 to-neon-purple/20 text-neon-pink text-xs font-bold rounded-xl border border-neon-pink/50">
                              📌 PROGRAMMATA
                            </span>
                          )}
                          {past && (
                            <span className="px-3 py-1.5 bg-gray-500/20 text-gray-400 text-xs font-bold rounded-xl border border-gray-500/30">
                              ⏸️ PASSATA
                            </span>
                          )}
                        </div>

                        <div className="space-y-3">
                          <div className="glass-card-dark p-3 rounded-xl border border-neon-cyan/20">
                            <p className="text-sm text-gray-400 mb-1">📅 Periodo</p>
                            <p className="text-neon-cyan font-semibold">
                              Dal {formatDate(closure.startDate)} al {formatDate(closure.endDate)}
                              <span className="ml-2 text-neon-blue">({getDaysCount(closure.startDate, closure.endDate)} giorni)</span>
                            </p>
                          </div>
                          {closure.description && (
                            <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                              <p className="text-xs text-gray-500 mb-1">💬 Descrizione</p>
                              <p className="text-gray-300">{closure.description}</p>
                            </div>
                          )}
                        </div>
                      </div>

                      <button
                        onClick={() => deleteClosure(closure.id)}
                        className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105 ml-4"
                      >
                        🗑️ Elimina
                      </button>
                    </div>
                  </div>
                )
              })}
          </div>
        )}

        <div className="mt-8 glass-card rounded-3xl p-6 border border-neon-blue/30">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-xl bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 border border-neon-blue/30">
              <svg className="w-5 h-5 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h4 className="font-semibold text-neon-blue">Informazioni</h4>
          </div>
          <ul className="text-sm text-gray-300 space-y-2">
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Le chiusure straordinarie hanno priorità sugli orari standard</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Durante questi periodi non sarà possibile effettuare prenotazioni</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Puoi creare una lista indefinita di chiusure future</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Esempi: ferie estive, festività, ristrutturazioni, eventi speciali</span>
            </li>
          </ul>
        </div>
      </div>
    </AppLayout>
  )
}

export default ClosurePeriodsPage
