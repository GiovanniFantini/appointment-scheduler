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

interface BusinessHoursShift {
  id?: number
  openingTime: string
  closingTime: string
  label?: string
  maxCapacity?: number
  sortOrder: number
}

interface BusinessHours {
  id: number
  serviceId: number
  dayOfWeek: number
  isClosed: boolean
  shifts: BusinessHoursShift[]
  maxCapacity?: number
}

interface BusinessHoursException {
  id: number
  serviceId: number
  date: string
  isClosed: boolean
  reason?: string
  shifts: BusinessHoursShift[]
  maxCapacity?: number
}

interface DayConfig {
  dayOfWeek: number
  isClosed: boolean
  shifts: BusinessHoursShift[]
  maxCapacity: string
}

interface BusinessHoursProps {
  onLogout: () => void
}

const DAYS = ['Lunedì', 'Martedì', 'Mercoledì', 'Giovedì', 'Venerdì', 'Sabato', 'Domenica']

function BusinessHours({ onLogout }: BusinessHoursProps) {
  const [services, setServices] = useState<Service[]>([])
  const [selectedServiceId, setSelectedServiceId] = useState<number | null>(null)
  const [exceptions, setExceptions] = useState<BusinessHoursException[]>([])
  const [loading, setLoading] = useState(true)
  const [showExceptionForm, setShowExceptionForm] = useState(false)
  const [exceptionFormData, setExceptionFormData] = useState({
    date: '',
    isClosed: false,
    reason: '',
    shifts: [] as BusinessHoursShift[],
    maxCapacity: ''
  })

  // Configurazione per ogni giorno della settimana
  const [weekConfig, setWeekConfig] = useState<DayConfig[]>(
    DAYS.map((_, index) => ({
      dayOfWeek: index,
      isClosed: index === 0, // Lunedì chiuso di default
      shifts: [
        { openingTime: '12:00', closingTime: '15:00', label: 'Pranzo', sortOrder: 0 },
        { openingTime: '19:00', closingTime: '23:00', label: 'Cena', sortOrder: 1 }
      ],
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
              shifts: existing.shifts || [],
              maxCapacity: existing.maxCapacity?.toString() || ''
            }
          }
          return weekConfig[index]
        })
        setWeekConfig(newWeekConfig)
      }
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
        shifts: day.isClosed ? [] : day.shifts,
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

  const handleAddShift = (dayIndex: number) => {
    const newConfig = [...weekConfig]
    const newShift: BusinessHoursShift = {
      openingTime: '09:00',
      closingTime: '18:00',
      label: '',
      sortOrder: newConfig[dayIndex].shifts.length
    }
    newConfig[dayIndex].shifts = [...newConfig[dayIndex].shifts, newShift]
    setWeekConfig(newConfig)
  }

  const handleRemoveShift = (dayIndex: number, shiftIndex: number) => {
    const newConfig = [...weekConfig]
    newConfig[dayIndex].shifts = newConfig[dayIndex].shifts.filter((_, i) => i !== shiftIndex)
    // Renumber sortOrder
    newConfig[dayIndex].shifts.forEach((shift, i) => shift.sortOrder = i)
    setWeekConfig(newConfig)
  }

  const handleShiftChange = (dayIndex: number, shiftIndex: number, field: keyof BusinessHoursShift, value: any) => {
    const newConfig = [...weekConfig]
    const shift = newConfig[dayIndex].shifts[shiftIndex]
    newConfig[dayIndex].shifts[shiftIndex] = { ...shift, [field]: value }
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
        shifts: exceptionFormData.isClosed ? [] : exceptionFormData.shifts,
        maxCapacity: exceptionFormData.maxCapacity === '' ? null : parseInt(exceptionFormData.maxCapacity)
      }

      await apiClient.post('/businesshours/exceptions', payload)
      alert('Eccezione aggiunta con successo!')
      setShowExceptionForm(false)
      setExceptionFormData({
        date: '',
        isClosed: false,
        reason: '',
        shifts: [],
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
    newConfig[toDay] = {
      ...weekConfig[fromDay],
      dayOfWeek: toDay,
      shifts: weekConfig[fromDay].shifts.map(s => ({ ...s })) // Deep copy shifts
    }
    setWeekConfig(newConfig)
  }

  const addExceptionShift = () => {
    setExceptionFormData({
      ...exceptionFormData,
      shifts: [
        ...exceptionFormData.shifts,
        {
          openingTime: '09:00',
          closingTime: '18:00',
          label: '',
          sortOrder: exceptionFormData.shifts.length
        }
      ]
    })
  }

  const removeExceptionShift = (index: number) => {
    const newShifts = exceptionFormData.shifts.filter((_, i) => i !== index)
    newShifts.forEach((shift, i) => shift.sortOrder = i)
    setExceptionFormData({ ...exceptionFormData, shifts: newShifts })
  }

  const updateExceptionShift = (index: number, field: keyof BusinessHoursShift, value: any) => {
    const newShifts = [...exceptionFormData.shifts]
    newShifts[index] = { ...newShifts[index], [field]: value }
    setExceptionFormData({ ...exceptionFormData, shifts: newShifts })
  }

  if (loading) {
    return <div className="min-h-screen bg-gradient-to-br from-purple-900 via-indigo-900 to-blue-900 text-white p-8">Caricamento...</div>
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-purple-900 via-indigo-900 to-blue-900 text-white p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-4xl font-bold">Gestione Disponibilità</h1>
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
                {weekConfig.map((day, dayIndex) => (
                  <div key={dayIndex} className="bg-white/5 border border-white/10 rounded-xl p-4">
                    <div className="flex items-center gap-4 mb-3">
                      <div className="w-32 font-semibold">{DAYS[dayIndex]}</div>
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={day.isClosed}
                          onChange={(e) => handleDayChange(dayIndex, 'isClosed', e.target.checked)}
                          className="w-5 h-5"
                        />
                        <span>Chiuso</span>
                      </label>
                      {dayIndex > 0 && (
                        <button
                          onClick={() => copyDay(dayIndex - 1, dayIndex)}
                          className="ml-auto px-3 py-1 bg-white/10 rounded-lg hover:bg-white/20 text-sm"
                        >
                          Copia da {DAYS[dayIndex - 1]}
                        </button>
                      )}
                    </div>

                    {!day.isClosed && (
                      <div className="space-y-3">
                        {/* Shifts */}
                        {day.shifts.map((shift, shiftIndex) => (
                          <div key={shiftIndex} className="bg-white/5 border border-white/10 rounded-lg p-3">
                            <div className="flex items-center gap-2 mb-2">
                              <input
                                type="text"
                                placeholder="Etichetta (es. Pranzo)"
                                value={shift.label || ''}
                                onChange={(e) => handleShiftChange(dayIndex, shiftIndex, 'label', e.target.value)}
                                className="flex-1 px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white placeholder-white/50"
                              />
                              <button
                                onClick={() => handleRemoveShift(dayIndex, shiftIndex)}
                                className="px-3 py-2 bg-red-500/80 rounded-lg hover:bg-red-600 transition-all"
                                disabled={day.shifts.length === 1}
                              >
                                Rimuovi
                              </button>
                            </div>
                            <div className="grid grid-cols-3 gap-2">
                              <div>
                                <label className="text-xs text-white/60">Apertura</label>
                                <input
                                  type="time"
                                  value={shift.openingTime}
                                  onChange={(e) => handleShiftChange(dayIndex, shiftIndex, 'openingTime', e.target.value)}
                                  className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                                />
                              </div>
                              <div>
                                <label className="text-xs text-white/60">Chiusura</label>
                                <input
                                  type="time"
                                  value={shift.closingTime}
                                  onChange={(e) => handleShiftChange(dayIndex, shiftIndex, 'closingTime', e.target.value)}
                                  className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                                />
                              </div>
                              <div>
                                <label className="text-xs text-white/60">Capacità (opz.)</label>
                                <input
                                  type="number"
                                  placeholder="Auto"
                                  value={shift.maxCapacity || ''}
                                  onChange={(e) => handleShiftChange(dayIndex, shiftIndex, 'maxCapacity', e.target.value ? parseInt(e.target.value) : undefined)}
                                  className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                                />
                              </div>
                            </div>
                          </div>
                        ))}

                        <button
                          onClick={() => handleAddShift(dayIndex)}
                          className="w-full px-3 py-2 bg-green-500/80 rounded-lg hover:bg-green-600 transition-all"
                        >
                          + Aggiungi Turno
                        </button>

                        {/* Default Capacity */}
                        <div>
                          <label className="text-xs text-white/60">Capacità Massima Giornaliera (opzionale)</label>
                          <input
                            type="number"
                            placeholder="Illimitata"
                            value={day.maxCapacity}
                            onChange={(e) => handleDayChange(dayIndex, 'maxCapacity', e.target.value)}
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
                        onChange={(e) => setExceptionFormData({ ...exceptionFormData, isClosed: e.target.checked, shifts: [] })}
                        className="w-5 h-5"
                      />
                      <span>Chiuso questo giorno</span>
                    </label>
                  </div>

                  {!exceptionFormData.isClosed && (
                    <div className="space-y-3 mb-4">
                      {exceptionFormData.shifts.map((shift, index) => (
                        <div key={index} className="bg-white/5 border border-white/10 rounded-lg p-3">
                          <div className="flex items-center gap-2 mb-2">
                            <input
                              type="text"
                              placeholder="Etichetta"
                              value={shift.label || ''}
                              onChange={(e) => updateExceptionShift(index, 'label', e.target.value)}
                              className="flex-1 px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                            />
                            <button
                              type="button"
                              onClick={() => removeExceptionShift(index)}
                              className="px-3 py-2 bg-red-500/80 rounded-lg hover:bg-red-600"
                            >
                              Rimuovi
                            </button>
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <div>
                              <label className="text-xs text-white/60">Apertura</label>
                              <input
                                type="time"
                                value={shift.openingTime}
                                onChange={(e) => updateExceptionShift(index, 'openingTime', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                            <div>
                              <label className="text-xs text-white/60">Chiusura</label>
                              <input
                                type="time"
                                value={shift.closingTime}
                                onChange={(e) => updateExceptionShift(index, 'closingTime', e.target.value)}
                                className="w-full px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
                              />
                            </div>
                          </div>
                        </div>
                      ))}
                      <button
                        type="button"
                        onClick={addExceptionShift}
                        className="w-full px-3 py-2 bg-green-500/80 rounded-lg hover:bg-green-600"
                      >
                        + Aggiungi Turno
                      </button>
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
                    <div key={exc.id} className="bg-white/5 border border-white/10 rounded-lg p-3 flex justify-between items-start">
                      <div>
                        <div className="font-semibold">{new Date(exc.date).toLocaleDateString('it-IT')}</div>
                        <div className="text-sm text-white/60">
                          {exc.isClosed ? (
                            <span className="text-red-400">Chiuso</span>
                          ) : (
                            <div>
                              {exc.shifts.map((shift, i) => (
                                <div key={i}>
                                  {shift.label && `${shift.label}: `}
                                  {shift.openingTime} - {shift.closingTime}
                                </div>
                              ))}
                            </div>
                          )}
                          {exc.reason && <div className="mt-1">- {exc.reason}</div>}
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
