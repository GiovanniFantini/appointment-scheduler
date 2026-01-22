import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface Colleague {
  id: number
  firstName: string
  lastName: string
  fullName: string
  email: string
  phoneNumber?: string
  badgeCode?: string
  role?: string
  isActive: boolean
}

interface ColleaguesProps {
  user: any
  onLogout: () => void
}

export default function Colleagues({ user, onLogout }: ColleaguesProps) {
  const [colleagues, setColleagues] = useState<Colleague[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    fetchColleagues()
  }, [])

  const fetchColleagues = async () => {
    try {
      const response = await apiClient.get('/employeecolleagues')
      setColleagues(response.data)
    } catch (err: any) {
      setError(err.response?.data?.message || 'Errore nel caricamento dei colleghi')
    } finally {
      setLoading(false)
    }
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="I Miei Colleghi">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <div className="glass-card p-8 rounded-2xl border border-white/10">
              <svg className="animate-spin h-12 w-12 text-neon-cyan mx-auto" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <p className="text-gray-300 text-center mt-4">Caricamento colleghi...</p>
            </div>
          </div>
        ) : error ? (
          <div className="glass-card p-6 rounded-2xl border border-red-500/50 bg-red-500/10">
            <div className="flex items-center gap-2 text-red-300">
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <span>{error}</span>
            </div>
          </div>
        ) : colleagues.length === 0 ? (
          <div className="glass-card p-8 rounded-2xl border border-white/10 text-center">
            <svg className="w-16 h-16 text-gray-500 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            <p className="text-gray-300 text-lg">Nessun collega trovato</p>
            <p className="text-gray-400 text-sm mt-2">Sei l'unico dipendente al momento</p>
          </div>
        ) : (
          <>
            <div className="mb-6 glass-card p-4 rounded-xl border border-white/10">
              <p className="text-gray-300">
                Totale colleghi: <span className="text-neon-cyan font-semibold">{colleagues.length}</span>
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {colleagues.map((colleague) => (
                <div
                  key={colleague.id}
                  className="glass-card p-6 rounded-2xl border border-white/10 hover:shadow-glow-cyan transition-all duration-300 transform hover:scale-105"
                >
                  {/* Avatar Circle */}
                  <div className="flex items-center gap-4 mb-4">
                    <div className="w-16 h-16 rounded-full bg-gradient-to-br from-neon-cyan to-neon-blue flex items-center justify-center text-white font-bold text-2xl">
                      {colleague.firstName[0]}{colleague.lastName[0]}
                    </div>
                    <div className="flex-1">
                      <h3 className="text-xl font-bold text-white">{colleague.fullName}</h3>
                      {colleague.role && (
                        <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-neon-green/20 text-neon-green border border-neon-green/30 mt-1">
                          {colleague.role}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Info */}
                  <div className="space-y-3">
                    {colleague.email && (
                      <div className="flex items-center gap-2 text-gray-300">
                        <svg className="w-5 h-5 text-neon-cyan flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                        <span className="text-sm truncate">{colleague.email}</span>
                      </div>
                    )}

                    {colleague.phoneNumber && (
                      <div className="flex items-center gap-2 text-gray-300">
                        <svg className="w-5 h-5 text-neon-green flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                        </svg>
                        <span className="text-sm">{colleague.phoneNumber}</span>
                      </div>
                    )}

                    {colleague.badgeCode && (
                      <div className="flex items-center gap-2 text-gray-300">
                        <svg className="w-5 h-5 text-neon-purple flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V8a2 2 0 00-2-2h-5m-4 0V5a2 2 0 114 0v1m-4 0a2 2 0 104 0m-5 8a2 2 0 100-4 2 2 0 000 4zm0 0c1.306 0 2.417.835 2.83 2M9 14a3.001 3.001 0 00-2.83 2M15 11h3m-3 4h2" />
                        </svg>
                        <span className="text-sm font-mono">{colleague.badgeCode}</span>
                      </div>
                    )}
                  </div>

                  {/* Status Badge */}
                  <div className="mt-4 pt-4 border-t border-white/10">
                    <span className={`inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold ${
                      colleague.isActive 
                        ? 'bg-green-500/20 text-green-400 border border-green-500/30' 
                        : 'bg-gray-500/20 text-gray-400 border border-gray-500/30'
                    }`}>
                      <span className={`w-2 h-2 rounded-full ${colleague.isActive ? 'bg-green-400' : 'bg-gray-400'}`}></span>
                      {colleague.isActive ? 'Attivo' : 'Non attivo'}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
    </AppLayout>
  )
}
