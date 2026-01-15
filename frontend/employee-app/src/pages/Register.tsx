import { useState } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import VersionInfo from '../components/VersionInfo'

interface RegisterProps {
  onRegister: (user: any, token: string) => void
}

function Register({ onRegister }: RegisterProps) {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const response = await apiClient.post('/employeeauth/register', formData)
      const { token, ...userData } = response.data
      onRegister(userData, token)
    } catch (err: any) {
      setError(err.response?.data?.message || 'Errore durante la registrazione')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center relative overflow-hidden py-12">
      {/* Animated Background - Business Theme */}
      <div className="absolute inset-0 bg-gradient-to-br from-dark-bg via-cyber-950 to-dark-surface">
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-green opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-cyan opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
        <div className="absolute top-1/2 left-1/2 w-96 h-96 bg-neon-blue opacity-5 rounded-full blur-3xl animate-float" style={{ animationDelay: '4s' }}></div>
      </div>

      {/* Register Card */}
      <div className="relative z-10 w-full max-w-2xl mx-4 animate-scale-in">
        <div className="glass-card rounded-3xl p-8 shadow-glow-cyan border border-white/10">
          {/* Logo/Title */}
          <div className="text-center mb-8">
            <div className="inline-block p-4 rounded-2xl bg-gradient-to-br from-neon-green/20 to-neon-cyan/20 mb-4 border border-neon-green/30">
              <svg className="w-12 h-12 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <h1 className="text-4xl font-bold gradient-text mb-2">
              Registra il tuo account Employee
            </h1>
            <p className="text-gray-400 text-sm">Utilizza l'email con cui sei stato censito dal tuo datore di lavoro</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-red-500/10 border border-red-500/50 text-red-400 px-4 py-3 rounded-xl backdrop-blur-sm animate-slide-down">
                <div className="flex items-center gap-2">
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                  {error}
                </div>
              </div>
            )}

            <div className="glass-card-dark p-4 rounded-xl border border-neon-blue/30">
              <div className="flex items-start gap-2">
                <svg className="w-5 h-5 text-neon-blue flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
                <p className="text-sm text-gray-300">
                  <strong className="text-neon-cyan">Importante:</strong> Utilizza l'email con cui sei stato registrato dal tuo datore di lavoro. I tuoi dati personali saranno recuperati automaticamente.
                </p>
              </div>
            </div>

            <div className="space-y-2">
              <label className="block text-white/90 text-sm font-semibold">
                Email
              </label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-green focus:shadow-glow-cyan transition-all duration-300"
                placeholder="tua.email@azienda.com"
                required
              />
            </div>

            <div className="space-y-2">
              <label className="block text-white/90 text-sm font-semibold">
                Password
              </label>
              <input
                type="password"
                value={formData.password}
                onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                placeholder="••••••••"
                required
                minLength={6}
              />
              <p className="text-xs text-gray-400">Minimo 6 caratteri</p>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-gradient-to-r from-neon-green to-neon-cyan text-white py-3.5 rounded-xl font-semibold hover:shadow-glow-cyan transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed transform hover:scale-[1.02] active:scale-[0.98]"
            >
              {loading ? (
                <span className="flex items-center justify-center gap-2">
                  <svg className="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Registrazione in corso...
                </span>
              ) : (
                <span className="flex items-center justify-center gap-2">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                  </svg>
                  Registrati
                </span>
              )}
            </button>
          </form>

          <div className="mt-6 pt-6 border-t border-white/10">
            <p className="text-center text-gray-400">
              Hai già un account?{' '}
              <Link to="/login" className="text-neon-green hover:text-neon-cyan transition-colors font-semibold">
                Accedi
              </Link>
            </p>
          </div>

          <VersionInfo />
        </div>
      </div>
    </div>
  )
}

export default Register
