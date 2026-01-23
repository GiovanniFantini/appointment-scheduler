import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface Service {
  id: number
  name: string
  bookingMode: number
  bookingModeName: string
}

interface Availability {
  id: number
  serviceId: number
  dayOfWeek: number | null
  dayOfWeekName: string
  specificDate: string | null
  startTime: string
  endTime: string
  isRecurring: boolean
  maxCapacity: number | null
  isActive: boolean
  slots: AvailabilitySlot[]
}

interface AvailabilitySlot {
  id: number
  slotTime: string
  maxCapacity: number | null
}

interface AvailabilitiesProps {
  user: any
  onLogout: () => void
}

const DAYS_OF_WEEK = [
  { value: 0, label: 'Domenica' },
  { value: 1, label: 'Lunedì' },
  { value: 2, label: 'Martedì' },
  { value: 3, label: 'Mercoledì' },
  { value: 4, label: 'Giovedì' },
  { value: 5, label: 'Venerdì' },
  { value: 6, label: 'Sabato' }
]

function Availabilities({ user, onLogout }: AvailabilitiesProps) {
  const [services, setServices] = useState<Service[]>([])
  const [availabilities, setAvailabilities] = useState<Availability[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [showSlotForm, setShowSlotForm] = useState(false)
  const [selectedAvailability, setSelectedAvailability] = useState<Availability | null>(null)
  const [formData, setFormData] = useState({
    serviceId: 0,
    isRecurring: true,
    dayOfWeek: 1,
    specificDate: '',
    startTime: '09:00',
    endTime: '18:00',
    maxCapacity: ''
  })
  const [slotFormData, setSlotFormData] = useState({
    slotTime: '09:00',
    maxCapacity: ''
  })

  useEffect(() => {
    fetchData()
  }, [])

  const fetchData = async () => {
    try {
      const [servicesRes, availabilitiesRes] = await Promise.all([
        apiClient.get('/services/my-services'),
        apiClient.get('/availability/my-availabilities')
      ])
      setServices(servicesRes.data)
      setAvailabilities(availabilitiesRes.data)
    } catch (err: any) {
      console.error('Errore nel caricamento dei dati:', err)

      // Gestione errori più dettagliata
      if (err.response?.status === 401) {
        const errorMessage = err.response?.data?.message || err.response?.data || 'Non autorizzato'
        alert(`Errore di autenticazione: ${errorMessage}\n\nAssicurati di essere loggato come merchant.`)
      } else if (err.response?.status === 400) {
        const errorMessage = err.response?.data?.message || err.response?.data || 'Richiesta non valida'
        alert(`Errore: ${errorMessage}`)
      } else if (err.response) {
        const errorMessage = err.response?.data?.message || err.response?.data || 'Errore sconosciuto'
        alert(`Errore nel caricamento dei dati: ${errorMessage}`)
      } else if (err.request) {
        alert('Errore di rete: impossibile contattare il server. Verifica la tua connessione.')
      } else {
        alert(`Errore: ${err.message || 'Errore sconosciuto'}`)
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const requestData = {
      serviceId: formData.serviceId,
      isRecurring: formData.isRecurring,
      dayOfWeek: formData.isRecurring ? formData.dayOfWeek : null,
      specificDate: !formData.isRecurring ? formData.specificDate : null,
      startTime: formData.startTime,
      endTime: formData.endTime,
      maxCapacity: formData.maxCapacity ? parseInt(formData.maxCapacity) : null
    }

    try {
      await apiClient.post('/availability', requestData)
      fetchData()
      setShowForm(false)
      resetForm()
      alert('Disponibilità creata con successo!')
    } catch (err: any) {
      alert(err.response?.data || 'Errore nella creazione della disponibilità')
    }
  }

  const handleAddSlot = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedAvailability) return

    const slots = [{
      slotTime: slotFormData.slotTime,
      maxCapacity: slotFormData.maxCapacity ? parseInt(slotFormData.maxCapacity) : null
    }]

    try {
      await apiClient.post(`/availability/${selectedAvailability.id}/slots`, slots)
      fetchData()
      setShowSlotForm(false)
      setSelectedAvailability(null)
      setSlotFormData({ slotTime: '09:00', maxCapacity: '' })
      alert('Slot aggiunto con successo!')
    } catch (err: any) {
      alert(err.response?.data || 'Errore nell\'aggiunta dello slot')
    }
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questa disponibilità?')) return

    try {
      await apiClient.delete(`/availability/${id}`)
      fetchData()
      alert('Disponibilità eliminata!')
    } catch (err: any) {
      alert(err.response?.data || 'Errore nell\'eliminazione')
    }
  }

  const resetForm = () => {
    setFormData({
      serviceId: 0,
      isRecurring: true,
      dayOfWeek: 1,
      specificDate: '',
      startTime: '09:00',
      endTime: '18:00',
      maxCapacity: ''
    })
  }

  const getServiceName = (serviceId: number) => {
    return services.find(s => s.id === serviceId)?.name || 'Sconosciuto'
  }

  const getServiceBookingMode = (serviceId: number) => {
    return services.find(s => s.id === serviceId)?.bookingMode || 1
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Disponibilità">
      <div className="container mx-auto px-4 py-8">
        {loading ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
            <div className="flex items-center justify-center gap-3 text-neon-cyan">
              <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span className="text-xl font-semibold">Caricamento...</span>
            </div>
          </div>
        ) : (
          <>
        <div className="mb-6 flex justify-between items-center">
          <h2 className="text-3xl font-bold gradient-text">Le tue disponibilità</h2>
          <button
            onClick={() => setShowForm(true)}
            className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            ➕ Nuova Disponibilità
          </button>
        </div>

        {/* Form creazione availability */}
        {showForm && (
          <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10 shadow-glow-blue animate-scale-in">
            <h3 className="text-2xl font-bold gradient-text mb-6">
              ✨ Crea Nuova Disponibilità
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <label className="block text-sm font-semibold text-white/90">
                  Servizio *
                </label>
                <select
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  value={formData.serviceId}
                  onChange={(e) => setFormData({ ...formData, serviceId: parseInt(e.target.value) })}
                  required
                >
                  <option value={0}>Seleziona un servizio</option>
                  {services.map(s => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.bookingModeName})
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-semibold text-white/90">
                  Tipo Disponibilità
                </label>
                <div className="flex gap-4">
                  <label className="glass-card-dark p-3 rounded-xl cursor-pointer hover:border-neon-blue/50 transition-all border border-white/10 flex items-center">
                    <input
                      type="radio"
                      checked={formData.isRecurring}
                      onChange={() => setFormData({ ...formData, isRecurring: true })}
                      className="mr-2"
                    />
                    <span className="text-gray-300">🔄 Ricorrente (settimanale)</span>
                  </label>
                  <label className="glass-card-dark p-3 rounded-xl cursor-pointer hover:border-neon-blue/50 transition-all border border-white/10 flex items-center">
                    <input
                      type="radio"
                      checked={!formData.isRecurring}
                      onChange={() => setFormData({ ...formData, isRecurring: false })}
                      className="mr-2"
                    />
                    <span className="text-gray-300">📅 Data specifica</span>
                  </label>
                </div>
              </div>

              {formData.isRecurring ? (
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-white/90">
                    Giorno della settimana *
                  </label>
                  <select
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    value={formData.dayOfWeek}
                    onChange={(e) => setFormData({ ...formData, dayOfWeek: parseInt(e.target.value) })}
                    required
                  >
                    {DAYS_OF_WEEK.map(day => (
                      <option key={day.value} value={day.value}>{day.label}</option>
                    ))}
                  </select>
                </div>
              ) : (
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-white/90">
                    Data *
                  </label>
                  <input
                    type="date"
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    value={formData.specificDate}
                    onChange={(e) => setFormData({ ...formData, specificDate: e.target.value })}
                    required
                  />
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-white/90">
                    Orario inizio *
                  </label>
                  <input
                    type="time"
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    value={formData.startTime}
                    onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-white/90">
                    Orario fine *
                  </label>
                  <input
                    type="time"
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    value={formData.endTime}
                    onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-semibold text-white/90">
                  Capacità massima (opzionale)
                </label>
                <input
                  type="number"
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  value={formData.maxCapacity}
                  onChange={(e) => setFormData({ ...formData, maxCapacity: e.target.value })}
                  placeholder="Lascia vuoto per illimitato"
                  min="1"
                />
              </div>

              <div className="flex justify-end gap-3 mt-6">
                <button
                  type="button"
                  onClick={() => { setShowForm(false); resetForm() }}
                  className="glass-card-dark px-6 py-3 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10"
                >
                  ❌ Annulla
                </button>
                <button
                  type="submit"
                  className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                >
                  💾 Crea Disponibilità
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Form aggiunta slot */}
        {showSlotForm && selectedAvailability && (
          <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10 shadow-glow-purple animate-scale-in">
            <h3 className="text-2xl font-bold gradient-text mb-4">
              ⏰ Aggiungi Slot Orario
            </h3>
            <p className="text-gray-400 mb-6">
              <span className="text-neon-cyan font-semibold">Servizio:</span> {getServiceName(selectedAvailability.serviceId)} -
              {selectedAvailability.isRecurring
                ? ` ${selectedAvailability.dayOfWeekName}`
                : ` ${new Date(selectedAvailability.specificDate!).toLocaleDateString('it-IT', { timeZone: 'UTC' })}`}
              {' '}({selectedAvailability.startTime} - {selectedAvailability.endTime})
            </p>
            <form onSubmit={handleAddSlot} className="space-y-4">
              <div className="space-y-2">
                <label className="block text-sm font-semibold text-white/90">
                  Orario Slot *
                </label>
                <input
                  type="time"
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  value={slotFormData.slotTime}
                  onChange={(e) => setSlotFormData({ ...slotFormData, slotTime: e.target.value })}
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-semibold text-white/90">
                  Capacità massima slot (opzionale)
                </label>
                <input
                  type="number"
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  value={slotFormData.maxCapacity}
                  onChange={(e) => setSlotFormData({ ...slotFormData, maxCapacity: e.target.value })}
                  placeholder="Usa capacità availability"
                  min="1"
                />
              </div>

              <div className="flex justify-end gap-3 mt-6">
                <button
                  type="button"
                  onClick={() => {
                    setShowSlotForm(false)
                    setSelectedAvailability(null)
                    setSlotFormData({ slotTime: '09:00', maxCapacity: '' })
                  }}
                  className="glass-card-dark px-6 py-3 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10"
                >
                  ❌ Annulla
                </button>
                <button
                  type="submit"
                  className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                >
                  ➕ Aggiungi Slot
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Lista availabilities */}
        <div className="space-y-6">
          {availabilities.length === 0 ? (
            <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
              <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-2xl text-gray-400">Nessuna disponibilità configurata. Creane una per iniziare!</p>
            </div>
          ) : (
            availabilities.map((avail, index) => (
              <div key={avail.id} className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-cyan/50 transition-all hover:shadow-glow-cyan animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-2xl font-bold gradient-text mb-2">
                      {getServiceName(avail.serviceId)}
                    </h3>
                    <div className="space-y-2 text-gray-400">
                      <div className="flex items-center gap-2">
                        <span className="glass-card-dark px-3 py-1 rounded-lg border border-neon-purple/30">
                          <span className="text-neon-purple">
                            {avail.isRecurring
                              ? `🔄 Ogni ${avail.dayOfWeekName}`
                              : `📅 ${new Date(avail.specificDate!).toLocaleDateString('it-IT', { timeZone: 'UTC' })}`}
                          </span>
                        </span>
                        <span className="glass-card-dark px-3 py-1 rounded-lg border border-neon-blue/30">
                          <span className="text-neon-blue">
                            🕐 {avail.startTime} - {avail.endTime}
                          </span>
                        </span>
                      </div>
                      {avail.maxCapacity && (
                        <div className="glass-card-dark inline-block px-3 py-1 rounded-lg border border-neon-green/30">
                          <span className="text-neon-green">👥 Capacità massima: {avail.maxCapacity}</span>
                        </div>
                      )}
                      <div>
                        <span className={`px-3 py-1 rounded-lg text-xs font-semibold border ${avail.isActive ? 'bg-neon-green/20 text-neon-green border-neon-green/30' : 'bg-gray-500/20 text-gray-400 border-gray-500/30'}`}>
                          {avail.isActive ? '✅ Attivo' : '⏸️ Disattivo'}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-3">
                    {getServiceBookingMode(avail.serviceId) === 1 && (
                      <button
                        onClick={() => {
                          setSelectedAvailability(avail)
                          setShowSlotForm(true)
                        }}
                        className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10"
                      >
                        ➕ Slot
                      </button>
                    )}
                    <button
                      onClick={() => handleDelete(avail.id)}
                      className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all"
                    >
                      🗑️ Elimina
                    </button>
                  </div>
                </div>

                {/* Mostra slot se presenti */}
                {avail.slots.length > 0 && (
                  <div className="mt-6 pt-6 border-t border-white/10">
                    <p className="text-sm font-semibold text-neon-cyan mb-3">⏰ Slot Orari:</p>
                    <div className="flex flex-wrap gap-3">
                      {avail.slots.map(slot => (
                        <span key={slot.id} className="glass-card-dark px-4 py-2 rounded-xl border border-neon-blue/30">
                          <span className="text-neon-blue font-semibold">
                            {slot.slotTime}
                            {slot.maxCapacity && ` (max ${slot.maxCapacity})`}
                          </span>
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
        )}
      </div>
    </AppLayout>
  )
}

export default Availabilities
