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

function Availabilities({ user, onLogout }: AvailabilitiesProps) {
  const [services, setServices] = useState<Service[]>([])
  const [availabilities, setAvailabilities] = useState<Availability[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)

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

  const getServiceName = (serviceId: number) => {
    return services.find(s => s.id === serviceId)?.name || 'Sconosciuto'
  }

return (
  <AppLayout user={user} onLogout={onLogout} pageTitle="Disponibilità">
    <div className="container mx-auto px-4 py-8">
      {loading ? (
        <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
          <div className="flex items-center justify-center gap-3 text-neon-cyan">
            <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
              />
            </svg>
            <span className="text-xl font-semibold">Caricamento...</span>
          </div>
        </div>
      ) : (
        <>
          <div className="mb-6 flex justify-between items-center">
            <h2 className="text-3xl font-bold gradient-text">
              Le tue disponibilità
            </h2>
            <button
              onClick={() => setShowForm(true)}
              className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl font-semibold"
            >
              ➕ Nuova Disponibilità
            </button>
          </div>

          {showForm && (
            <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
              {/* FORM CREA DISPONIBILITÀ */}
            </div>
          )}

          <div className="space-y-6">
            {availabilities.length === 0 ? (
              <div className="glass-card rounded-3xl p-16 text-center border border-white/10">
                <p className="text-2xl text-gray-400">
                  Nessuna disponibilità configurata
                </p>
              </div>
            ) : (
              availabilities.map(avail => (
                <div
                  key={avail.id}
                  className="glass-card rounded-3xl p-6 border border-white/10"
                >
                  <h3 className="text-2xl font-bold gradient-text">
                    {getServiceName(avail.serviceId)}
                  </h3>
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
