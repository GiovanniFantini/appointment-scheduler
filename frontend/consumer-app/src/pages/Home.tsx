import { Link } from 'react-router-dom'
import { useState, useEffect } from 'react'
import axios from 'axios'

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
}

/**
 * Home page per utenti consumer - visualizza servizi disponibili
 */
function Home({ user, onLogout }: HomeProps) {
  const [services, setServices] = useState<Service[]>([])
  const [selectedType, setSelectedType] = useState<number | null>(null)
  const [showBookingForm, setShowBookingForm] = useState(false)
  const [selectedService, setSelectedService] = useState<Service | null>(null)
  const [bookingData, setBookingData] = useState({
    bookingDate: '',
    startTime: '',
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

  const fetchServices = async () => {
    try {
      const url = selectedType
        ? `/api/services?serviceType=${selectedType}`
        : '/api/services'
      const response = await axios.get(url)
      setServices(response.data)
    } catch (err) {
      console.error(err)
    }
  }

  const handleBooking = (service: Service) => {
    setSelectedService(service)
    setShowBookingForm(true)
  }

  const submitBooking = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedService) return

    try {
      const token = localStorage.getItem('token')
      const startTime = new Date(`${bookingData.bookingDate}T${bookingData.startTime}`)
      const endTime = new Date(startTime.getTime() + selectedService.durationMinutes * 60000)

      await axios.post('/api/bookings', {
        serviceId: selectedService.id,
        bookingDate: startTime.toISOString(),
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        numberOfPeople: bookingData.numberOfPeople,
        notes: bookingData.notes
      }, {
        headers: { Authorization: `Bearer ${token}` }
      })

      alert('Prenotazione effettuata con successo!')
      setShowBookingForm(false)
      setBookingData({ bookingDate: '', startTime: '', numberOfPeople: 1, notes: '' })
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nella prenotazione')
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

        {showBookingForm && selectedService && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-8 max-w-md w-full">
              <h3 className="text-2xl font-bold mb-4">Prenota: {selectedService.name}</h3>
              <form onSubmit={submitBooking} className="space-y-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Data</label>
                  <input
                    type="date"
                    value={bookingData.bookingDate}
                    onChange={(e) => setBookingData({ ...bookingData, bookingDate: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                    min={new Date().toISOString().split('T')[0]}
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Orario</label>
                  <input
                    type="time"
                    value={bookingData.startTime}
                    onChange={(e) => setBookingData({ ...bookingData, startTime: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Numero persone</label>
                  <input
                    type="number"
                    min="1"
                    value={bookingData.numberOfPeople}
                    onChange={(e) => setBookingData({ ...bookingData, numberOfPeople: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Note (opzionale)</label>
                  <textarea
                    value={bookingData.notes}
                    onChange={(e) => setBookingData({ ...bookingData, notes: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    rows={3}
                  />
                </div>
                <div className="flex gap-2">
                  <button type="submit" className="flex-1 bg-green-600 text-white py-2 rounded-lg hover:bg-green-700">
                    Conferma Prenotazione
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowBookingForm(false)}
                    className="flex-1 bg-gray-200 text-gray-800 py-2 rounded-lg hover:bg-gray-300"
                  >
                    Annulla
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

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
                    <div className="flex gap-4 text-sm text-gray-500">
                      {service.price && <span>Prezzo: €{service.price}</span>}
                      <span>Durata: {service.durationMinutes} minuti</span>
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
