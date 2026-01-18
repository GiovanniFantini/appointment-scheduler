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
      case 1: return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30'
      case 2: return 'bg-neon-green/20 text-neon-green border-neon-green/30'
      case 3: return 'bg-red-500/20 text-red-400 border-red-500/30'
      case 4: return 'bg-neon-blue/20 text-neon-blue border-neon-blue/30'
      default: return 'bg-gray-500/20 text-gray-400 border-gray-500/30'
    }
  }

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Futuristic Header */}
      <header className="glass-card border-b border-white/10 sticky top-0 z-40 backdrop-blur-xl">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold gradient-text flex items-center gap-2">
              <svg className="w-8 h-8 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
              Le Mie Prenotazioni
            </h1>
            <div className="flex items-center gap-3">
              <Link to="/" className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10">
                🏠 Home
              </Link>
              <button onClick={onLogout} className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10">
                Esci
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Background Animation */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-purple opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-blue opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="container mx-auto px-4 py-12 relative z-10">
        {loading ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
            <div className="flex items-center justify-center gap-3 text-neon-cyan">
              <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span className="text-xl font-semibold">Caricamento prenotazioni...</span>
            </div>
          </div>
        ) : bookings.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>
            <p className="text-2xl text-gray-400 mb-6">Non hai ancora prenotazioni</p>
            <Link to="/" className="inline-flex items-center gap-2 bg-gradient-to-r from-neon-blue to-neon-purple text-white px-8 py-4 rounded-2xl hover:shadow-glow-blue font-bold transition-all transform hover:scale-105">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Esplora Servizi
            </Link>
          </div>
        ) : (
          <div className="grid gap-6">
            {bookings.map((booking, index) => (
              <div key={booking.id} className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <div className="flex justify-between items-start gap-6">
                  <div className="flex-1">
                    <h3 className="text-2xl font-bold gradient-text mb-3">{booking.serviceName}</h3>
                    <div className="glass-card-dark inline-block px-3 py-1 rounded-lg mb-3 border border-neon-purple/30">
                      <span className="text-neon-purple text-sm font-semibold">🏪 {booking.merchantName}</span>
                    </div>
                    <div className="flex flex-wrap gap-3 mb-3">
                      <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-cyan/20">
                        <span className="text-neon-cyan">📅 {new Date(booking.bookingDate).toLocaleDateString('it-IT', { timeZone: 'UTC' })}</span>
                      </div>
                      <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-blue/20">
                        <span className="text-neon-blue">🕐 {new Date(booking.startTime).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })}</span>
                      </div>
                      <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-purple/20">
                        <span className="text-neon-purple">👥 {booking.numberOfPeople} {booking.numberOfPeople === 1 ? 'persona' : 'persone'}</span>
                      </div>
                    </div>
                    {booking.notes && (
                      <div className="glass-card-dark p-3 rounded-xl mt-3 border border-white/5">
                        <p className="text-gray-400 text-sm"><span className="text-neon-cyan font-semibold">📝 Note:</span> {booking.notes}</p>
                      </div>
                    )}
                  </div>
                  <span className={`px-4 py-2 rounded-xl font-bold border ${getStatusColor(booking.status)}`}>
                    {booking.statusText}
                  </span>
                </div>
                {(booking.status === 1 || booking.status === 2) && (
                  <div className="mt-6 pt-6 border-t border-white/10">
                    <button
                      onClick={() => handleCancel(booking.id)}
                      className="glass-card-dark px-6 py-3 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                    >
                      ❌ Cancella Prenotazione
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
