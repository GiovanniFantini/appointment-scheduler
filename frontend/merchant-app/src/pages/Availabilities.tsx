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
  const [editingId, setEditingId] = useState<number | null>(null)
  const [formData, setFormData] = useState({
    serviceId: '',
    isRecurring: true,
    dayOfWeek: '',
    specificDate: '',
    startTime: '09:00',
    endTime: '18:00',
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
      if (err.response?.status === 401) {
        alert('Errore di autenticazione. Assicurati di essere loggato come merchant.')
      } else {
        alert(err.response?.data?.message || 'Errore nel caricamento dei dati')
      }
    } finally {
      setLoading(false)
    }
  }

  const getServiceName = (serviceId: number) => {
    return services.find(s => s.id === serviceId)?.name || 'Sconosciuto'
  }

  const getDayName = (dayOfWeek: number | null) => {
    if (dayOfWeek === null) return '-'
    return DAYS_OF_WEEK.find(d => d.value === dayOfWeek)?.label || '-'
  }

  const formatTime = (time: string) => {
    if (!time) return '-'
    return time.substring(0, 5)
  }

  const formatDate = (date: string | null) => {
    if (!date) return '-'
    return new Date(date).toLocaleDateString('it-IT')
  }

  const resetForm = () => {
    setFormData({
      serviceId: '',
      isRecurring: true,
      dayOfWeek: '',
      specificDate: '',
      startTime: '09:00',
      endTime: '18:00',
      maxCapacity: ''
    })
    setEditingId(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const payload = {
      serviceId: parseInt(formData.serviceId),
      isRecurring: formData.isRecurring,
      dayOfWeek: formData.isRecurring && formData.dayOfWeek ? parseInt(formData.dayOfWeek) : null,
      specificDate: !formData.isRecurring && formData.specificDate ? formData.specificDate : null,
      startTime: formData.startTime + ':00',
      endTime: formData.endTime + ':00',
      maxCapacity: formData.maxCapacity ? parseInt(formData.maxCapacity) : null
    }

    try {
      if (editingId) {
        await apiClient.put(`/availability/${editingId}`, {
          startTime: payload.startTime,
          endTime: payload.endTime,
          maxCapacity: payload.maxCapacity,
          isActive: true
        })
      } else {
        await apiClient.post('/availability', payload)
      }
      await fetchData()
      setShowForm(false)
      resetForm()
    } catch (err: any) {
      alert(err.response?.data?.message || err.response?.data || 'Errore nel salvataggio')
    }
  }

  const handleEdit = (avail: Availability) => {
    setFormData({
      serviceId: avail.serviceId.toString(),
      isRecurring: avail.isRecurring,
      dayOfWeek: avail.dayOfWeek?.toString() || '',
      specificDate: avail.specificDate ? avail.specificDate.split('T')[0] : '',
      startTime: formatTime(avail.startTime),
      endTime: formatTime(avail.endTime),
      maxCapacity: avail.maxCapacity?.toString() || ''
    })
    setEditingId(avail.id)
    setShowForm(true)
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questa disponibilità?')) return

    try {
      await apiClient.delete(`/availability/${id}`)
      await fetchData()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'eliminazione')
    }
  }

  const handleToggleActive = async (avail: Availability) => {
    try {
      await apiClient.put(`/availability/${avail.id}`, {
        startTime: avail.startTime,
        endTime: avail.endTime,
        maxCapacity: avail.maxCapacity,
        isActive: !avail.isActive
      })
      await fetchData()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'aggiornamento')
    }
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Disponibilità">
      <div className="container mx-auto px-4 py-8">
        {loading ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
            <div className="flex items-center justify-center gap-3 text-neon-cyan">
              <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <span className="text-xl font-semibold">Caricamento...</span>
            </div>
          </div>
        ) : (
          <>
            {/* Info Panel */}
            <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
              <div className="flex items-start gap-4">
                <div className="bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 p-3 rounded-xl border border-neon-blue/30">
                  <svg className="w-6 h-6 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-lg font-bold text-neon-cyan mb-1">Override Disponibilità</h3>
                  <p className="text-gray-400 text-sm">
                    Queste disponibilità sovrascrivono gli orari standard di apertura per servizi specifici.
                    Usa questa sezione per configurare eccezioni o slot personalizzati.
                  </p>
                </div>
              </div>
            </div>

            {/* Header */}
            <div className="mb-6 flex justify-between items-center">
              <h2 className="text-3xl font-bold gradient-text">Le tue disponibilità</h2>
              <button
                onClick={() => { setShowForm(!showForm); resetForm(); }}
                className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl font-semibold hover:shadow-glow-cyan transition-all transform hover:scale-105"
              >
                {showForm ? 'Annulla' : 'Nuova Disponibilità'}
              </button>
            </div>

            {/* Form */}
            {showForm && (
              <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10 animate-scale-in">
                <h3 className="text-2xl font-bold gradient-text mb-6">
                  {editingId ? 'Modifica Disponibilità' : 'Nuova Disponibilità'}
                </h3>
                <form onSubmit={handleSubmit} className="space-y-6">
                  {/* Service Selection */}
                  <div className="space-y-2">
                    <label className="block text-sm font-bold text-white/90">Servizio</label>
                    <select
                      value={formData.serviceId}
                      onChange={(e) => setFormData({ ...formData, serviceId: e.target.value })}
                      disabled={!!editingId}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300 disabled:opacity-50"
                      required
                    >
                      <option value="">Seleziona un servizio</option>
                      {services.map(service => (
                        <option key={service.id} value={service.id}>
                          {service.name} ({service.bookingModeName})
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Recurring or Specific Date */}
                  <div className="space-y-4">
                    <label className="block text-sm font-bold text-white/90">Tipo</label>
                    <div className="grid grid-cols-2 gap-4">
                      <label className={`glass-card-dark p-4 rounded-xl cursor-pointer border transition-all ${formData.isRecurring ? 'border-neon-green/50 shadow-glow-green' : 'border-white/10'}`}>
                        <input
                          type="radio"
                          checked={formData.isRecurring}
                          onChange={() => setFormData({ ...formData, isRecurring: true, specificDate: '' })}
                          disabled={!!editingId}
                          className="mr-3"
                        />
                        <span className="text-gray-300">Ricorrente (settimanale)</span>
                      </label>
                      <label className={`glass-card-dark p-4 rounded-xl cursor-pointer border transition-all ${!formData.isRecurring ? 'border-neon-purple/50 shadow-glow-purple' : 'border-white/10'}`}>
                        <input
                          type="radio"
                          checked={!formData.isRecurring}
                          onChange={() => setFormData({ ...formData, isRecurring: false, dayOfWeek: '' })}
                          disabled={!!editingId}
                          className="mr-3"
                        />
                        <span className="text-gray-300">Data specifica</span>
                      </label>
                    </div>
                  </div>

                  {/* Day of Week or Specific Date */}
                  {formData.isRecurring ? (
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Giorno della settimana</label>
                      <select
                        value={formData.dayOfWeek}
                        onChange={(e) => setFormData({ ...formData, dayOfWeek: e.target.value })}
                        disabled={!!editingId}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300 disabled:opacity-50"
                        required
                      >
                        <option value="">Seleziona giorno</option>
                        {DAYS_OF_WEEK.map(day => (
                          <option key={day.value} value={day.value}>{day.label}</option>
                        ))}
                      </select>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Data</label>
                      <input
                        type="date"
                        value={formData.specificDate}
                        onChange={(e) => setFormData({ ...formData, specificDate: e.target.value })}
                        disabled={!!editingId}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300 disabled:opacity-50"
                        required
                      />
                    </div>
                  )}

                  {/* Time Range */}
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Ora inizio</label>
                      <input
                        type="time"
                        value={formData.startTime}
                        onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Ora fine</label>
                      <input
                        type="time"
                        value={formData.endTime}
                        onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                        required
                      />
                    </div>
                  </div>

                  {/* Max Capacity */}
                  <div className="space-y-2">
                    <label className="block text-sm font-bold text-white/90">Capacità massima (opzionale)</label>
                    <input
                      type="number"
                      min="1"
                      value={formData.maxCapacity}
                      onChange={(e) => setFormData({ ...formData, maxCapacity: e.target.value })}
                      placeholder="Es. 10"
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    />
                    <p className="text-xs text-gray-400">Lascia vuoto per capacità illimitata</p>
                  </div>

                  {/* Submit */}
                  <button
                    type="submit"
                    className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                  >
                    Salva
                  </button>
                </form>
              </div>
            )}

            {/* Availabilities List */}
            <div className="space-y-4">
              {availabilities.length === 0 ? (
                <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
                  <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <p className="text-2xl text-gray-400">Nessuna disponibilità override configurata</p>
                  <p className="text-gray-500 mt-2">I servizi usano gli orari standard di apertura</p>
                </div>
              ) : (
                availabilities.map((avail, index) => (
                  <div
                    key={avail.id}
                    className={`glass-card rounded-3xl p-6 border transition-all hover:shadow-glow-cyan animate-slide-up ${avail.isActive ? 'border-white/10' : 'border-red-500/30 opacity-60'}`}
                    style={{ animationDelay: `${index * 0.05}s` }}
                  >
                    <div className="flex justify-between items-start">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-3">
                          <h3 className="text-2xl font-bold gradient-text">{getServiceName(avail.serviceId)}</h3>
                          <span className={`px-3 py-1 rounded-lg text-xs font-semibold ${avail.isActive ? 'bg-neon-green/20 text-neon-green border border-neon-green/30' : 'bg-red-500/20 text-red-400 border border-red-500/30'}`}>
                            {avail.isActive ? 'Attivo' : 'Disattivato'}
                          </span>
                          <span className={`px-3 py-1 rounded-lg text-xs font-semibold ${avail.isRecurring ? 'bg-neon-blue/20 text-neon-blue border border-neon-blue/30' : 'bg-neon-purple/20 text-neon-purple border border-neon-purple/30'}`}>
                            {avail.isRecurring ? 'Ricorrente' : 'Data specifica'}
                          </span>
                        </div>

                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                          <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                            <span className="text-gray-400 block mb-1">Giorno/Data</span>
                            <span className="text-white font-semibold">
                              {avail.isRecurring ? getDayName(avail.dayOfWeek) : formatDate(avail.specificDate)}
                            </span>
                          </div>
                          <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                            <span className="text-gray-400 block mb-1">Orario</span>
                            <span className="text-neon-cyan font-semibold">
                              {formatTime(avail.startTime)} - {formatTime(avail.endTime)}
                            </span>
                          </div>
                          <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                            <span className="text-gray-400 block mb-1">Capacità</span>
                            <span className="text-neon-green font-semibold">
                              {avail.maxCapacity || 'Illimitata'}
                            </span>
                          </div>
                          {avail.slots && avail.slots.length > 0 && (
                            <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                              <span className="text-gray-400 block mb-1">Slot</span>
                              <span className="text-neon-purple font-semibold">
                                {avail.slots.length} slot configurati
                              </span>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="mt-4 pt-4 border-t border-white/10 flex gap-3">
                      <button
                        onClick={() => handleEdit(avail)}
                        className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10 transform hover:scale-105"
                      >
                        Modifica
                      </button>
                      <button
                        onClick={() => handleToggleActive(avail)}
                        className={`glass-card-dark px-5 py-2.5 rounded-xl transition-all font-semibold border border-white/10 transform hover:scale-105 ${avail.isActive ? 'text-gray-300 hover:text-yellow-400 hover:border-yellow-500/50' : 'text-gray-300 hover:text-neon-green hover:border-neon-green/50'}`}
                      >
                        {avail.isActive ? 'Disattiva' : 'Attiva'}
                      </button>
                      <button
                        onClick={() => handleDelete(avail.id)}
                        className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                      >
                        Elimina
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </>
        )}
      </div>
    </AppLayout>
  )
}

export default Availabilities
