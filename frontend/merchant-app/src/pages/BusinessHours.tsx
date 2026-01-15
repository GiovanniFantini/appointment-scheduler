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
      { dayOfWeek: 0, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }, // Dom
      { dayOfWeek: 1, openingTime: null, closingTime: null, secondOpeningTime: null, secondClosingTime: null }, // Lun (chiuso)
      { dayOfWeek: 2, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }, // Mar
      { dayOfWeek: 3, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }, // Mer
      { dayOfWeek: 4, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }, // Gio
      { dayOfWeek: 5, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }, // Ven
      { dayOfWeek: 6, openingTime: '12:00', closingTime: '15:00', secondOpeningTime: '19:00', secondClosingTime: '23:00' }  // Sab
    ]
  },
  parrucchiere: {
    name: 'Parrucchiere (Lun-Sab 9-19, Domenica chiuso)',
    hours: [
      { dayOfWeek: 0, openingTime: null, closingTime: null, secondOpeningTime: null, secondClosingTime: null }, // Dom (chiuso)
      { dayOfWeek: 1, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }, // Lun
      { dayOfWeek: 2, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }, // Mar
      { dayOfWeek: 3, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }, // Mer
      { dayOfWeek: 4, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }, // Gio
      { dayOfWeek: 5, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }, // Ven
      { dayOfWeek: 6, openingTime: '09:00', closingTime: '19:00', secondOpeningTime: null, secondClosingTime: null }  // Sab
    ]
  },
  palestra: {
    name: 'Palestra (Lun-Ven 6-22, Sab-Dom 8-20)',
    hours: [
      { dayOfWeek: 0, openingTime: '08:00', closingTime: '20:00', secondOpeningTime: null, secondClosingTime: null }, // Dom
      { dayOfWeek: 1, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null }, // Lun
      { dayOfWeek: 2, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null }, // Mar
      { dayOfWeek: 3, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null }, // Mer
      { dayOfWeek: 4, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null }, // Gio
      { dayOfWeek: 5, openingTime: '06:00', closingTime: '22:00', secondOpeningTime: null, secondClosingTime: null }, // Ven
      { dayOfWeek: 6, openingTime: '08:00', closingTime: '20:00', secondOpeningTime: null, secondClosingTime: null }  // Sab
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

      // Popola il form con i dati esistenti
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

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-100">
        <nav className="bg-indigo-600 text-white p-4">
          <div className="container mx-auto flex justify-between items-center">
            <h1 className="text-2xl font-bold">Merchant Dashboard</h1>
          </div>
        </nav>
        <div className="container mx-auto p-4">
          <div className="text-center py-8">Caricamento...</div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-indigo-600 text-white p-4">
        <div className="container mx-auto flex justify-between items-center">
          <h1 className="text-2xl font-bold">Merchant Dashboard</h1>
          <div className="space-x-4">
            <Link to="/dashboard" className="hover:underline">Dashboard</Link>
            <Link to="/services" className="hover:underline">Servizi</Link>
            <Link to="/business-hours" className="hover:underline font-semibold">Orari</Link>
            <Link to="/closures" className="hover:underline">Chiusure</Link>
            <Link to="/availabilities" className="hover:underline">Disponibilità</Link>
            <Link to="/employees" className="hover:underline">Dipendenti</Link>
            <Link to="/bookings" className="hover:underline">Prenotazioni</Link>
            <button onClick={onLogout} className="hover:underline">Logout</button>
          </div>
        </div>
      </nav>

      <div className="container mx-auto p-4">
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <h2 className="text-2xl font-bold mb-4">🕒 Orari di Apertura</h2>
          <p className="text-gray-600 mb-4">
            Configura gli orari standard del tuo business. Questi orari saranno automaticamente
            applicati a tutti i tuoi servizi, a meno che non imposti disponibilità specifiche.
          </p>

          {/* Templates */}
          <div className="mb-6">
            <h3 className="font-semibold mb-2">Template Veloci:</h3>
            <div className="flex gap-2">
              <button
                onClick={() => applyTemplate('pizzeria')}
                className="px-4 py-2 bg-orange-500 text-white rounded hover:bg-orange-600"
              >
                🍕 Pizzeria
              </button>
              <button
                onClick={() => applyTemplate('parrucchiere')}
                className="px-4 py-2 bg-pink-500 text-white rounded hover:bg-pink-600"
              >
                ✂️ Parrucchiere
              </button>
              <button
                onClick={() => applyTemplate('palestra')}
                className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
              >
                💪 Palestra
              </button>
            </div>
          </div>

          {/* Weekly Form */}
          <div className="space-y-4">
            {DAYS_OF_WEEK.map((day, index) => (
              <div key={day.value} className="border rounded p-4">
                <div className="flex items-center gap-4">
                  <div className="w-32 font-semibold">{day.label}</div>

                  <div className="flex-1 grid grid-cols-4 gap-2">
                    <div>
                      <label className="block text-xs text-gray-600">Apertura</label>
                      <input
                        type="time"
                        value={weekFormData[index].openingTime}
                        onChange={(e) => updateDayData(index, 'openingTime', e.target.value)}
                        className="w-full px-2 py-1 border rounded"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-600">Chiusura</label>
                      <input
                        type="time"
                        value={weekFormData[index].closingTime}
                        onChange={(e) => updateDayData(index, 'closingTime', e.target.value)}
                        className="w-full px-2 py-1 border rounded"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-600">2° Apertura</label>
                      <input
                        type="time"
                        value={weekFormData[index].secondOpeningTime}
                        onChange={(e) => updateDayData(index, 'secondOpeningTime', e.target.value)}
                        className="w-full px-2 py-1 border rounded"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-600">2° Chiusura</label>
                      <input
                        type="time"
                        value={weekFormData[index].secondClosingTime}
                        onChange={(e) => updateDayData(index, 'secondClosingTime', e.target.value)}
                        className="w-full px-2 py-1 border rounded"
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
                    className="px-3 py-1 text-sm bg-red-500 text-white rounded hover:bg-red-600"
                  >
                    Chiuso
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-6">
            <button
              onClick={saveWeek}
              className="w-full px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
            >
              💾 Salva Settimana
            </button>
          </div>

          <div className="mt-4 p-4 bg-blue-50 rounded">
            <h4 className="font-semibold mb-2">ℹ️ Suggerimenti:</h4>
            <ul className="text-sm text-gray-700 list-disc list-inside space-y-1">
              <li>Lascia vuoti i campi per indicare un giorno di chiusura</li>
              <li>Usa "2° Apertura" e "2° Chiusura" per turni spezzati (es. pranzo e cena)</li>
              <li>I template sono preconfigurati per facilitare la configurazione</li>
              <li>Questi orari si applicano automaticamente a tutti i servizi</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}

export default BusinessHoursPage
