import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'

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
  onLogout: () => void
}

/**
 * Pannello amministrazione per approvare merchant
 */
function AdminPanel({ onLogout }: AdminPanelProps) {
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
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Admin Panel</h1>
          <button
            onClick={onLogout}
            className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300"
          >
            Esci
          </button>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="mb-6 flex gap-4">
          <button
            onClick={() => setFilter('pending')}
            className={`px-6 py-2 rounded-lg font-semibold ${
              filter === 'pending'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-700'
            }`}
          >
            In attesa ({merchants.length})
          </button>
          <button
            onClick={() => setFilter('all')}
            className={`px-6 py-2 rounded-lg font-semibold ${
              filter === 'all'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-700'
            }`}
          >
            Tutti i merchant
          </button>
        </div>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12">Caricamento...</div>
        ) : merchants.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <p className="text-xl text-gray-600">
              {filter === 'pending'
                ? 'Nessun merchant in attesa di approvazione'
                : 'Nessun merchant trovato'}
            </p>
          </div>
        ) : (
          <div className="grid gap-6">
            {merchants.map((merchant) => (
              <div key={merchant.id} className="bg-white rounded-lg shadow-lg p-6">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h3 className="text-2xl font-bold text-gray-800 mb-2">
                      {merchant.businessName}
                    </h3>
                    <div className="grid grid-cols-2 gap-4 mb-4">
                      <div>
                        <p className="text-sm text-gray-500">Proprietario</p>
                        <p className="font-semibold">
                          {merchant.user.firstName} {merchant.user.lastName}
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-500">Email</p>
                        <p className="font-semibold">{merchant.user.email}</p>
                      </div>
                      {merchant.user.phoneNumber && (
                        <div>
                          <p className="text-sm text-gray-500">Telefono</p>
                          <p className="font-semibold">{merchant.user.phoneNumber}</p>
                        </div>
                      )}
                      {merchant.vatNumber && (
                        <div>
                          <p className="text-sm text-gray-500">Partita IVA</p>
                          <p className="font-semibold">{merchant.vatNumber}</p>
                        </div>
                      )}
                      {merchant.city && (
                        <div>
                          <p className="text-sm text-gray-500">Citta</p>
                          <p className="font-semibold">{merchant.city}</p>
                        </div>
                      )}
                      <div>
                        <p className="text-sm text-gray-500">Registrato il</p>
                        <p className="font-semibold">
                          {new Date(merchant.createdAt).toLocaleDateString('it-IT')}
                        </p>
                      </div>
                    </div>
                    {merchant.description && (
                      <div className="mb-4">
                        <p className="text-sm text-gray-500">Descrizione</p>
                        <p className="text-gray-700">{merchant.description}</p>
                      </div>
                    )}
                  </div>
                  <div className="ml-6">
                    {merchant.isApproved ? (
                      <span className="px-4 py-2 bg-green-100 text-green-800 rounded-full font-semibold">
                        Approvato
                      </span>
                    ) : (
                      <span className="px-4 py-2 bg-yellow-100 text-yellow-800 rounded-full font-semibold">
                        In attesa
                      </span>
                    )}
                  </div>
                </div>

                {!merchant.isApproved && (
                  <div className="mt-6 flex gap-4">
                    <button
                      onClick={() => handleApprove(merchant.id)}
                      className="flex-1 bg-green-600 text-white py-3 rounded-lg hover:bg-green-700 font-semibold"
                    >
                      Approva Merchant
                    </button>
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="flex-1 bg-red-600 text-white py-3 rounded-lg hover:bg-red-700 font-semibold"
                    >
                      Rifiuta
                    </button>
                  </div>
                )}

                {merchant.isApproved && (
                  <div className="mt-6">
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="bg-red-600 text-white px-6 py-2 rounded-lg hover:bg-red-700"
                    >
                      Disabilita Merchant
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default AdminPanel
