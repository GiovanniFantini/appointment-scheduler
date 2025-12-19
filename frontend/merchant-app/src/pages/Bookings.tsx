import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import axios from 'axios'

interface Booking {
  id: number
  userName: string
  userEmail: string
  userPhone: string | null
  serviceName: string
  bookingDate: string
  startTime: string
  endTime: string
  status: number
  statusText: string
  numberOfPeople: number
  notes: string | null
}

interface BookingsProps {
  user: any
  onLogout: () => void
}

/**
 * Pagina per gestire le prenotazioni del merchant
 */
function Bookings({ user, onLogout }: BookingsProps) {
  const [bookings, setBookings] = useState<Booking[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchBookings()
  }, [])

  const fetchBookings = async () => {
    try {
      const token = localStorage.getItem('token')
      const response = await axios.get('/api/bookings/merchant-bookings', {
        headers: { Authorization: `Bearer ${token}` }
      })
      setBookings(response.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleConfirm = async (id: number) => {
    try {
      const token = localStorage.getItem('token')
      await axios.post(`/api/bookings/${id}/confirm`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      })
      fetchBookings()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nella conferma')
    }
  }

  const handleComplete = async (id: number) => {
    try {
      const token = localStorage.getItem('token')
      await axios.post(`/api/bookings/${id}/complete`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      })
      fetchBookings()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nel completamento')
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
          <h1 className="text-2xl font-bold text-gray-800">Prenotazioni</h1>
          <div className="flex items-center gap-4">
            <Link to="/" className="text-gray-600 hover:text-gray-800">Dashboard</Link>
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg">Esci</button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        {loading ? (
          <div className="text-center py-12">Caricamento...</div>
        ) : bookings.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <p className="text-xl text-gray-600">Nessuna prenotazione</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {bookings.map((booking) => (
              <div key={booking.id} className="bg-white rounded-lg shadow-lg p-6">
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-xl font-bold">{booking.serviceName}</h3>
                    <p className="text-gray-600">Cliente: {booking.userName}</p>
                    <p className="text-sm text-gray-500">{booking.userEmail}</p>
                    {booking.userPhone && <p className="text-sm text-gray-500">{booking.userPhone}</p>}
                  </div>
                  <span className={`px-4 py-2 rounded-full font-semibold ${getStatusColor(booking.status)}`}>
                    {booking.statusText}
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-4 mb-4">
                  <div>
                    <p className="text-sm text-gray-500">Data</p>
                    <p className="font-semibold">{new Date(booking.bookingDate).toLocaleDateString('it-IT')}</p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Orario</p>
                    <p className="font-semibold">
                      {new Date(booking.startTime).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Persone</p>
                    <p className="font-semibold">{booking.numberOfPeople}</p>
                  </div>
                </div>
                {booking.notes && (
                  <div className="mb-4">
                    <p className="text-sm text-gray-500">Note</p>
                    <p>{booking.notes}</p>
                  </div>
                )}
                <div className="flex gap-2">
                  {booking.status === 1 && (
                    <button
                      onClick={() => handleConfirm(booking.id)}
                      className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700"
                    >
                      Conferma
                    </button>
                  )}
                  {booking.status === 2 && (
                    <button
                      onClick={() => handleComplete(booking.id)}
                      className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
                    >
                      Completa
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default Bookings
