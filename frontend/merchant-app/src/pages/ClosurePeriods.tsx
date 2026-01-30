import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface ClosurePeriod {
  id: number
  merchantId: number
  startDate: string
  endDate: string
  reason: string
  description: string | null
  isActive: boolean
  createdAt: string
}

interface ClosurePeriodsProps {
  user: any
  onLogout: () => void
}

/** Format a local Date as YYYY-MM-DD without UTC conversion */
const toLocalDateString = (date: Date): string => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/** Parse a date string (ISO or YYYY-MM-DD) to a local YYYY-MM-DD string */
const toDateOnly = (dateString: string): string => {
  return dateString.split('T')[0]
}

function ClosurePeriodsPage({ user, onLogout }: ClosurePeriodsProps) {
  const [closurePeriods, setClosurePeriods] = useState<ClosurePeriod[]>([])
  const [showForm, setShowForm] = useState(false)
  const [formData, setFormData] = useState({
    startDate: '',
    endDate: '',
    reason: '',
    description: ''
  })
  const [calendarDate, setCalendarDate] = useState(new Date())

  useEffect(() => {
    fetchClosurePeriods()
  }, [])

  const fetchClosurePeriods = async () => {
    try {
      const res = await apiClient.get('/closureperiod/my-closures')
      setClosurePeriods(res.data)
    } catch (err: any) {
      console.error('Errore nel caricamento delle chiusure:', err)
      if (err.response?.status !== 404) {
        alert('Errore nel caricamento delle chiusure')
      }
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!formData.startDate || !formData.endDate || !formData.reason) {
      alert('Compila tutti i campi obbligatori')
      return
    }

    if (formData.startDate > formData.endDate) {
      alert('La data di fine deve essere uguale o successiva alla data di inizio')
      return
    }

    try {
      await apiClient.post('/closureperiod', {
        startDate: formData.startDate,
        endDate: formData.endDate,
        reason: formData.reason,
        description: formData.description || null
      })

      alert('Chiusura creata con successo!')
      setShowForm(false)
      setFormData({ startDate: '', endDate: '', reason: '', description: '' })
      fetchClosurePeriods()
    } catch (err: any) {
      console.error('Errore nella creazione:', err)
      const errorMessage = err.response?.data?.message || err.response?.data || 'Errore nella creazione'
      alert(`Errore: ${errorMessage}`)
    }
  }

  const deleteClosure = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questa chiusura?')) return

    try {
      await apiClient.delete(`/closureperiod/${id}`)
      alert('Chiusura eliminata!')
      fetchClosurePeriods()
    } catch (err: any) {
      console.error('Errore nell\'eliminazione:', err)
      alert('Errore nell\'eliminazione')
    }
  }

  const formatDate = (dateString: string) => {
    // Parse only the date part to avoid timezone shift
    const parts = toDateOnly(dateString).split('-')
    const date = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]))
    return date.toLocaleDateString('it-IT', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    })
  }

  const getDaysCount = (start: string, end: string) => {
    const startParts = toDateOnly(start).split('-')
    const endParts = toDateOnly(end).split('-')
    const startDate = new Date(Number(startParts[0]), Number(startParts[1]) - 1, Number(startParts[2]))
    const endDate = new Date(Number(endParts[0]), Number(endParts[1]) - 1, Number(endParts[2]))
    const diffTime = Math.abs(endDate.getTime() - startDate.getTime())
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1
    return diffDays
  }

  const isUpcoming = (startDate: string) => {
    const todayStr = toLocalDateString(new Date())
    return toDateOnly(startDate) > todayStr
  }

  const isCurrent = (startDate: string, endDate: string) => {
    const todayStr = toLocalDateString(new Date())
    return toDateOnly(startDate) <= todayStr && toDateOnly(endDate) >= todayStr
  }

  const isPast = (endDate: string) => {
    const todayStr = toLocalDateString(new Date())
    return toDateOnly(endDate) < todayStr
  }

  // Calendar helpers
  const getCalendarDays = () => {
    const year = calendarDate.getFullYear()
    const month = calendarDate.getMonth()

    // First day of month
    const firstDay = new Date(year, month, 1)
    // Last day of month
    const lastDay = new Date(year, month + 1, 0)

    // Start from Monday before the first day
    const startDow = firstDay.getDay() // 0=Sun, 1=Mon, ..., 6=Sat
    const mondayOffset = startDow === 0 ? 6 : startDow - 1
    const start = new Date(year, month, 1 - mondayOffset)

    // End on Sunday after the last day
    const endDow = lastDay.getDay()
    const sundayOffset = endDow === 0 ? 0 : 7 - endDow
    const end = new Date(year, month, lastDay.getDate() + sundayOffset)

    const days: Date[] = []
    const current = new Date(start)
    while (current <= end) {
      days.push(new Date(current))
      current.setDate(current.getDate() + 1)
    }
    return days
  }

  const isDateInClosure = (dateStr: string) => {
    return closurePeriods.some(cp => {
      const start = toDateOnly(cp.startDate)
      const end = toDateOnly(cp.endDate)
      return dateStr >= start && dateStr <= end
    })
  }

  const isDateInSelection = (dateStr: string) => {
    if (!formData.startDate) return false
    if (!formData.endDate) return dateStr === formData.startDate
    return dateStr >= formData.startDate && dateStr <= formData.endDate
  }

  const isSelectionStart = (dateStr: string) => {
    return formData.startDate === dateStr
  }

  const isSelectionEnd = (dateStr: string) => {
    return formData.endDate === dateStr
  }

  const handleCalendarDayClick = (dateStr: string) => {
    if (!showForm) return

    if (!formData.startDate || (formData.startDate && formData.endDate)) {
      // Start new selection
      setFormData({ ...formData, startDate: dateStr, endDate: '' })
    } else {
      // Complete selection
      if (dateStr < formData.startDate) {
        setFormData({ ...formData, startDate: dateStr, endDate: formData.startDate })
      } else {
        setFormData({ ...formData, endDate: dateStr })
      }
    }
  }

  const handlePrevMonth = () => {
    const newDate = new Date(calendarDate)
    newDate.setMonth(newDate.getMonth() - 1)
    setCalendarDate(newDate)
  }

  const handleNextMonth = () => {
    const newDate = new Date(calendarDate)
    newDate.setMonth(newDate.getMonth() + 1)
    setCalendarDate(newDate)
  }

  const handleToday = () => {
    setCalendarDate(new Date())
  }

  const getCalendarLabel = () => {
    return calendarDate.toLocaleDateString('it-IT', { month: 'long', year: 'numeric' })
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Chiusure">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => setShowForm(!showForm)}
            className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            {showForm ? '❌ Annulla' : '➕ Nuova Chiusura'}
          </button>
        </div>

        {/* Calendar */}
        <div className="glass-card rounded-3xl shadow-lg p-6 mb-6 border border-white/10">
          {/* Calendar Navigation */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <button
                onClick={handlePrevMonth}
                className="glass-card-dark px-4 py-2 text-neon-cyan rounded-xl hover:border-neon-cyan/50 transition-all border border-white/10"
              >
                ← Precedente
              </button>
              <button
                onClick={handleToday}
                className="glass-card-dark px-4 py-2 text-neon-cyan rounded-xl hover:border-neon-cyan/50 transition-all border border-white/10"
              >
                Oggi
              </button>
              <button
                onClick={handleNextMonth}
                className="glass-card-dark px-4 py-2 text-neon-cyan rounded-xl hover:border-neon-cyan/50 transition-all border border-white/10"
              >
                Successivo →
              </button>
            </div>
            <span className="text-xl font-semibold text-neon-cyan capitalize">
              {getCalendarLabel()}
            </span>
          </div>

          {showForm && (
            <p className="text-sm text-neon-pink mb-3">
              Clicca su un giorno per selezionare la data di inizio, poi clicca su un altro giorno per la data di fine.
            </p>
          )}

          {/* Calendar Grid */}
          <div className="grid grid-cols-7 gap-px bg-white/5 rounded-xl overflow-hidden">
            {/* Day headers */}
            {['Lun', 'Mar', 'Mer', 'Gio', 'Ven', 'Sab', 'Dom'].map((dayName) => (
              <div key={dayName} className="glass-card-dark p-2 text-center text-sm font-bold text-neon-cyan">
                {dayName}
              </div>
            ))}
            {/* Day cells */}
            {getCalendarDays().map((day, index) => {
              const dateStr = toLocalDateString(day)
              const isCurrentMonth = day.getMonth() === calendarDate.getMonth()
              const isToday = dateStr === toLocalDateString(new Date())
              const inClosure = isDateInClosure(dateStr)
              const inSelection = isDateInSelection(dateStr)
              const isStart = isSelectionStart(dateStr)
              const isEnd = isSelectionEnd(dateStr)

              return (
                <div
                  key={index}
                  onClick={() => handleCalendarDayClick(dateStr)}
                  className={`glass-card-dark min-h-[3rem] p-2 flex items-center justify-center relative transition-all
                    ${!isCurrentMonth ? 'opacity-30' : ''}
                    ${isToday ? 'ring-2 ring-neon-cyan' : ''}
                    ${showForm ? 'cursor-pointer hover:bg-white/10' : ''}
                    ${inClosure && !inSelection ? 'bg-red-500/20' : ''}
                    ${inSelection ? 'bg-neon-pink/30' : ''}
                    ${isStart || isEnd ? 'bg-neon-pink/50' : ''}
                  `}
                >
                  <span className={`text-sm font-semibold
                    ${isToday ? 'text-neon-cyan' : ''}
                    ${inClosure && !inSelection ? 'text-red-400' : ''}
                    ${inSelection ? 'text-white' : ''}
                    ${!isToday && !inClosure && !inSelection ? 'text-gray-300' : ''}
                  `}>
                    {day.getDate()}
                  </span>
                  {inClosure && !inSelection && (
                    <span className="absolute bottom-1 left-1/2 transform -translate-x-1/2 w-1.5 h-1.5 bg-red-400 rounded-full"></span>
                  )}
                </div>
              )
            })}
          </div>

          {/* Legend */}
          <div className="flex items-center gap-6 mt-4 text-sm text-gray-400">
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 bg-red-500/40 rounded-sm border border-red-400/50"></span>
              <span>Giorni di chiusura</span>
            </div>
            {showForm && (
              <div className="flex items-center gap-2">
                <span className="w-3 h-3 bg-neon-pink/50 rounded-sm border border-neon-pink/50"></span>
                <span>Selezione corrente</span>
              </div>
            )}
          </div>
        </div>

        {/* Form */}
        {showForm && (
          <div className="glass-card rounded-3xl shadow-lg p-6 mb-6 border border-white/10 animate-scale-in">
            <h2 className="text-2xl font-bold gradient-text mb-6">✨ Nuova Chiusura</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Data Inizio *</label>
                  <input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Data Fine *</label>
                  <input
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-bold text-white/90">Motivo *</label>
                <input
                  type="text"
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  placeholder="Es: Ferie estive, Natale, Ristrutturazione..."
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-bold text-white/90">Descrizione (opzionale)</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                  rows={2}
                  placeholder="Dettagli aggiuntivi..."
                />
              </div>

              <button
                type="submit"
                className="w-full bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
              >
                💾 Crea Chiusura
              </button>
            </form>
          </div>
        )}

        {/* Lista Chiusure */}
        {closurePeriods.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
            <p className="text-2xl text-gray-400 mb-2">Nessuna chiusura programmata</p>
            <p className="text-gray-500">Clicca su "Nuova Chiusura" per aggiungerne una</p>
          </div>
        ) : (
          <div className="space-y-6">
            {closurePeriods
              .sort((a, b) => toDateOnly(b.startDate).localeCompare(toDateOnly(a.startDate)))
              .map((closure, index) => {
                const upcoming = isUpcoming(closure.startDate)
                const current = isCurrent(closure.startDate, closure.endDate)
                const past = isPast(closure.endDate)

                return (
                  <div
                    key={closure.id}
                    className={`glass-card rounded-3xl shadow-lg p-6 transition-all hover:shadow-glow-pink animate-slide-up ${
                      current ? 'border border-red-500/50' :
                      upcoming ? 'border border-neon-pink/50' :
                      'border border-white/10'
                    }`}
                    style={{ animationDelay: `${index * 0.1}s` }}
                  >
                    <div className="flex justify-between items-start">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-3">
                          <h3 className="text-2xl font-bold gradient-text">{closure.reason}</h3>
                          {current && (
                            <span className="px-3 py-1.5 bg-gradient-to-r from-red-500/20 to-neon-pink/20 text-red-400 text-xs font-bold rounded-xl border border-red-500/50">
                              🔴 IN CORSO
                            </span>
                          )}
                          {upcoming && (
                            <span className="px-3 py-1.5 bg-gradient-to-r from-neon-pink/20 to-neon-purple/20 text-neon-pink text-xs font-bold rounded-xl border border-neon-pink/50">
                              📌 PROGRAMMATA
                            </span>
                          )}
                          {past && (
                            <span className="px-3 py-1.5 bg-gray-500/20 text-gray-400 text-xs font-bold rounded-xl border border-gray-500/30">
                              ⏸️ PASSATA
                            </span>
                          )}
                        </div>

                        <div className="space-y-3">
                          <div className="glass-card-dark p-3 rounded-xl border border-neon-cyan/20">
                            <p className="text-sm text-gray-400 mb-1">📅 Periodo</p>
                            <p className="text-neon-cyan font-semibold">
                              Dal {formatDate(closure.startDate)} al {formatDate(closure.endDate)}
                              <span className="ml-2 text-neon-blue">({getDaysCount(closure.startDate, closure.endDate)} giorni)</span>
                            </p>
                          </div>
                          {closure.description && (
                            <div className="glass-card-dark p-3 rounded-xl border border-white/5">
                              <p className="text-xs text-gray-500 mb-1">💬 Descrizione</p>
                              <p className="text-gray-300">{closure.description}</p>
                            </div>
                          )}
                        </div>
                      </div>

                      <button
                        onClick={() => deleteClosure(closure.id)}
                        className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105 ml-4"
                      >
                        🗑️ Elimina
                      </button>
                    </div>
                  </div>
                )
              })}
          </div>
        )}

        <div className="mt-8 glass-card rounded-3xl p-6 border border-neon-blue/30">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-xl bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 border border-neon-blue/30">
              <svg className="w-5 h-5 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h4 className="font-semibold text-neon-blue">Informazioni</h4>
          </div>
          <ul className="text-sm text-gray-300 space-y-2">
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Le chiusure straordinarie hanno priorità sugli orari standard</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Durante questi periodi non sarà possibile effettuare prenotazioni</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Puoi creare una lista indefinita di chiusure future</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-neon-cyan">•</span>
              <span>Esempi: ferie estive, festività, ristrutturazioni, eventi speciali</span>
            </li>
          </ul>
        </div>
      </div>
    </AppLayout>
  )
}

export default ClosurePeriodsPage
