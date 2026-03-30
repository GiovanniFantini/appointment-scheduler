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
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
              </svg>
            </div>
            <h1 className="text-3xl font-bold gradient-text mb-2">Recupero Password</h1>
            <p className="text-gray-400 text-sm">Inserisci la tua email per ricevere le istruzioni</p>
          </div>

          {submitted ? (
            <div className="text-center space-y-4">
              <div className="bg-green-500/10 border border-green-500/50 text-green-400 px-4 py-4 rounded-xl">
                <svg className="w-8 h-8 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p className="font-semibold">Controlla la tua email</p>
                <p className="text-sm mt-1 text-green-300">Se l'indirizzo risulta registrato, riceverai le istruzioni per il recupero della password.</p>
              </div>
              <Link to="/login" className="block text-neon-cyan hover:text-neon-blue transition-colors text-sm">
                Torna al login
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-6">
              {error && (
                <div className="bg-red-500/10 border border-red-500/50 text-red-400 px-4 py-3 rounded-xl">
                  {error}
                </div>
              )}

              <div className="space-y-2">
                <label className="block text-white/90 text-sm font-semibold mb-2">Email</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all duration-300"
                  placeholder="nome@esempio.com"
                  required
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full bg-gradient-to-r from-neon-blue to-neon-purple text-white py-3.5 rounded-xl font-semibold hover:shadow-glow-blue transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Invio in corso...' : 'Invia istruzioni'}
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

export default ForgotPassword
