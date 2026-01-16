import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

interface BusinessHours {
  id: number
  merchantId: number
  dayOfWeek: number
  openingTime: string | null
  closingTime: string | null
  secondOpeningTime: string | null
  secondClosingTime: string | null
  isActive: boolean
}

interface BusinessHoursProps {
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

// Template comuni per diversi tipi di business
const BUSINESS_TEMPLATES = {
  pizzeria: {
    name: 'Pizzeria (Lun-Dom 12-15, 19-23, Lunedì chiuso)',
    hours: [
      { dayOfWeek: 0, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' },
      { dayOfWeek: 1, openingTime: null, closingTime: null, secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 2, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' },
      { dayOfWeek: 3, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' },
      { dayOfWeek: 4, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' },
      { dayOfWeek: 5, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' },
      { dayOfWeek: 6, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }
    ]
  },
  parrucchiere: {
    name: 'Parrucchiere (Lun-Sab 9-19, Domenica chiuso)',
    hours: [
      { dayOfWeek: 0, openingTime: null, closingTime: null, secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 1, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 2, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 3, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 4, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 5, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 6, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }
    ]
  },
  palestra: {
    name: 'Palestra (Lun-Ven 6-22, Sab-Dom 8-20)',
    hours: [
      { dayOfWeek: 0, openingTime: '08:00', closingTime: '20:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 1, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 2, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 3, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 4, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 5, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null },
      { dayOfWeek: 6, openingTime: '08:00', closingTime: '20:00', secondOpeningTime: null, secondClosingTime: null }
    ]
  }
}

function BusinessHoursPage({ onLogout }: BusinessHoursProps) {
  const [loading, setLoading] = useState(true)
  const [weekFormData, setWeekFormData] = useState<any[]>(
    DAYS_OF_WEEK.map(day => ({
      dayOfWeek: day.value,
      openingTime: '',
      closingTime: '',
      secondOpeningTime: '',
      secondClosingTime: ''
    }))
  )

  useEffect(() => {
    fetchBusinessHours()
  }, [])

  const fetchBusinessHours = async () => {
    try {
      const res = await apiClient.get('/businesshours/my-business-hours')

      const formData = DAYS_OF_WEEK.map(day => {
        const existing = res.data.find((bh: BusinessHours) => bh.dayOfWeek === day.value)
        if (existing) {
          return {
            dayOfWeek: day.value,
            openingTime: existing.openingTime || '',
            closingTime: existing.closingTime || '',
            secondOpeningTime: existing.secondOpeningTime || '',
            secondClosingTime: existing.secondClosingTime || ''
          }
        }
        return {
          dayOfWeek: day.value,
          openingTime: '',
          closingTime: '',
          secondOpeningTime: '',
          secondClosingTime: ''
        }
      })
      setWeekFormData(formData)
    } catch (err: any) {
      console.error('Errore nel caricamento degli orari:', err)
      if (err.response?.status !== 404) {
        alert('Errore nel caricamento degli orari')
      }
    } finally {
      setLoading(false)
    }
  }

  const applyTemplate = (templateKey: keyof typeof BUSINESS_TEMPLATES) => {
    const template = BUSINESS_TEMPLATES[templateKey]
    setWeekFormData(template.hours)
    alert(`Template "${template.name}" applicato! Clicca "Salva Settimana" per confermare.`)
  }

  const updateDayData = (dayIndex: number, field: string, value: string) => {
    const newData = [...weekFormData]
    newData[dayIndex] = { ...newData[dayIndex], [field]: value }
    setWeekFormData(newData)
  }

  const saveWeek = async () => {
    try {
      const payload = weekFormData.map(day => ({
        dayOfWeek: day.dayOfWeek,
        openingTime: day.openingTime || null,
        closingTime: day.closingTime || null,
        secondOpeningTime: day.secondOpeningTime || null,
        secondClosingTime: day.secondClosingTime || null
      }))

      await apiClient.post('/businesshours/setup-week', payload)
      alert('Orari settimanali salvati con successo!')
      fetchBusinessHours()
    } catch (err: any) {
      console.error('Errore nel salvataggio:', err)
      const errorMessage = err.response?.data?.message || err.response?.data || 'Errore nel salvataggio'
      alert(`Errore: ${errorMessage}`)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Futuristic Header */}
      <header className="glass-card border-b border-white/10 sticky top-0 z-40 backdrop-blur-xl">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold gradient-text flex items-center gap-2">
              <svg className="w-8 h-8 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Orari di Apertura
            </h1>
            <div className="flex items-center gap-3">
              <Link to="/" className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10">
                🏠 Dashboard
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
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-cyan opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-blue opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="container mx-auto px-4 py-12 relative z-10">
        {loading ? (
          <div className="text-center py-12 text-neon-cyan text-xl">Caricamento...</div>
        ) : (
          <div className="glass-card rounded-3xl p-8 mb-6 border border-white/10">
            <h2 className="text-3xl font-bold gradient-text mb-4">🕒 Orari di Apertura</h2>
            <p className="text-gray-300 mb-6">
              Configura gli orari standard del tuo business. Questi orari saranno automaticamente
              applicati a tutti i tuoi servizi, a meno che non imposti disponibilità specifiche.
            </p>

            {/* Templates */}
            <div className="mb-6">
              <h3 className="font-semibold mb-3 text-neon-cyan">Template Veloci:</h3>
              <div className="flex gap-3">
                <button
                  onClick={() => applyTemplate('pizzeria')}
                  className="px-6 py-3 bg-gradient-to-r from-orange-500 to-red-500 text-white rounded-xl hover:shadow-glow-purple transition-all transform hover:scale-105 font-semibold"
                >
                  🍕 Pizzeria
                </button>
                <button
                  onClick={() => applyTemplate('parrucchiere')}
                  className="px-6 py-3 bg-gradient-to-r from-pink-500 to-purple-500 text-white rounded-xl hover:shadow-glow-purple transition-all transform hover:scale-105 font-semibold"
                >
                  ✂️ Parrucchiere
                </button>
                <button
                  onClick={() => applyTemplate('palestra')}
                  className="px-6 py-3 bg-gradient-to-r from-neon-blue to-neon-cyan text-white rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                >
                  💪 Palestra
                </button>
              </div>
            </div>

            {/* Weekly Form */}
            <div className="space-y-4">
              {DAYS_OF_WEEK.map((day, index) => (
                <div key={day.value} className="glass-card-dark rounded-xl p-4 border border-white/10">
                  <div className="flex items-center gap-4">
                    <div className="w-32 font-semibold text-neon-cyan">{day.label}</div>

                    <div className="flex-1 grid grid-cols-4 gap-2">
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">Apertura</label>
                        <input
                          type="time"
                          value={weekFormData[index].openingTime}
                          onChange={(e) => updateDayData(index, 'openingTime', e.target.value)}
                          className="w-full px-3 py-2 bg-white/5 text-white rounded-lg border border-white/10 focus:border-neon-cyan focus:outline-none"
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">Chiusura</label>
                        <input
                          type="time"
                          value={weekFormData[index].closingTime}
                          onChange={(e) => updateDayData(index, 'closingTime', e.target.value)}
                          className="w-full px-3 py-2 bg-white/5 text-white rounded-lg border border-white/10 focus:border-neon-cyan focus:outline-none"
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">2° Apertura</label>
                        <input
                          type="time"
                          value={weekFormData[index].secondOpeningTime}
                          onChange={(e) => updateDayData(index, 'secondOpeningTime', e.target.value)}
                          className="w-full px-3 py-2 bg-white/5 text-white rounded-lg border border-white/10 focus:border-neon-cyan focus:outline-none"
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-400 mb-1">2° Chiusura</label>
                        <input
                          type="time"
                          value={weekFormData[index].secondClosingTime}
                          onChange={(e) => updateDayData(index, 'secondClosingTime', e.target.value)}
                          className="w-full px-3 py-2 bg-white/5 text-white rounded-lg border border-white/10 focus:border-neon-cyan focus:outline-none"
                        />
                      </div>
                    </div>

                    <button
                      onClick={() => {
                        const newData = [...weekFormData]
                        newData[index] = {
                          dayOfWeek: day.value,
                          openingTime: '',
                          closingTime: '',
                          secondOpeningTime: '',
                          secondClosingTime: ''
                        }
                        setWeekFormData(newData)
                      }}
                      className="px-4 py-2 bg-red-500/20 text-red-400 rounded-xl hover:bg-red-500/30 transition-all font-semibold"
                    >
                      Chiuso
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="mt-8">
              <button
                onClick={saveWeek}
                className="w-full px-6 py-4 bg-gradient-to-r from-neon-cyan to-neon-blue text-white rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-bold text-lg"
              >
                💾 Salva Settimana
              </button>
            </div>

            <div className="mt-6 glass-card-dark rounded-xl p-6 border border-neon-blue/30">
              <h4 className="font-semibold mb-3 text-neon-blue flex items-center gap-2">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Suggerimenti
              </h4>
              <ul className="text-sm text-gray-300 space-y-2">
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan">•</span>
                  <span>Lascia vuoti i campi per indicare un giorno di chiusura</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan">•</span>
                  <span>Usa "2° Apertura" e "2° Chiusura" per turni spezzati (es. pranzo e cena)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan">•</span>
                  <span>I template sono preconfigurati per facilitare la configurazione</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan">•</span>
                  <span>Questi orari si applicano automaticamente a tutti i servizi</span>
                </li>
              </ul>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default BusinessHoursPage
