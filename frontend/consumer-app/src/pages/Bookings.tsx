import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

interface BookingsProps {
  onLogout: () => void
}

interface Booking {
  id: number
  serviceName: string
  merchantName: string
  bookingDate: string
  startTime: string
  status: number
  statusText: string
  numberOfPeople: number
  notes: string | null
}

/**
 * Pagina per visualizzare e gestire le proprie prenotazioni
 */
function Bookings({ onLogout }: BookingsProps) {
  const [bookings, setBookings] = useState<Booking[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchBookings()
  }, [])

  const fetchBookings = async () => {
    try {
      const response = await apiClient.get('/bookings/my-bookings')
      setBookings(response.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleCancel = async (id: number) => {
    if (!confirm('Sei sicuro di voler cancellare questa prenotazione?')) return

    try {
      await apiClient.post(`/bookings/${id}/cancel`, {})
      fetchBookings()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nella cancellazione')
    }
  }

  const getStatusColor = (status: number) => {
    switch (status) {
      case 1: return 'bg-yellow-100 text-yellow-800'
      case 2: return 'bg-green-100 text-green-800'
      case 3: return 'bg-red-100 text-red-800'
      case 4: return 'bg-blue-100 text-blue-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Le mie prenotazioni</h1>
          <div className="flex items-center gap-4">
            <Link to="/" className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Home
            </Link>
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Esci
            </button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-12">
        {loading ? (
          <div className="text-center">Caricamento...</div>
        ) : bookings.length === 0 ? (
          <div className="text-center bg-white p-12 rounded-lg shadow">
            <p className="text-xl text-gray-600 mb-4">Non hai ancora prenotazioni</p>
            <Link to="/" className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 inline-block">
              Esplora servizi
            </Link>
          </div>
        ) : (
          <div className="grid gap-6">
            {bookings.map((booking) => (
              <div key={booking.id} className="bg-white p-6 rounded-lg shadow-lg">
                <div className="flex justify-between items-start">
                  <div>
                    <h3 className="text-xl font-bold text-gray-800 mb-2">{booking.serviceName}</h3>
                    <p className="text-gray-600 mb-1">{booking.merchantName}</p>
                    <p className="text-gray-500">
                      {new Date(booking.bookingDate).toLocaleDateString('it-IT')} - {' '}
                      {new Date(booking.startTime).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}
                    </p>
                    <p className="text-gray-500">Persone: {booking.numberOfPeople}</p>
                    {booking.notes && <p className="text-gray-500 mt-2">Note: {booking.notes}</p>}
                  </div>
                  <span className={`px-4 py-2 rounded-full font-semibold ${getStatusColor(booking.status)}`}>
                    {booking.statusText}
                  </span>
                </div>
                {(booking.status === 1 || booking.status === 2) && (
                  <div className="mt-4">
                    <button
                      onClick={() => handleCancel(booking.id)}
                      className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700"
                    >
                      Cancella prenotazione
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default Bookings
