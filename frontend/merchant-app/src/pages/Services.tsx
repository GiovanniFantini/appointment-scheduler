import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import axios from 'axios'

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
      const token = localStorage.getItem('token')
      const response = await axios.get('/api/services/my-services', {
        headers: { Authorization: `Bearer ${token}` }
      })
      setServices(response.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const token = localStorage.getItem('token')

    try {
      if (editingId) {
        await axios.put(`/api/services/${editingId}`, formData, {
          headers: { Authorization: `Bearer ${token}` }
        })
      } else {
        await axios.post('/api/services', formData, {
          headers: { Authorization: `Bearer ${token}` }
        })
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
      const token = localStorage.getItem('token')
      await axios.delete(`/api/services/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
      })
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
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">I Miei Servizi</h1>
          <div className="flex items-center gap-4">
            <Link to="/" className="text-gray-600 hover:text-gray-800">Dashboard</Link>
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Esci
            </button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => { setShowForm(!showForm); setEditingId(null); resetForm(); }}
            className="bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 font-semibold"
          >
            {showForm ? 'Annulla' : '+ Nuovo Servizio'}
          </button>
        </div>

        {showForm && (
          <div className="bg-white rounded-lg shadow-lg p-6 mb-6">
            <h2 className="text-xl font-bold mb-4">{editingId ? 'Modifica Servizio' : 'Nuovo Servizio'}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Nome</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Tipo Servizio</label>
                  <select
                    value={formData.serviceType}
                    onChange={(e) => setFormData({ ...formData, serviceType: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value={1}>Ristorante</option>
                    <option value={2}>Sport</option>
                    <option value={3}>Wellness</option>
                    <option value={4}>Healthcare</option>
                    <option value={5}>Professional</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-bold mb-2">Descrizione</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  rows={3}
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Prezzo (opzionale)</label>
                  <input
                    type="number"
                    step="0.01"
                    value={formData.price}
                    onChange={(e) => setFormData({ ...formData, price: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Durata (minuti)</label>
                  <input
                    type="number"
                    value={formData.durationMinutes}
                    onChange={(e) => setFormData({ ...formData, durationMinutes: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
              </div>

              <div className="border-t pt-4 mt-4">
                <h3 className="text-md font-bold mb-3">Modalità Prenotazione</h3>
                <div className="grid grid-cols-3 gap-4 mb-4">
                  <label className="flex items-center">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 1}
                      onChange={() => setFormData({ ...formData, bookingMode: 1 })}
                      className="mr-2"
                    />
                    Slot Orari
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 2}
                      onChange={() => setFormData({ ...formData, bookingMode: 2 })}
                      className="mr-2"
                    />
                    Orario Flessibile
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      checked={formData.bookingMode === 3}
                      onChange={() => setFormData({ ...formData, bookingMode: 3 })}
                      className="mr-2"
                    />
                    Solo Giorno
                  </label>
                </div>

                {formData.bookingMode === 1 && (
                  <div className="grid grid-cols-2 gap-4 bg-blue-50 p-4 rounded">
                    <div>
                      <label className="block text-sm font-bold mb-2">Durata Slot (min)</label>
                      <input
                        type="number"
                        value={formData.slotDurationMinutes}
                        onChange={(e) => setFormData({ ...formData, slotDurationMinutes: e.target.value })}
                        className="w-full px-3 py-2 border rounded-lg"
                        placeholder="Es. 30"
                      />
                      <p className="text-xs text-gray-600 mt-1">Lascia vuoto per usare la durata generale</p>
                    </div>
                    <div>
                      <label className="block text-sm font-bold mb-2">Capacità per Slot</label>
                      <input
                        type="number"
                        value={formData.maxCapacityPerSlot}
                        onChange={(e) => setFormData({ ...formData, maxCapacityPerSlot: e.target.value })}
                        className="w-full px-3 py-2 border rounded-lg"
                        placeholder="Es. 20"
                        min="1"
                      />
                      <p className="text-xs text-gray-600 mt-1">Numero max persone per slot</p>
                    </div>
                  </div>
                )}
              </div>

              <div>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="mr-2"
                  />
                  Servizio attivo
                </label>
              </div>
              <button type="submit" className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700">
                Salva
              </button>
            </form>
          </div>
        )}

        {loading ? (
          <div className="text-center py-12">Caricamento...</div>
        ) : services.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <p className="text-xl text-gray-600">Nessun servizio ancora creato</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {services.map((service) => (
              <div key={service.id} className="bg-white rounded-lg shadow-lg p-6">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h3 className="text-xl font-bold">{service.name}</h3>
                    <p className="text-gray-600 mt-2">{service.description}</p>
                    <div className="mt-3 flex gap-4 text-sm text-gray-500">
                      <span>Tipo: {service.serviceTypeName}</span>
                      {service.price && <span>Prezzo: €{service.price}</span>}
                      <span>Durata: {service.durationMinutes} min</span>
                      <span>📋 {service.bookingModeName}</span>
                    </div>
                  </div>
                  <div>
                    <span className={`px-3 py-1 rounded-full text-sm font-semibold ${service.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                      {service.isActive ? 'Attivo' : 'Non attivo'}
                    </span>
                  </div>
                </div>
                <div className="mt-4 flex gap-2">
                  <button
                    onClick={() => handleEdit(service)}
                    className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
                  >
                    Modifica
                  </button>
                  <button
                    onClick={() => handleDelete(service.id)}
                    className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700"
                  >
                    Elimina
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default Services
