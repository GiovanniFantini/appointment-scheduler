import { useState } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

/** Pagina di recupero password: invia email con link di reset. */
function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      await apiClient.post('/auth/forgot-password', { email })
      setSubmitted(true)
    } catch {
      setError('Errore durante la richiesta. Riprova piu\' tardi.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-purple-600 to-red-600">
      <div className="bg-white p-8 rounded-lg shadow-2xl w-full max-w-md">
        <h1 className="text-3xl font-bold text-center mb-2 text-gray-800">Recupero Password</h1>
        <p className="text-center text-gray-600 mb-6">Inserisci la tua email per ricevere le istruzioni</p>

        {submitted ? (
          <div className="text-center space-y-4">
            <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-4 rounded">
              <p className="font-semibold">Controlla la tua email</p>
              <p className="text-sm mt-1">Se l'indirizzo risulta registrato, riceverai le istruzioni per il recupero della password.</p>
            </div>
            <Link to="/login" className="block text-purple-600 hover:text-purple-800 text-sm">
              Torna al login
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
                {error}
              </div>
            )}

            <div>
              <label className="block text-gray-700 text-sm font-bold mb-2">Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 text-gray-800 bg-white"
                required
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-purple-600 text-white py-2 rounded-lg hover:bg-purple-700 transition duration-200 disabled:opacity-50"
            >
              {loading ? 'Caricamento...' : 'Invia istruzioni'}
            </button>

            <div className="text-center">
              <Link to="/login" className="text-purple-600 hover:text-purple-800 text-sm">
                Torna al login
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}

export default ForgotPassword
