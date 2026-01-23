import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface Service {
  id: number
  name: string
  description: string | null
  serviceType: number
  serviceTypeName: string
  price: number | null
  durationMinutes: number
  isActive: boolean
  bookingMode: number
  bookingModeName: string
  slotDurationMinutes: number | null
  maxCapacityPerSlot: number | null
}

interface ServicesProps {
  user: any
  onLogout: () => void
}

/**
 * Pagina per gestire i servizi del merchant (CRUD completo)
 */
function Services({ user, onLogout }: ServicesProps) {
  const [services, setServices] = useState<Service[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    serviceType: 1,
    price: '',
    durationMinutes: 60,
    isActive: true,
    bookingMode: 1,
    slotDurationMinutes: '',
    maxCapacityPerSlot: ''
  })

  useEffect(() => {
    fetchServices()
  }, [])

  const fetchServices = async () => {
    try {
      const response = await apiClient.get('/services/my-services')
      setServices(response.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // Prepara i dati convertendo stringhe vuote in null e stringhe numeriche in numeri
    const payload = {
      ...formData,
      price: formData.price === '' ? null : parseFloat(formData.price),
      slotDurationMinutes: formData.slotDurationMinutes === '' ? null : parseInt(formData.slotDurationMinutes),
      maxCapacityPerSlot: formData.maxCapacityPerSlot === '' ? null : parseInt(formData.maxCapacityPerSlot)
    }

    try {
      if (editingId) {
        await apiClient.put(`/services/${editingId}`, payload)
      } else {
        await apiClient.post('/services', payload)
      }
      fetchServices()
      setShowForm(false)
      setEditingId(null)
      resetForm()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nel salvataggio')
    }
  }

  const handleEdit = (service: Service) => {
    setFormData({
      name: service.name,
      description: service.description || '',
      serviceType: service.serviceType,
      price: service.price?.toString() || '',
      durationMinutes: service.durationMinutes,
      isActive: service.isActive,
      bookingMode: service.bookingMode,
      slotDurationMinutes: service.slotDurationMinutes?.toString() || '',
      maxCapacityPerSlot: service.maxCapacityPerSlot?.toString() || ''
    })
    setEditingId(service.id)
    setShowForm(true)
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo servizio?')) return

    try {
      await apiClient.delete(`/services/${id}`)
      fetchServices()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'eliminazione')
    }
  }

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      serviceType: 1,
      price: '',
      durationMinutes: 60,
      isActive: true,
      bookingMode: 1,
      slotDurationMinutes: '',
      maxCapacityPerSlot: ''
    })
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="I Miei Servizi">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => { setShowForm(!showForm); setEditingId(null); resetForm(); }}
            className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            {showForm ? '❌ Annulla' : '➕ Nuovo Servizio'}
          </button>
        </div>

        {showForm && (
          <div className="glass-card rounded-3xl shadow-lg p-6 mb-6 border border-white/10 animate-scale-in">
            <h2 className="text-2xl font-bold gradient-text mb-6">{editingId ? '✏️ Modifica Servizio' : '✨ Nuovo Servizio'}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Nome</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="Es: Taglio Capelli"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Tipo Servizio</label>
                  <select
                    value={formData.serviceType}
                    onChange={(e) => setFormData({ ...formData, serviceType: parseInt(e.target.value) })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  >
                    <option value={1}>🍝 Ristorante</option>
                    <option value={2}>⚽ Sport</option>
                    <option value={3}>💆 Wellness</option>
                    <option value={4}>🏥 Sanitario</option>
                    <option value={5}>💼 Professionale</option>
                  </select>
                </div>
              </div>
              <div className="space-y-2">
                <label className="block text-sm font-bold text-white/90">Descrizione</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  rows={3}
                  placeholder="Descrivi il tuo servizio..."
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Prezzo (opzionale)</label>
                  <input
                    type="number"
                    step="0.01"
                    value={formData.price}
                    onChange={(e) => setFormData({ ...formData, price: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="€ 50.00"
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Durata (minuti)</label>
                  <input
                    type="number"
                    value={formData.durationMinutes}
                    onChange={(e) => setFormData({ ...formData, durationMinutes: parseInt(e.target.value) })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    required
                  />
                </div>
              </div>

              <div className="border-t border-white/10 pt-4 mt-4">
                <h3 className="text-lg font-bold text-neon-green mb-3">📋 Modalità Prenotazione</h3>
                <div className="grid grid-cols-3 gap-4 mb-4">
                  <label className="glass-card-dark p-3 rounded-xl cursor-pointer hover:border-neon-blue/50 transition-all border border-white/10">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 1}
                      onChange={() => setFormData({ ...formData, bookingMode: 1 })}
                      className="mr-2"
                    />
                    <span className="text-gray-300">Slot Orari</span>
                  </label>
                  <label className="glass-card-dark p-3 rounded-xl cursor-pointer hover:border-neon-blue/50 transition-all border border-white/10">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 2}
                      onChange={() => setFormData({ ...formData, bookingMode: 2 })}
                      className="mr-2"
                    />
                    <span className="text-gray-300">Orario Flessibile</span>
                  </label>
                  <label className="glass-card-dark p-3 rounded-xl cursor-pointer hover:border-neon-blue/50 transition-all border border-white/10">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 3}
                      onChange={() => setFormData({ ...formData, bookingMode: 3 })}
                      className="mr-2"
                    />
                    <span className="text-gray-300">Solo Giorno</span>
                  </label>
                </div>

                {formData.bookingMode === 1 && (
                  <div className="grid grid-cols-2 gap-4 glass-card-dark p-4 rounded-xl border border-neon-blue/20">
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Durata Slot (min)</label>
                      <input
                        type="number"
                        value={formData.slotDurationMinutes}
                        onChange={(e) => setFormData({ ...formData, slotDurationMinutes: e.target.value })}
                        className="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                        placeholder="Es. 30"
                      />
                      <p className="text-xs text-gray-400">Lascia vuoto per usare la durata generale</p>
                    </div>
                    <div className="space-y-2">
                      <label className="block text-sm font-bold text-white/90">Capacità per Slot</label>
                      <input
                        type="number"
                        value={formData.maxCapacityPerSlot}
                        onChange={(e) => setFormData({ ...formData, maxCapacityPerSlot: e.target.value })}
                        className="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                        placeholder="Es. 20"
                        min="1"
                      />
                      <p className="text-xs text-gray-400">Numero max persone per slot</p>
                    </div>
                  </div>
                )}
              </div>

              <div>
                <label className="flex items-center glass-card-dark p-3 rounded-xl cursor-pointer border border-white/10">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="mr-2"
                  />
                  <span className="text-gray-300">✅ Servizio attivo</span>
                </label>
              </div>
              <button type="submit" className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold">
                💾 Salva
              </button>
            </form>
          </div>
        )}

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
        ) : services.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>
            <p className="text-2xl text-gray-400">Nessun servizio ancora creato</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {services.map((service, index) => (
              <div key={service.id} className="glass-card rounded-3xl shadow-lg p-6 border border-white/10 hover:border-neon-cyan/50 transition-all hover:shadow-glow-cyan animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h3 className="text-2xl font-bold gradient-text mb-2">{service.name}</h3>
                    <p className="text-gray-400 mb-3">{service.description}</p>
                    <div className="flex flex-wrap gap-3 text-sm">
                      <span className="glass-card-dark px-3 py-1.5 rounded-lg border border-neon-purple/30">
                        <span className="text-neon-purple">📦 {service.serviceTypeName}</span>
                      </span>
                      {service.price && (
                        <span className="glass-card-dark px-3 py-1.5 rounded-lg border border-neon-green/30">
                          <span className="text-neon-green">💰 €{service.price}</span>
                        </span>
                      )}
                      <span className="glass-card-dark px-3 py-1.5 rounded-lg border border-neon-cyan/30">
                        <span className="text-neon-cyan">⏱️ {service.durationMinutes} min</span>
                      </span>
                      <span className="glass-card-dark px-3 py-1.5 rounded-lg border border-neon-blue/30">
                        <span className="text-neon-blue">📋 {service.bookingModeName}</span>
                      </span>
                    </div>
                  </div>
                  <div>
                    <span className={`px-4 py-2 rounded-xl text-sm font-semibold border ${service.isActive ? 'bg-neon-green/20 text-neon-green border-neon-green/30' : 'bg-gray-500/20 text-gray-400 border-gray-500/30'}`}>
                      {service.isActive ? '✅ Attivo' : '⏸️ Non attivo'}
                    </span>
                  </div>
                </div>
                <div className="mt-6 pt-6 border-t border-white/10 flex gap-3">
                  <button
                    onClick={() => handleEdit(service)}
                    className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10 transform hover:scale-105"
                  >
                    ✏️ Modifica
                  </button>
                  <button
                    onClick={() => handleDelete(service.id)}
                    className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                  >
                    🗑️ Elimina
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </AppLayout>
  )
}

export default Services
