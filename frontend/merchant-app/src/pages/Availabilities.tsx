import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import axios from 'axios'

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

function Availabilities({ onLogout }: AvailabilitiesProps) {
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
      const token = localStorage.getItem('token')
      const [servicesRes, availabilitiesRes] = await Promise.all([
        axios.get('/api/services/my-services', {
          headers: { Authorization: `Bearer ${token}` }
        }),
        axios.get('/api/availability/my-availabilities', {
          headers: { Authorization: `Bearer ${token}` }
        })
      ])
      setServices(servicesRes.data)
      setAvailabilities(availabilitiesRes.data)
    } catch (err) {
      console.error(err)
      alert('Errore nel caricamento dei dati')
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const token = localStorage.getItem('token')

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
      await axios.post('/api/availability', requestData, {
        headers: { Authorization: `Bearer ${token}` }
      })
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

    const token = localStorage.getItem('token')
    const slots = [{
      slotTime: slotFormData.slotTime,
      maxCapacity: slotFormData.maxCapacity ? parseInt(slotFormData.maxCapacity) : null
    }]

    try {
      await axios.post(`/api/availability/${selectedAvailability.id}/slots`, slots, {
        headers: { Authorization: `Bearer ${token}` }
      })
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
      const token = localStorage.getItem('token')
      await axios.delete(`/api/availability/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
      })
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

  if (loading) return <div className="p-4">Caricamento...</div>

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <h1 className="text-xl font-bold text-gray-900">Gestione Disponibilità</h1>
          <div className="flex items-center gap-4">
            <Link to="/dashboard" className="text-blue-600 hover:text-blue-800">Dashboard</Link>
            <Link to="/services" className="text-blue-600 hover:text-blue-800">Servizi</Link>
            <Link to="/bookings" className="text-blue-600 hover:text-blue-800">Prenotazioni</Link>
            <button onClick={onLogout} className="text-red-600 hover:text-red-800">Logout</button>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-6 flex justify-between items-center">
          <h2 className="text-2xl font-bold text-gray-900">Le tue disponibilità</h2>
          <button
            onClick={() => setShowForm(true)}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            + Nuova Disponibilità
          </button>
        </div>

        {/* Form creazione availability */}
        {showForm && (
          <div className="bg-white p-6 rounded-lg shadow mb-6">
            <h3 className="text-lg font-semibold mb-4">
              Crea Nuova Disponibilità
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Servizio *
                </label>
                <select
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
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

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tipo Disponibilità
                </label>
                <div className="flex gap-4">
                  <label className="flex items-center">
                    <input
                      type="radio"
                      checked={formData.isRecurring}
                      onChange={() => setFormData({ ...formData, isRecurring: true })}
                      className="mr-2"
                    />
                    Ricorrente (settimanale)
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      checked={!formData.isRecurring}
                      onChange={() => setFormData({ ...formData, isRecurring: false })}
                      className="mr-2"
                    />
                    Data specifica
                  </label>
                </div>
              </div>

              {formData.isRecurring ? (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Giorno della settimana *
                  </label>
                  <select
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
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
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Data *
                  </label>
                  <input
                    type="date"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    value={formData.specificDate}
                    onChange={(e) => setFormData({ ...formData, specificDate: e.target.value })}
                    required
                  />
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Orario inizio *
                  </label>
                  <input
                    type="time"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    value={formData.startTime}
                    onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Orario fine *
                  </label>
                  <input
                    type="time"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    value={formData.endTime}
                    onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Capacità massima (opzionale)
                </label>
                <input
                  type="number"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                  value={formData.maxCapacity}
                  onChange={(e) => setFormData({ ...formData, maxCapacity: e.target.value })}
                  placeholder="Lascia vuoto per illimitato"
                  min="1"
                />
              </div>

              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => { setShowForm(false); resetForm() }}
                  className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Annulla
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Crea Disponibilità
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Form aggiunta slot */}
        {showSlotForm && selectedAvailability && (
          <div className="bg-white p-6 rounded-lg shadow mb-6">
            <h3 className="text-lg font-semibold mb-4">
              Aggiungi Slot Orario
            </h3>
            <p className="text-sm text-gray-600 mb-4">
              Servizio: {getServiceName(selectedAvailability.serviceId)} -
              {selectedAvailability.isRecurring
                ? ` ${selectedAvailability.dayOfWeekName}`
                : ` ${new Date(selectedAvailability.specificDate!).toLocaleDateString()}`}
              {' '}({selectedAvailability.startTime} - {selectedAvailability.endTime})
            </p>
            <form onSubmit={handleAddSlot} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Orario Slot *
                </label>
                <input
                  type="time"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                  value={slotFormData.slotTime}
                  onChange={(e) => setSlotFormData({ ...slotFormData, slotTime: e.target.value })}
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Capacità massima slot (opzionale)
                </label>
                <input
                  type="number"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                  value={slotFormData.maxCapacity}
                  onChange={(e) => setSlotFormData({ ...slotFormData, maxCapacity: e.target.value })}
                  placeholder="Usa capacità availability"
                  min="1"
                />
              </div>

              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowSlotForm(false)
                    setSelectedAvailability(null)
                    setSlotFormData({ slotTime: '09:00', maxCapacity: '' })
                  }}
                  className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Annulla
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Aggiungi Slot
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Lista availabilities */}
        <div className="space-y-4">
          {availabilities.length === 0 ? (
            <div className="bg-white p-8 rounded-lg shadow text-center text-gray-500">
              Nessuna disponibilità configurata. Creane una per iniziare!
            </div>
          ) : (
            availabilities.map(avail => (
              <div key={avail.id} className="bg-white p-6 rounded-lg shadow">
                <div className="flex justify-between items-start mb-4">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900">
                      {getServiceName(avail.serviceId)}
                    </h3>
                    <div className="text-sm text-gray-600 space-y-1 mt-2">
                      <p>
                        {avail.isRecurring
                          ? `🔄 Ogni ${avail.dayOfWeekName}`
                          : `📅 ${new Date(avail.specificDate!).toLocaleDateString()}`}
                      </p>
                      <p>
                        🕐 {avail.startTime} - {avail.endTime}
                      </p>
                      {avail.maxCapacity && (
                        <p>👥 Capacità massima: {avail.maxCapacity}</p>
                      )}
                      <p>
                        <span className={`px-2 py-1 rounded text-xs ${avail.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                          {avail.isActive ? 'Attivo' : 'Disattivo'}
                        </span>
                      </p>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {getServiceBookingMode(avail.serviceId) === 1 && (
                      <button
                        onClick={() => {
                          setSelectedAvailability(avail)
                          setShowSlotForm(true)
                        }}
                        className="text-blue-600 hover:text-blue-800 text-sm"
                      >
                        + Slot
                      </button>
                    )}
                    <button
                      onClick={() => handleDelete(avail.id)}
                      className="text-red-600 hover:text-red-800 text-sm"
                    >
                      Elimina
                    </button>
                  </div>
                </div>

                {/* Mostra slot se presenti */}
                {avail.slots.length > 0 && (
                  <div className="mt-4 border-t pt-4">
                    <p className="text-sm font-medium text-gray-700 mb-2">Slot Orari:</p>
                    <div className="flex flex-wrap gap-2">
                      {avail.slots.map(slot => (
                        <span key={slot.id} className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm">
                          {slot.slotTime}
                          {slot.maxCapacity && ` (max ${slot.maxCapacity})`}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </main>
    </div>
  )
}

export default Availabilities
