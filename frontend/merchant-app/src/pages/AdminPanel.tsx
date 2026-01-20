import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface Merchant {
  id: number
  businessName: string
  description: string | null
  vatNumber: string | null
  city: string | null
  isApproved: boolean
  createdAt: string
  user: {
    email: string
    firstName: string
    lastName: string
    phoneNumber: string | null
  }
}

interface AdminPanelProps {
  user: any
  onLogout: () => void
}

/**
 * Pannello amministrazione per approvare merchant
 */
function AdminPanel({ user, onLogout }: AdminPanelProps) {
  const [merchants, setMerchants] = useState<Merchant[]>([])
  const [filter, setFilter] = useState<'all' | 'pending'>('pending')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    fetchMerchants()
  }, [filter])

  const fetchMerchants = async () => {
    setLoading(true)
    setError('')

    try {
      const endpoint = filter === 'pending'
        ? '/merchants/pending'
        : '/merchants'

      const response = await apiClient.get(endpoint)

      setMerchants(response.data)
    } catch (err: any) {
      setError(err.response?.data?.message || 'Errore nel caricamento dei merchant')
    } finally {
      setLoading(false)
    }
  }

  const handleApprove = async (id: number) => {
    try {
      await apiClient.post(`/merchants/${id}/approve`, {})

      setMerchants(merchants.filter(m => m.id !== id))
      alert('Merchant approvato con successo')
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'approvazione')
    }
  }

  const handleReject = async (id: number) => {
    if (!confirm('Sei sicuro di voler rifiutare questo merchant?')) return

    try {
      await apiClient.post(`/merchants/${id}/reject`, {})

      fetchMerchants()
      alert('Merchant rifiutato')
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nel rifiuto')
    }
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Admin Panel">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6 flex gap-4">
          <button
            onClick={() => setFilter('pending')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all transform hover:scale-105 ${
              filter === 'pending'
                ? 'bg-gradient-to-r from-neon-purple to-neon-pink text-white shadow-glow-purple'
                : 'glass-card-dark text-gray-300 border border-white/10'
            }`}
          >
            ⏳ In attesa ({merchants.length})
          </button>
          <button
            onClick={() => setFilter('all')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all transform hover:scale-105 ${
              filter === 'all'
                ? 'bg-gradient-to-r from-neon-purple to-neon-pink text-white shadow-glow-purple'
                : 'glass-card-dark text-gray-300 border border-white/10'
            }`}
          >
            📊 Tutti i merchant
          </button>
        </div>

        {error && (
          <div className="bg-red-500/10 border border-red-500/50 text-red-400 px-4 py-3 rounded-xl mb-4 backdrop-blur-sm animate-slide-down">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {error}
            </div>
          </div>
        )}

        {loading ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
            <div className="flex items-center justify-center gap-3 text-neon-purple">
              <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span className="text-xl font-semibold">Caricamento...</span>
            </div>
          </div>
        ) : merchants.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
            <p className="text-2xl text-gray-400">
              {filter === 'pending'
                ? 'Nessun merchant in attesa di approvazione'
                : 'Nessun merchant trovato'}
            </p>
          </div>
        ) : (
          <div className="grid gap-6">
            {merchants.map((merchant, index) => (
              <div key={merchant.id} className="glass-card rounded-3xl shadow-lg p-6 border border-white/10 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h3 className="text-3xl font-bold gradient-text mb-3">
                      {merchant.businessName}
                    </h3>
                    <div className="grid grid-cols-2 gap-4 mb-4">
                      <div className="glass-card-dark p-3 rounded-xl border border-neon-cyan/20">
                        <p className="text-xs text-gray-500 mb-1">👤 Proprietario</p>
                        <p className="font-semibold text-neon-cyan">
                          {merchant.user.firstName} {merchant.user.lastName}
                        </p>
                      </div>
                      <div className="glass-card-dark p-3 rounded-xl border border-neon-blue/20">
                        <p className="text-xs text-gray-500 mb-1">📧 Email</p>
                        <p className="font-semibold text-neon-blue">{merchant.user.email}</p>
                      </div>
                      {merchant.user.phoneNumber && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-purple/20">
                          <p className="text-xs text-gray-500 mb-1">📱 Telefono</p>
                          <p className="font-semibold text-neon-purple">{merchant.user.phoneNumber}</p>
                        </div>
                      )}
                      {merchant.vatNumber && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-green/20">
                          <p className="text-xs text-gray-500 mb-1">🏢 Partita IVA</p>
                          <p className="font-semibold text-neon-green font-mono">{merchant.vatNumber}</p>
                        </div>
                      )}
                      {merchant.city && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-pink/20">
                          <p className="text-xs text-gray-500 mb-1">📍 Città</p>
                          <p className="font-semibold text-neon-pink">{merchant.city}</p>
                        </div>
                      )}
                      <div className="glass-card-dark p-3 rounded-xl border border-neon-cyan/20">
                        <p className="text-xs text-gray-500 mb-1">📅 Registrato il</p>
                        <p className="font-semibold text-neon-cyan">
                          {new Date(merchant.createdAt).toLocaleDateString('it-IT')}
                        </p>
                      </div>
                    </div>
                    {merchant.description && (
                      <div className="glass-card-dark p-4 rounded-xl mb-4 border border-white/5">
                        <p className="text-xs text-gray-500 mb-2">📝 Descrizione</p>
                        <p className="text-gray-300">{merchant.description}</p>
                      </div>
                    )}
                  </div>
                  <div className="ml-6">
                    {merchant.isApproved ? (
                      <span className="px-4 py-2 bg-neon-green/20 text-neon-green rounded-xl font-semibold border border-neon-green/30">
                        ✅ Approvato
                      </span>
                    ) : (
                      <span className="px-4 py-2 bg-yellow-500/20 text-yellow-400 rounded-xl font-semibold border border-yellow-500/30">
                        ⏳ In attesa
                      </span>
                    )}
                  </div>
                </div>

                {!merchant.isApproved && (
                  <div className="mt-6 pt-6 border-t border-white/10 flex gap-4">
                    <button
                      onClick={() => handleApprove(merchant.id)}
                      className="flex-1 bg-gradient-to-r from-neon-green to-neon-cyan text-white py-3 rounded-xl hover:shadow-glow-cyan font-semibold transition-all transform hover:scale-105"
                    >
                      ✅ Approva Merchant
                    </button>
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="flex-1 glass-card-dark py-3 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                    >
                      ❌ Rifiuta
                    </button>
                  </div>
                )}

                {merchant.isApproved && (
                  <div className="mt-6 pt-6 border-t border-white/10">
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="glass-card-dark px-6 py-3 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                    >
                      🚫 Disabilita Merchant
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </AppLayout>
  )
}

export default AdminPanel
