import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface HomeProps {
  user: any
  onLogout: () => void
}

interface Service {
  id: number
  name: string
  description: string | null
  merchantName: string
  serviceType: number
  price: number | null
  durationMinutes: number
  bookingMode: number
  bookingModeName: string
}

interface AvailableSlot {
  date: string
  slotTime: string
  availableCapacity: number
  totalCapacity: number
  isAvailable: boolean
}

/**
 * Home page per utenti consumer - visualizza servizi disponibili con booking dinamico
 */
function Home({ user, onLogout }: HomeProps) {
  const [services, setServices] = useState<Service[]>([])
  const [selectedType, setSelectedType] = useState<number | null>(null)
  const [showBookingForm, setShowBookingForm] = useState(false)
  const [selectedService, setSelectedService] = useState<Service | null>(null)
  const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([])
  const [loadingSlots, setLoadingSlots] = useState(false)
  const [bookingData, setBookingData] = useState({
    bookingDate: '',
    startTime: '',
    endTime: '',
    numberOfPeople: 1,
    notes: ''
  })

  const categories = [
    { id: 1, name: 'Ristoranti', icon: '🍽️' },
    { id: 2, name: 'Sport', icon: '⚽' },
    { id: 3, name: 'Wellness', icon: '💆' },
    { id: 4, name: 'Healthcare', icon: '🏥' },
    { id: 5, name: 'Professionisti', icon: '💼' },
  ]

  useEffect(() => {
    fetchServices()
  }, [selectedType])

  // Fetch available slots when date changes (only for TimeSlot mode)
  useEffect(() => {
    if (selectedService?.bookingMode === 1 && bookingData.bookingDate) {
      fetchAvailableSlots()
    }
  }, [bookingData.bookingDate, selectedService])

  const fetchServices = async () => {
    try {
      const url = selectedType
        ? `/services?serviceType=${selectedType}`
        : '/services'
      const response = await apiClient.get(url)
      setServices(response.data)
    } catch (err) {
      console.error(err)
    }
  }

  const fetchAvailableSlots = async () => {
    if (!selectedService || !bookingData.bookingDate) return

    setLoadingSlots(true)
    try {
      const response = await apiClient.get('/availability/available-slots', {
        params: {
          serviceId: selectedService.id,
          date: bookingData.bookingDate
        }
      })
      setAvailableSlots(response.data)
    } catch (err) {
      console.error(err)
      setAvailableSlots([])
    } finally {
      setLoadingSlots(false)
    }
  }

  const handleBooking = (service: Service) => {
    setSelectedService(service)
    setShowBookingForm(true)
    setBookingData({ bookingDate: '', startTime: '', endTime: '', numberOfPeople: 1, notes: '' })
    setAvailableSlots([])
  }

  const submitBooking = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedService) return

    try {
      let startTime: Date
      let endTime: Date

      // Costruisci startTime e endTime basandosi sul BookingMode
      if (selectedService.bookingMode === 1) {
        // TimeSlot: usa lo slot selezionato
        startTime = new Date(`${bookingData.bookingDate}T${bookingData.startTime}Z`)
        endTime = new Date(startTime.getTime() + selectedService.durationMinutes * 60000)
      } else if (selectedService.bookingMode === 2) {
        // TimeRange: usa inizio e fine specificati dall'utente
        startTime = new Date(`${bookingData.bookingDate}T${bookingData.startTime}Z`)
        endTime = new Date(`${bookingData.bookingDate}T${bookingData.endTime}Z`)
      } else {
        // DayOnly: usa solo la data, con orari dummy (mezzanotte)
        startTime = new Date(`${bookingData.bookingDate}T00:00:00Z`)
        endTime = new Date(`${bookingData.bookingDate}T23:59:59Z`)
      }

      await apiClient.post('/bookings', {
        serviceId: selectedService.id,
        bookingDate: startTime.toISOString(),
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        numberOfPeople: bookingData.numberOfPeople,
        notes: bookingData.notes
      })

      alert('Prenotazione effettuata con successo! In attesa di conferma dal merchant.')
      setShowBookingForm(false)
      setBookingData({ bookingDate: '', startTime: '', endTime: '', numberOfPeople: 1, notes: '' })
      setAvailableSlots([])
    } catch (err: any) {
      alert(err.response?.data || 'Errore nella prenotazione')
    }
  }

  const getBookingModeDescription = (mode: number) => {
    switch (mode) {
      case 1:
        return '📅 Prenotazione con slot orari fissi'
      case 2:
        return '🕐 Prenotazione con orario flessibile'
      case 3:
        return '📆 Prenotazione giornaliera'
      default:
        return ''
    }
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Catalogo Servizi">
      {/* Categories Section */}
      <section className="container mx-auto px-4 py-12">
        <h3 className="text-4xl font-bold gradient-text mb-8 text-center">Esplora per Categoria</h3>
        <div className="grid grid-cols-2 md:grid-cols-6 gap-4 mb-12">
          <button
            onClick={() => setSelectedType(null)}
            className={`p-6 rounded-2xl font-semibold transition-all transform hover:scale-105 ${
              !selectedType
                ? 'bg-gradient-to-br from-neon-blue to-neon-purple text-white shadow-glow-blue'
                : 'glass-card hover-glow border border-white/10 text-gray-300'
            }`}
          >
            <div className="text-3xl mb-2">✨</div>
            <div className="text-sm">Tutti</div>
          </button>
          {categories.map((cat) => (
            <button
              key={cat.id}
              onClick={() => setSelectedType(cat.id)}
              className={`p-6 rounded-2xl font-semibold transition-all transform hover:scale-105 ${
                selectedType === cat.id
                  ? 'bg-gradient-to-br from-neon-blue to-neon-purple text-white shadow-glow-blue'
                  : 'glass-card hover-glow border border-white/10 text-gray-300'
              }`}
            >
              <div className="text-3xl mb-2">{cat.icon}</div>
              <div className="text-sm">{cat.name}</div>
            </button>
          ))}
        </div>

        {/* Modal di prenotazione futuristico */}
        {showBookingForm && selectedService && (
          <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
            <div className="glass-card rounded-3xl p-8 max-w-lg w-full max-h-[90vh] overflow-y-auto border border-white/20 shadow-glow-purple animate-scale-in">
              <div className="flex items-start justify-between mb-6">
                <div>
                  <h3 className="text-3xl font-bold gradient-text mb-2">{selectedService.name}</h3>
                  <p className="text-gray-400">{selectedService.merchantName}</p>
                </div>
                <button
                  onClick={() => {
                    setShowBookingForm(false)
                    setBookingData({ bookingDate: '', startTime: '', endTime: '', numberOfPeople: 1, notes: '' })
                    setAvailableSlots([])
                  }}
                  className="glass-card-dark p-2 rounded-xl hover:bg-red-500/20 transition-all"
                >
                  <svg className="w-6 h-6 text-gray-400 hover:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <div className="glass-card-dark p-4 rounded-xl mb-6 border border-neon-blue/20">
                <div className="text-neon-cyan text-sm font-semibold">
                  {getBookingModeDescription(selectedService.bookingMode)}
                </div>
              </div>

              <form onSubmit={submitBooking} className="space-y-5">
                {/* Data (sempre presente) */}
                <div>
                  <label className="block text-white/90 text-sm font-semibold mb-2">Data *</label>
                  <input
                    type="date"
                    value={bookingData.bookingDate}
                    onChange={(e) => setBookingData({ ...bookingData, bookingDate: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all"
                    required
                    min={new Date().toISOString().split('T')[0]}
                  />
                </div>

                {/* TimeSlot Mode - Mostra slot disponibili */}
                {selectedService.bookingMode === 1 && (
                  <>
                    {bookingData.bookingDate && (
                      <div>
                        <label className="block text-white/90 text-sm font-semibold mb-3">Seleziona Slot *</label>
                        {loadingSlots ? (
                          <div className="text-center py-8 text-gray-400 flex items-center justify-center gap-2">
                            <svg className="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            Caricamento slot...
                          </div>
                        ) : availableSlots.length === 0 ? (
                          <div className="glass-card-dark border border-yellow-500/30 rounded-xl p-4 text-sm text-yellow-400">
                            ⚠️ Nessuno slot disponibile per questa data. Prova un'altra data.
                          </div>
                        ) : (
                          <div className="grid grid-cols-3 gap-2 max-h-60 overflow-y-auto pr-2">
                            {availableSlots.map((slot, idx) => (
                              <button
                                key={idx}
                                type="button"
                                onClick={() => setBookingData({ ...bookingData, startTime: slot.slotTime })}
                                disabled={!slot.isAvailable}
                                className={`p-3 rounded-xl text-sm font-semibold transition-all transform ${
                                  bookingData.startTime === slot.slotTime
                                    ? 'bg-gradient-to-br from-neon-blue to-neon-purple text-white shadow-glow-blue scale-105'
                                    : slot.isAvailable
                                    ? 'glass-card hover:scale-105 border border-white/10 text-gray-300 hover:border-neon-blue/50'
                                    : 'bg-gray-800/30 text-gray-600 cursor-not-allowed border border-gray-700/30'
                                }`}
                              >
                                <div className="font-bold">{slot.slotTime.substring(0, 5)}</div>
                                <div className="text-xs mt-1">
                                  {slot.isAvailable ? `${slot.availableCapacity} posti` : 'Completo'}
                                </div>
                              </button>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                  </>
                )}

                {/* TimeRange Mode - Campi inizio e fine */}
                {selectedService.bookingMode === 2 && (
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-white/90 text-sm font-semibold mb-2">Orario Inizio *</label>
                      <input
                        type="time"
                        value={bookingData.startTime}
                        onChange={(e) => setBookingData({ ...bookingData, startTime: e.target.value })}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-white/90 text-sm font-semibold mb-2">Orario Fine *</label>
                      <input
                        type="time"
                        value={bookingData.endTime}
                        onChange={(e) => setBookingData({ ...bookingData, endTime: e.target.value })}
                        className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all"
                        required
                      />
                    </div>
                  </div>
                )}

                {/* DayOnly Mode - Nessun campo orario */}
                {selectedService.bookingMode === 3 && (
                  <div className="glass-card-dark border border-green-500/30 rounded-xl p-4 text-sm text-green-400">
                    ✓ Prenotazione per l'intera giornata selezionata
                  </div>
                )}

                {/* Numero persone */}
                <div>
                  <label className="block text-white/90 text-sm font-semibold mb-2">Numero persone *</label>
                  <input
                    type="number"
                    min="1"
                    value={bookingData.numberOfPeople}
                    onChange={(e) => setBookingData({ ...bookingData, numberOfPeople: parseInt(e.target.value) })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
                    required
                  />
                </div>

                {/* Note */}
                <div>
                  <label className="block text-white/90 text-sm font-semibold mb-2">Note <span className="text-gray-500 text-xs">(opzionale)</span></label>
                  <textarea
                    value={bookingData.notes}
                    onChange={(e) => setBookingData({ ...bookingData, notes: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all resize-none"
                    rows={3}
                    placeholder="Eventuali richieste speciali..."
                  />
                </div>

                <div className="flex gap-3 pt-4">
                  <button
                    type="submit"
                    disabled={selectedService.bookingMode === 1 && !bookingData.startTime}
                    className="flex-1 bg-gradient-to-r from-neon-green to-neon-cyan text-white py-3.5 rounded-xl hover:shadow-glow-cyan font-semibold disabled:opacity-30 disabled:cursor-not-allowed transition-all transform hover:scale-105 active:scale-95"
                  >
                    ✓ Conferma Prenotazione
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setShowBookingForm(false)
                      setBookingData({ bookingDate: '', startTime: '', endTime: '', numberOfPeople: 1, notes: '' })
                      setAvailableSlots([])
                    }}
                    className="flex-1 glass-card-dark py-3.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 transition-all"
                  >
                    ✕ Annulla
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Lista servizi futuristica */}
        <div className="grid gap-6 pb-12">
          {services.length === 0 ? (
            <div className="glass-card rounded-3xl shadow-glow-purple p-16 text-center border border-white/10 animate-fade-in">
              <svg className="w-20 h-20 text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
              <p className="text-xl text-gray-400">Nessun servizio disponibile in questa categoria</p>
            </div>
          ) : (
            services.map((service, index) => (
              <div
                key={service.id}
                className="glass-card rounded-3xl p-6 hover:shadow-glow-blue transition-all border border-white/10 hover:border-neon-blue/50 transform hover:scale-[1.02] animate-slide-up"
                style={{ animationDelay: `${index * 0.1}s` }}
              >
                <div className="flex justify-between items-start gap-6">
                  <div className="flex-1">
                    <h4 className="text-3xl font-bold gradient-text mb-2">{service.name}</h4>
                    <div className="flex items-center gap-2 mb-3">
                      <div className="glass-card-dark px-3 py-1 rounded-lg border border-neon-purple/30">
                        <span className="text-neon-purple text-sm font-semibold">🏪 {service.merchantName}</span>
                      </div>
                    </div>
                    {service.description && (
                      <p className="text-gray-300 mb-4 leading-relaxed">{service.description}</p>
                    )}
                    <div className="flex flex-wrap gap-3 mb-3">
                      {service.price && (
                        <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-cyan/20">
                          <span className="text-neon-cyan font-bold">💰 €{service.price}</span>
                        </div>
                      )}
                      <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-blue/20">
                        <span className="text-neon-blue font-bold">⏱️ {service.durationMinutes} min</span>
                      </div>
                    </div>
                    <div className="glass-card-dark inline-block px-4 py-2 rounded-xl border border-neon-purple/20">
                      <span className="text-neon-purple text-sm font-semibold">
                        {getBookingModeDescription(service.bookingMode)}
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={() => handleBooking(service)}
                    className="bg-gradient-to-r from-neon-blue to-neon-purple text-white px-8 py-4 rounded-2xl hover:shadow-glow-blue font-bold transition-all transform hover:scale-110 active:scale-95 flex items-center gap-2 whitespace-nowrap"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                    Prenota
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </AppLayout>
  )
}

export default Home
