import { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import apiClient from '../lib/axios'

/** Pagina di reset password: imposta la nuova password tramite token ricevuto via email. */
function ResetPassword() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!token) {
      setError('Link non valido. Richiedi un nuovo recupero password.')
    }
  }, [token])

  useEffect(() => {
    if (success) {
      const timer = setTimeout(() => navigate('/login'), 3000)
      return () => clearTimeout(timer)
    }
  }, [success, navigate])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (newPassword.length < 8) {
      setError('La password deve contenere almeno 8 caratteri.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Le password non corrispondono.')
      return
    }

    setLoading(true)
    try {
      await apiClient.post('/auth/reset-password', { token, newPassword })
      setSuccess(true)
    } catch (err: any) {
      setError(err.response?.data?.message || 'Errore durante il reset. Riprova piu\' tardi.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-purple-600 to-red-600">
      <div className="bg-white p-8 rounded-lg shadow-2xl w-full max-w-md">
        <h1 className="text-3xl font-bold text-center mb-2 text-gray-800">Nuova Password</h1>
        <p className="text-center text-gray-600 mb-6">Scegli una nuova password per il tuo account</p>

        {success ? (
          <div className="text-center space-y-4">
            <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-4 rounded">
              <p className="font-semibold">Password aggiornata con successo</p>
              <p className="text-sm mt-1">Verrai reindirizzato al login tra pochi secondi...</p>
            </div>
            <Link to="/login" className="block text-purple-600 hover:text-purple-800 text-sm">
              Vai al login ora
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
                {error}
                {!token && (
                  <div className="mt-2">
                    <Link to="/forgot-password" className="underline text-sm">Richiedi nuovo link</Link>
                  </div>
                )}
              </div>
            )}

            <div>
              <label className="block text-gray-700 text-sm font-bold mb-2">Nuova password</label>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 text-gray-800 bg-white"
                placeholder="Minimo 8 caratteri"
                minLength={8}
                required
                disabled={!token}
              />
            </div>

            <div>
              <label className="block text-gray-700 text-sm font-bold mb-2">Conferma password</label>
              <input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 text-gray-800 bg-white"
                placeholder="Ripeti la password"
                minLength={8}
                required
                disabled={!token}
              />
            </div>

            <button
              type="submit"
              disabled={loading || !token}
              className="w-full bg-purple-600 text-white py-2 rounded-lg hover:bg-purple-700 transition duration-200 disabled:opacity-50"
            >
              {loading ? 'Caricamento...' : 'Salva nuova password'}
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

export default ResetPassword
