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
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: ''
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const response = await apiClient.post('/auth/register', {
        ...formData,
        registerAsMerchant: false // Consumer registration
      })
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
      {/* Animated Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-dark-bg via-cyber-950 to-dark-surface">
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-purple opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-cyan opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
        <div className="absolute top-1/2 left-1/2 w-96 h-96 bg-neon-blue opacity-5 rounded-full blur-3xl animate-float" style={{ animationDelay: '4s' }}></div>
      </div>

      {/* Register Card */}
      <div className="relative z-10 w-full max-w-md mx-4 animate-scale-in">
        <div className="glass-card rounded-3xl p-8 shadow-glow-purple border border-white/10">
          {/* Logo/Title */}
          <div className="text-center mb-8">
            <div className="inline-block p-4 rounded-2xl bg-gradient-to-br from-neon-purple/20 to-neon-cyan/20 mb-4 border border-neon-purple/30">
              <svg className="w-12 h-12 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
              </svg>
            </div>
            <h1 className="text-4xl font-bold gradient-text mb-2">
              Registrati
            </h1>
            <p className="text-gray-400 text-sm">Crea il tuo account in pochi secondi</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
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

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="block text-white/90 text-sm font-semibold">
                  Nome
                </label>
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all duration-300"
                  placeholder="Mario"
                  required
                />
              </div>

              <div className="space-y-2">
                <label className="block text-white/90 text-sm font-semibold">
                  Cognome
                </label>
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all duration-300"
                  placeholder="Rossi"
                  required
                />
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
                className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-blue focus:shadow-glow-blue transition-all duration-300"
                placeholder="nome@esempio.com"
                required
              />
            </div>

            <div className="space-y-2">
              <label className="block text-white/90 text-sm font-semibold">
                Telefono <span className="text-gray-500 text-xs">(opzionale)</span>
              </label>
              <input
                type="tel"
                value={formData.phoneNumber}
                onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                placeholder="+39 123 456 7890"
              />
            </div>

            <div className="space-y-2">
              <label className="block text-white/90 text-sm font-semibold">
                Password <span className="text-gray-500 text-xs">(min. 6 caratteri)</span>
              </label>
              <input
                type="password"
                value={formData.password}
                onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-purple focus:shadow-glow-purple transition-all duration-300"
                placeholder="••••••••"
                required
                minLength={6}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-gradient-to-r from-neon-purple to-neon-cyan text-white py-3.5 rounded-xl font-semibold hover:shadow-glow-purple transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed transform hover:scale-[1.02] active:scale-[0.98] mt-6"
            >
              {loading ? (
                <span className="flex items-center justify-center gap-2">
                  <svg className="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Registrazione...
                </span>
              ) : (
                <span className="flex items-center justify-center gap-2">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
                  </svg>
                  Crea Account
                </span>
              )}
            </button>
          </form>

          <div className="mt-6 pt-6 border-t border-white/10">
            <p className="text-center text-gray-400">
              Hai già un account?{' '}
              <Link to="/login" className="text-neon-cyan hover:text-neon-blue transition-colors font-semibold">
                Accedi ora
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
