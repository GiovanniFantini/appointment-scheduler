import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

interface Service {
  id: number
  name: string
  serviceType: number
  bookingMode: number
  bookingModeName: string
}

interface BusinessHours {
  id: number
  serviceId: number
  dayOfWeek: number
  isClosed: boolean
  openingTime1: string | null
  closingTime1: string | null
  openingTime2: string | null
  closingTime2: string | null
  maxCapacity: number | null
}

interface BusinessHoursException {
  id: number
  serviceId: number
  date: string
  isClosed: boolean
  reason: string | null
  openingTime1: string | null
  closingTime1: string | null
  openingTime2: string | null
  closingTime2: string | null
  maxCapacity: number | null
}

interface DayConfig {
  dayOfWeek: number
  isClosed: boolean
  openingTime1: string
  closingTime1: string
  openingTime2: string
  closingTime2: string
  maxCapacity: string
}

interface BusinessHoursProps {
  onLogout: () => void
}

const DAYS = ['Lunedì', 'Martedì', 'Mercoledì', 'Giovedì', 'Venerdì', 'Sabato', 'Domenica']

function BusinessHours({ onLogout }: BusinessHoursProps) {
  const [services, setServices] = useState<Service[]>([])
  const [selectedServiceId, setSelectedServiceId] = useState<number | null>(null)
  const [businessHours, setBusinessHours] = useState<BusinessHours[]>([])
  const [exceptions, setExceptions] = useState<BusinessHoursException[]>([])
  const [loading, setLoading] = useState(true)
  const [showExceptionForm, setShowExceptionForm] = useState(false)
  const [exceptionFormData, setExceptionFormData] = useState({
    date: '',
    isClosed: false,
    reason: '',
    openingTime1: '12:00',
    closingTime1: '15:00',
    openingTime2: '19:00',
    closingTime2: '23:00',
    maxCapacity: ''
  })

  // Configurazione per ogni giorno della settimana
  const [weekConfig, setWeekConfig] = useState<DayConfig[]>(
    DAYS.map((_, index) => ({
      dayOfWeek: index,
      isClosed: index === 0, // Lunedì chiuso di default
      openingTime1: '12:00',
      closingTime1: '15:00',
      openingTime2: '19:00',
      closingTime2: '23:00',
      maxCapacity: ''
    }))
  )

  useEffect(() => {
    fetchServices()
  }, [])

  useEffect(() => {
    if (selectedServiceId) {
      fetchBusinessHours()
      fetchExceptions()
    }
  }, [selectedServiceId])

  const fetchServices = async () => {
    try {
      const response = await apiClient.get('/services/my-services')
      setServices(response.data)
      setLoading(false)
    } catch (err) {
      console.error(err)
      setLoading(false)
    }
  }

  const fetchBusinessHours = async () => {
    if (!selectedServiceId) return

    try {
      const response = await apiClient.get(`/businesshours/service/${selectedServiceId}`)
      const hours: BusinessHours[] = response.data

      if (hours.length > 0) {
        // Popola weekConfig con i dati esistenti
        const newWeekConfig = DAYS.map((_, index) => {
          const existing = hours.find(h => h.dayOfWeek === index)
          if (existing) {
            return {
              dayOfWeek: index,
              isClosed: existing.isClosed,
              openingTime1: existing.openingTime1 || '12:00',
              closingTime1: existing.closingTime1 || '15:00',
              openingTime2: existing.openingTime2 || '19:00',
              closingTime2: existing.closingTime2 || '23:00',
              maxCapacity: existing.maxCapacity?.toString() || ''
            }
          }
          return weekConfig[index]
        })
        setWeekConfig(newWeekConfig)
      }

      setBusinessHours(hours)
    } catch (err) {
      console.error(err)
    }
  }

  const fetchExceptions = async () => {
    if (!selectedServiceId) return

    try {
      const response = await apiClient.get(`/businesshours/service/${selectedServiceId}/exceptions`)
      setExceptions(response.data)
    } catch (err) {
      console.error(err)
    }
  }

  const handleSaveWeeklyHours = async () => {
    if (!selectedServiceId) return

    try {
      const payload = weekConfig.map(day => ({
        serviceId: selectedServiceId,
        dayOfWeek: day.dayOfWeek,
        isClosed: day.isClosed,
        openingTime1: day.isClosed ? null : day.openingTime1,
        closingTime1: day.isClosed ? null : day.closingTime1,
        openingTime2: day.isClosed || !day.openingTime2 ? null : day.openingTime2,
        closingTime2: day.isClosed || !day.closingTime2 ? null : day.closingTime2,
        maxCapacity: day.maxCapacity === '' ? null : parseInt(day.maxCapacity)
      }))

      await apiClient.post(`/businesshours/service/${selectedServiceId}/setup-weekly`, payload)
      alert('Orari salvati con successo!')
      fetchBusinessHours()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nel salvataggio')
    }
  }

  const handleDayChange = (dayIndex: number, field: keyof DayConfig, value: any) => {
    const newConfig = [...weekConfig]
    newConfig[dayIndex] = { ...newConfig[dayIndex], [field]: value }
    setWeekConfig(newConfig)
  }

  const handleAddException = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedServiceId) return

    try {
      const payload = {
        serviceId: selectedServiceId,
        date: exceptionFormData.date,
        isClosed: exceptionFormData.isClosed,
        reason: exceptionFormData.reason || null,
        openingTime1: exceptionFormData.isClosed ? null : exceptionFormData.openingTime1,
        closingTime1: exceptionFormData.isClosed ? null : exceptionFormData.closingTime1,
        openingTime2: exceptionFormData.isClosed || !exceptionFormData.openingTime2 ? null : exceptionFormData.openingTime2,
        closingTime2: exceptionFormData.isClosed || !exceptionFormData.closingTime2 ? null : exceptionFormData.closingTime2,
        maxCapacity: exceptionFormData.maxCapacity === '' ? null : parseInt(exceptionFormData.maxCapacity)
      }

      await apiClient.post('/businesshours/exceptions', payload)
      alert('Eccezione aggiunta con successo!')
      setShowExceptionForm(false)
      setExceptionFormData({
        date: '',
        isClosed: false,
        reason: '',
        openingTime1: '12:00',
        closingTime1: '15:00',
        openingTime2: '19:00',
        closingTime2: '23:00',
        maxCapacity: ''
      })
      fetchExceptions()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'aggiunta dell\'eccezione')
    }
  }

  const handleDeleteException = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questa eccezione?')) return

    try {
      await apiClient.delete(`/businesshours/exceptions/${id}`)
      alert('Eccezione eliminata!')
      fetchExceptions()
    } catch (err) {
      alert('Errore nell\'eliminazione')
    }
  }

  const copyDay = (fromDay: number, toDay: number) => {
    const newConfig = [...weekConfig]
    newConfig[toDay] = { ...weekConfig[fromDay], dayOfWeek: toDay }
    setWeekConfig(newConfig)
  }

  if (loading) {
    return <div className="min-h-screen bg-gradient-to-br from-purple-900 via-indigo-900 to-blue-900 text-white p-8">Caricamento...</div>
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-purple-900 via-indigo-900 to-blue-900 text-white p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-4xl font-bold">Orari di Apertura</h1>
          <div className="flex gap-4">
            <Link to="/" className="px-4 py-2 bg-white/10 backdrop-blur-sm border border-white/20 rounded-lg hover:bg-white/20 transition-all">
              Dashboard
            </Link>
            <button onClick={onLogout} className="px-4 py-2 bg-red-500/80 backdrop-blur-sm rounded-lg hover:bg-red-600 transition-all">
              Logout
            </button>
          </div>
        </div>

        {/* Service Selector */}
        <div className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-6 mb-6">
          <label className="block text-lg font-semibold mb-2">Seleziona Servizio</label>
          <select
            className="w-full px-4 py-3 bg-white/5 border border-white/20 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-purple-400"
            value={selectedServiceId || ''}
            onChange={(e) => setSelectedServiceId(parseInt(e.target.value))}
          >
            <option value="">-- Seleziona un servizio --</option>
            {services.map(service => (
              <option key={service.id} value={service.id} className="bg-gray-800">
                {service.name} ({service.bookingModeName})
              </option>
            ))}
          </select>
        </div>

        {selectedServiceId && (
          <>
            {/* Weekly Hours Configuration */}
            <div className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-6 mb-6">
              <div className="flex justify-between items-center mb-6">
                <h2 className="text-2xl font-bold">Orari Settimanali</h2>
                <button
                  onClick={handleSaveWeeklyHours}
                  className="px-6 py-3 bg-gradient-to-r from-purple-500 to-pink-500 rounded-lg hover:from-purple-600 hover:to-pink-600 transition-all font-semibold"
                >
                  Salva Tutti gli Orari
                </button>
              </div>

              <div className="space-y-4">
                {weekConfig.map((day, index) => (
                  <div key={index} className="bg-white/5 border border-white/10 rounded-xl p-4">
                    <div className="flex items-center gap-4 mb-3">
                      <div className="w-32 font-semibold">{DAYS[index]}</div>
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={day.isClosed}
                          onChange={(e) => handleDayChange(index, 'isClosed', e.target.checked)}
                          className="w-5 h-5"
                        />
                        <span>Chiuso</span>
                      </label>
                      {index > 0 && (
                        <button
                          onClick={() => copyDay(index - 1, index)}
                          className="ml-auto px-3 py-1 bg-white/10 rounded-lg hover:bg-white/20 text-sm"
                        >
                          Copia da {DAYS[index - 1]}
                        </button>
                      )}
                    </div>

                    {!day.isClosed && (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {/* Primo turno */}
                        <div className="space-y-2">
                          <div className="text-sm font-semibold text-purple-300">Primo Turno (es. Pranzo)</div>
                          <div className="flex gap-2">
                            <div className="flex-1">
                              <label className="text-xs text-white/60">Apertura</label>
                              <input
                                type="time"
                                value={day.openingTime1}
                                onChange={(e) => handleDayChange(index, 'openingTime1', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                            <div className="flex-1">
                              <label className="text-xs text-white/60">Chiusura</label>
                              <input
                                type="time"
                                value={day.closingTime1}
                                onChange={(e) => handleDayChange(index, 'closingTime1', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                          </div>
                        </div>

                        {/* Secondo turno */}
                        <div className="space-y-2">
                          <div className="text-sm font-semibold text-pink-300">Secondo Turno (es. Cena)</div>
                          <div className="flex gap-2">
                            <div className="flex-1">
                              <label className="text-xs text-white/60">Apertura</label>
                              <input
                                type="time"
                                value={day.openingTime2}
                                onChange={(e) => handleDayChange(index, 'openingTime2', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                            <div className="flex-1">
                              <label className="text-xs text-white/60">Chiusura</label>
                              <input
                                type="time"
                                value={day.closingTime2}
                                onChange={(e) => handleDayChange(index, 'closingTime2', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                          </div>
                        </div>

                        {/* Capacità massima */}
                        <div>
                          <label className="text-xs text-white/60">Capacità Massima (opzionale)</label>
                          <input
                            type="number"
                            placeholder="Illimitata"
                            value={day.maxCapacity}
                            onChange={(e) => handleDayChange(index, 'maxCapacity', e.target.value)}
                            className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                          />
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* Exceptions */}
            <div className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-6">
              <div className="flex justify-between items-center mb-6">
                <h2 className="text-2xl font-bold">Eccezioni (Festività, Ferie, Eventi)</h2>
                <button
                  onClick={() => setShowExceptionForm(!showExceptionForm)}
                  className="px-6 py-3 bg-gradient-to-r from-green-500 to-teal-500 rounded-lg hover:from-green-600 hover:to-teal-600 transition-all font-semibold"
                >
                  {showExceptionForm ? 'Annulla' : '+ Aggiungi Eccezione'}
                </button>
              </div>

              {showExceptionForm && (
                <form onSubmit={handleAddException} className="bg-white/5 border border-white/10 rounded-xl p-4 mb-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                    <div>
                      <label className="block text-sm mb-1">Data</label>
                      <input
                        type="date"
                        required
                        value={exceptionFormData.date}
                        onChange={(e) => setExceptionFormData({ ...exceptionFormData, date: e.target.value })}
                        className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                      />
                    </div>
                    <div>
                      <label className="block text-sm mb-1">Motivo</label>
                      <input
                        type="text"
                        placeholder="Es: Natale, Ferie estive..."
                        value={exceptionFormData.reason}
                        onChange={(e) => setExceptionFormData({ ...exceptionFormData, reason: e.target.value })}
                        className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                      />
                    </div>
                  </div>

                  <div className="mb-4">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={exceptionFormData.isClosed}
                        onChange={(e) => setExceptionFormData({ ...exceptionFormData, isClosed: e.target.checked })}
                        className="w-5 h-5"
                      />
                      <span>Chiuso questo giorno</span>
                    </label>
                  </div>

                  {!exceptionFormData.isClosed && (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                      <div>
                        <label className="block text-sm mb-1">Apertura 1° turno</label>
                        <input
                          type="time"
                          value={exceptionFormData.openingTime1}
                          onChange={(e) => setExceptionFormData({ ...exceptionFormData, openingTime1: e.target.value })}
                          className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                        />
                      </div>
                      <div>
                        <label className="block text-sm mb-1">Chiusura 1° turno</label>
                        <input
                          type="time"
                          value={exceptionFormData.closingTime1}
                          onChange={(e) => setExceptionFormData({ ...exceptionFormData, closingTime1: e.target.value })}
                          className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                        />
                      </div>
                    </div>
                  )}

                  <button
                    type="submit"
                    className="w-full px-4 py-2 bg-gradient-to-r from-green-500 to-teal-500 rounded-lg hover:from-green-600 hover:to-teal-600 transition-all font-semibold"
                  >
                    Salva Eccezione
                  </button>
                </form>
              )}

              {/* Lista eccezioni */}
              <div className="space-y-2">
                {exceptions.length === 0 ? (
                  <p className="text-white/60 text-center py-4">Nessuna eccezione configurata</p>
                ) : (
                  exceptions.map(exc => (
                    <div key={exc.id} className="bg-white/5 border border-white/10 rounded-lg p-3 flex justify-between items-center">
                      <div>
                        <div className="font-semibold">{new Date(exc.date).toLocaleDateString('it-IT')}</div>
                        <div className="text-sm text-white/60">
                          {exc.isClosed ? (
                            <span className="text-red-400">Chiuso</span>
                          ) : (
                            <span>{exc.openingTime1} - {exc.closingTime1}</span>
                          )}
                          {exc.reason && <span className="ml-2">- {exc.reason}</span>}
                        </div>
                      </div>
                      <button
                        onClick={() => handleDeleteException(exc.id)}
                        className="px-3 py-1 bg-red-500/80 rounded-lg hover:bg-red-600 transition-all"
                      >
                        Elimina
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

export default BusinessHours
