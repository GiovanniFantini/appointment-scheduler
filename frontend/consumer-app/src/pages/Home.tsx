import { Link } from 'react-router-dom'
import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'

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
        startTime = new Date(`${bookingData.bookingDate}T${bookingData.startTime}`)
        endTime = new Date(startTime.getTime() + selectedService.durationMinutes * 60000)
      } else if (selectedService.bookingMode === 2) {
        // TimeRange: usa inizio e fine specificati dall'utente
        startTime = new Date(`${bookingData.bookingDate}T${bookingData.startTime}`)
        endTime = new Date(`${bookingData.bookingDate}T${bookingData.endTime}`)
      } else {
        // DayOnly: usa solo la data, con orari dummy (mezzanotte)
        startTime = new Date(`${bookingData.bookingDate}T00:00:00`)
        endTime = new Date(`${bookingData.bookingDate}T23:59:59`)
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
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Appointment Scheduler</h1>
          <div className="flex items-center gap-4">
            <span className="text-gray-600">Ciao, {user.firstName}!</span>
            <Link to="/bookings" className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
              Le mie prenotazioni
            </Link>
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Esci
            </button>
          </div>
        </div>
      </header>

      <section className="bg-gradient-to-r from-blue-600 to-purple-600 text-white py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-5xl font-bold mb-4">Prenota i tuoi servizi preferiti</h2>
          <p className="text-xl mb-8">Un'unica piattaforma per ristoranti, sport, wellness e molto altro</p>
        </div>
      </section>

      <section className="container mx-auto px-4 py-12">
        <h3 className="text-3xl font-bold text-gray-800 mb-8">Esplora per categoria</h3>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-8">
          <button
            onClick={() => setSelectedType(null)}
            className={`p-4 rounded-lg font-semibold ${!selectedType ? 'bg-blue-600 text-white' : 'bg-white text-gray-800'}`}
          >
            Tutti
          </button>
          {categories.map((cat) => (
            <button
              key={cat.id}
              onClick={() => setSelectedType(cat.id)}
              className={`p-4 rounded-lg font-semibold ${selectedType === cat.id ? 'bg-blue-600 text-white' : 'bg-white text-gray-800'}`}
            >
              {cat.icon} {cat.name}
            </button>
          ))}
        </div>

        {/* Modal di prenotazione dinamico */}
        {showBookingForm && selectedService && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-lg p-8 max-w-lg w-full max-h-[90vh] overflow-y-auto">
              <h3 className="text-2xl font-bold mb-2">{selectedService.name}</h3>
              <p className="text-gray-600 mb-4">{selectedService.merchantName}</p>
              <div className="bg-blue-50 p-3 rounded mb-4 text-sm">
                {getBookingModeDescription(selectedService.bookingMode)}
              </div>

              <form onSubmit={submitBooking} className="space-y-4">
                {/* Data (sempre presente) */}
                <div>
                  <label className="block text-sm font-bold mb-2">Data *</label>
                  <input
                    type="date"
                    value={bookingData.bookingDate}
                    onChange={(e) => setBookingData({ ...bookingData, bookingDate: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                    min={new Date().toISOString().split('T')[0]}
                  />
                </div>

                {/* TimeSlot Mode - Mostra slot disponibili */}
                {selectedService.bookingMode === 1 && (
                  <>
                    {bookingData.bookingDate && (
                      <div>
                        <label className="block text-sm font-bold mb-2">Seleziona Slot *</label>
                        {loadingSlots ? (
                          <div className="text-center py-4 text-gray-600">Caricamento slot...</div>
                        ) : availableSlots.length === 0 ? (
                          <div className="bg-yellow-50 border border-yellow-200 rounded p-4 text-sm text-yellow-800">
                            Nessuno slot disponibile per questa data. Prova un'altra data.
                          </div>
                        ) : (
                          <div className="grid grid-cols-3 gap-2 max-h-60 overflow-y-auto">
                            {availableSlots.map((slot, idx) => (
                              <button
                                key={idx}
                                type="button"
                                onClick={() => setBookingData({ ...bookingData, startTime: slot.slotTime })}
                                disabled={!slot.isAvailable}
                                className={`p-3 rounded border text-sm ${
                                  bookingData.startTime === slot.slotTime
                                    ? 'bg-blue-600 text-white border-blue-600'
                                    : slot.isAvailable
                                    ? 'bg-white hover:bg-gray-50 border-gray-300'
                                    : 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed'
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
                      <label className="block text-sm font-bold mb-2">Orario Inizio *</label>
                      <input
                        type="time"
                        value={bookingData.startTime}
                        onChange={(e) => setBookingData({ ...bookingData, startTime: e.target.value })}
                        className="w-full px-3 py-2 border rounded-lg"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-bold mb-2">Orario Fine *</label>
                      <input
                        type="time"
                        value={bookingData.endTime}
                        onChange={(e) => setBookingData({ ...bookingData, endTime: e.target.value })}
                        className="w-full px-3 py-2 border rounded-lg"
                        required
                      />
                    </div>
                  </div>
                )}

                {/* DayOnly Mode - Nessun campo orario */}
                {selectedService.bookingMode === 3 && (
                  <div className="bg-green-50 p-4 rounded text-sm text-green-800">
                    ✓ Prenotazione per l'intera giornata selezionata
                  </div>
                )}

                {/* Numero persone */}
                <div>
                  <label className="block text-sm font-bold mb-2">Numero persone *</label>
                  <input
                    type="number"
                    min="1"
                    value={bookingData.numberOfPeople}
                    onChange={(e) => setBookingData({ ...bookingData, numberOfPeople: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>

                {/* Note */}
                <div>
                  <label className="block text-sm font-bold mb-2">Note (opzionale)</label>
                  <textarea
                    value={bookingData.notes}
                    onChange={(e) => setBookingData({ ...bookingData, notes: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    rows={3}
                    placeholder="Eventuali richieste speciali..."
                  />
                </div>

                <div className="flex gap-2 pt-4">
                  <button
                    type="submit"
                    disabled={selectedService.bookingMode === 1 && !bookingData.startTime}
                    className="flex-1 bg-green-600 text-white py-3 rounded-lg hover:bg-green-700 font-semibold disabled:bg-gray-300 disabled:cursor-not-allowed"
                  >
                    Conferma Prenotazione
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setShowBookingForm(false)
                      setBookingData({ bookingDate: '', startTime: '', endTime: '', numberOfPeople: 1, notes: '' })
                      setAvailableSlots([])
                    }}
                    className="flex-1 bg-gray-200 text-gray-800 py-3 rounded-lg hover:bg-gray-300 font-semibold"
                  >
                    Annulla
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Lista servizi */}
        <div className="grid gap-6">
          {services.length === 0 ? (
            <div className="bg-white rounded-lg shadow p-12 text-center">
              <p className="text-xl text-gray-600">Nessun servizio disponibile in questa categoria</p>
            </div>
          ) : (
            services.map((service) => (
              <div key={service.id} className="bg-white rounded-lg shadow-lg p-6 hover:shadow-xl transition">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h4 className="text-2xl font-bold text-gray-800">{service.name}</h4>
                    <p className="text-gray-600 mb-2">{service.merchantName}</p>
                    {service.description && <p className="text-gray-700 mb-3">{service.description}</p>}
                    <div className="flex gap-4 text-sm text-gray-500 mb-2">
                      {service.price && <span>💰 €{service.price}</span>}
                      <span>⏱️ {service.durationMinutes} min</span>
                    </div>
                    <div className="text-sm text-blue-600">
                      {getBookingModeDescription(service.bookingMode)}
                    </div>
                  </div>
                  <button
                    onClick={() => handleBooking(service)}
                    className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 font-semibold"
                  >
                    Prenota
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  )
}

export default Home
