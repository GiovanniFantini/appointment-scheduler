import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
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

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  roles: string[]
  isAdmin: boolean
  isConsumer: boolean
  isMerchant: boolean
  isEmployee: boolean
  merchantId?: number
}

interface MerchantApprovalProps {
  user: User
  onLogout: () => void
}

function MerchantApproval({ user, onLogout }: MerchantApprovalProps) {
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
          <div>
            <h1 className="text-2xl font-bold text-gray-800">Gestione Merchant</h1>
            <p className="text-sm text-gray-600">
              Benvenuto, {user.firstName} {user.lastName}
            </p>
          </div>
          <div className="flex gap-4">
            <Link
              to="/"
              className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300"
            >
              Dashboard
            </Link>
            <button
              onClick={onLogout}
              className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700"
            >
              Esci
            </button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="mb-6 flex gap-4">
          <button
            onClick={() => setFilter('pending')}
            className={`px-6 py-2 rounded-lg font-semibold transition-colors ${
              filter === 'pending'
                ? 'bg-purple-600 text-white'
                : 'bg-white text-gray-700 hover:bg-gray-100'
            }`}
          >
            In attesa
          </button>
          <button
            onClick={() => setFilter('all')}
            className={`px-6 py-2 rounded-lg font-semibold transition-colors ${
              filter === 'all'
                ? 'bg-purple-600 text-white'
                : 'bg-white text-gray-700 hover:bg-gray-100'
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
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
            <p className="mt-4 text-gray-600">Caricamento...</p>
          </div>
        ) : merchants.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <svg
              className="mx-auto h-16 w-16 text-gray-400 mb-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
              />
            </svg>
            <p className="text-xl text-gray-600">
              {filter === 'pending'
                ? 'Nessun merchant in attesa di approvazione'
                : 'Nessun merchant trovato'}
            </p>
          </div>
        ) : (
          <div className="grid gap-6">
            {merchants.map((merchant) => (
              <div key={merchant.id} className="bg-white rounded-lg shadow-lg p-6 hover:shadow-xl transition-shadow">
                <div className="flex justify-between items-start mb-4">
                  <div className="flex-1">
                    <h3 className="text-2xl font-bold text-gray-800 mb-1">
                      {merchant.businessName}
                    </h3>
                    {merchant.description && (
                      <p className="text-gray-600 mb-4">{merchant.description}</p>
                    )}
                  </div>
                  <div className="ml-6">
                    {merchant.isApproved ? (
                      <span className="px-4 py-2 bg-green-100 text-green-800 rounded-full font-semibold text-sm">
                        Approvato
                      </span>
                    ) : (
                      <span className="px-4 py-2 bg-yellow-100 text-yellow-800 rounded-full font-semibold text-sm">
                        In attesa
                      </span>
                    )}
                  </div>
                </div>

                <div className="grid md:grid-cols-3 gap-4 mb-4">
                  <div>
                    <p className="text-sm text-gray-500 font-semibold">Proprietario</p>
                    <p className="text-gray-800">
                      {merchant.user.firstName} {merchant.user.lastName}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-500 font-semibold">Email</p>
                    <p className="text-gray-800">{merchant.user.email}</p>
                  </div>
                  {merchant.user.phoneNumber && (
                    <div>
                      <p className="text-sm text-gray-500 font-semibold">Telefono</p>
                      <p className="text-gray-800">{merchant.user.phoneNumber}</p>
                    </div>
                  )}
                  {merchant.vatNumber && (
                    <div>
                      <p className="text-sm text-gray-500 font-semibold">Partita IVA</p>
                      <p className="text-gray-800">{merchant.vatNumber}</p>
                    </div>
                  )}
                  {merchant.city && (
                    <div>
                      <p className="text-sm text-gray-500 font-semibold">Città</p>
                      <p className="text-gray-800">{merchant.city}</p>
                    </div>
                  )}
                  <div>
                    <p className="text-sm text-gray-500 font-semibold">Registrato il</p>
                    <p className="text-gray-800">
                      {new Date(merchant.createdAt).toLocaleDateString('it-IT')}
                    </p>
                  </div>
                </div>

                {!merchant.isApproved && (
                  <div className="mt-6 flex gap-4">
                    <button
                      onClick={() => handleApprove(merchant.id)}
                      className="flex-1 bg-green-600 text-white py-3 rounded-lg hover:bg-green-700 font-semibold transition-colors"
                    >
                      Approva Merchant
                    </button>
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="flex-1 bg-red-600 text-white py-3 rounded-lg hover:bg-red-700 font-semibold transition-colors"
                    >
                      Rifiuta
                    </button>
                  </div>
                )}

                {merchant.isApproved && (
                  <div className="mt-6">
                    <button
                      onClick={() => handleReject(merchant.id)}
                      className="bg-red-600 text-white px-6 py-2 rounded-lg hover:bg-red-700 transition-colors"
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

export default MerchantApproval
