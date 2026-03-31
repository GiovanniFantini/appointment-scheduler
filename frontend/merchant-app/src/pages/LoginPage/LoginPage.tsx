import { useState, FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import apiClient from '../../lib/axios'
import { MerchantUser } from '../../App'
import './LoginPage.css'

interface LoginPageProps {
  onLogin: (user: MerchantUser, token: string) => void
}

export default function LoginPage({ onLogin }: LoginPageProps) {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await apiClient.post('/auth/merchant/login', { email, password })
      const { token, ...user } = res.data
      onLogin(user, token)
      navigate('/')
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Credenziali non valide')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <div className="login-logo-icon">🏢</div>
          <h1 className="login-title">Merchant App</h1>
          <p className="login-subtitle">Accedi al tuo account aziendale</p>
        </div>
        <form className="login-form" onSubmit={handleSubmit}>
          {error && <div className="login-error">{error}</div>}
          <div className="form-group">
            <label className="form-label">Email</label>
            <input
              type="email"
              className="form-input"
              placeholder="nome@azienda.it"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>
          <div className="form-group">
            <label className="form-label">Password</label>
            <input
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Accesso in corso...' : 'Accedi'}
          </button>
        </form>
        <div className="login-footer">
          Non hai un account?{' '}
          <Link to="/register">Registrati</Link>
        </div>
      </div>
    </div>
  )
}
