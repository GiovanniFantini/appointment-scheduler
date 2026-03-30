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
    <div className="min-h-screen flex items-center justify-center relative overflow-hidden">
      <div className="absolute inset-0 bg-gradient-to-br from-dark-bg via-cyber-950 to-dark-surface">
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-blue opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-purple opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="relative z-10 w-full max-w-md mx-4 animate-scale-in">
        <div className="glass-card rounded-3xl p-8 shadow-glow-blue border border-white/10">
          <div className="text-center mb-8">
            <div className="inline-block p-4 rounded-2xl bg-gradient-to-br from-neon-blue/20 to-neon-purple/20 mb-4 border border-neon-blue/30">
              <svg className="w-12 h-12 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
            </div>
            <h1 className="text-3xl font-bold gradient-text mb-2">Nuova Password</h1>
            <p className="text-gray-400 text-sm">Scegli una nuova password per il tuo account</p>
          </div>

          {success ? (
            <div className="text-center space-y-4">
              <div className="bg-green-500/10 border border-green-500/50 text-green-400 px-4 py-4 rounded-xl">
                <svg className="w-8 h-8 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p className="font-semibold">Password aggiornata con successo</p>
                <p className="text-sm mt-1 text-green-300">Verrai reindirizzato al login tra pochi secondi...</p>
              </div>
              <Link to="/login" className="block text-neon-cyan hover:text-neon-blue transition-colors text-sm">
                Vai al login ora
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-6">
              {error && (
                <div className="bg-red-500/10 border border-red-500/50 text-red-400 px-4 py-3 rounded-xl">
                  {error}
                  {!token && (
                    <div className="mt-2">
                      <Link to="/forgot-password" className="underline text-sm">Richiedi nuovo link</Link>
                    </div>
                  )}
                </div>
              )}

              <div className="space-y-2">
                <label className="block text-white/90 text-sm font-semibold mb-2">Nuova password</label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all duration-300"
                  placeholder="Minimo 8 caratteri"
                  minLength={8}
                  required
                  disabled={!token}
                />
              </div>

              <div className="space-y-2">
                <label className="block text-white/90 text-sm font-semibold mb-2">Conferma password</label>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all duration-300"
                  placeholder="Ripeti la password"
                  minLength={8}
                  required
                  disabled={!token}
                />
              </div>

              <button
                type="submit"
                disabled={loading || !token}
                className="w-full bg-gradient-to-r from-neon-blue to-neon-purple text-white py-3.5 rounded-xl font-semibold hover:shadow-glow-blue transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Salvataggio...' : 'Salva nuova password'}
              </button>

              <div className="text-center">
                <Link to="/login" className="text-neon-cyan hover:text-neon-blue transition-colors text-sm">
                  Torna al login
                </Link>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}

export default ResetPassword
