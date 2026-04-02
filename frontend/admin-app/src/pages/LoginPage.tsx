import { useState, useEffect, useCallback } from 'react'
import apiClient from '../lib/axios'
import './LoginPage.css'

type ServiceStatus = 'checking' | 'online' | 'offline'

const apiURL = import.meta.env.PROD
  ? (import.meta.env.VITE_API_URL || 'https://appointment-scheduler-api.azurewebsites.net')
  : window.location.origin

const safeEnvVars: Record<string, string> = {
  VITE_API_URL: import.meta.env.VITE_API_URL || '(not set)',
  MODE: import.meta.env.MODE,
  PROD: String(import.meta.env.PROD),
  BASE_URL: import.meta.env.BASE_URL,
}

function StatusDot({ status }: { status: ServiceStatus }) {
  const colors: Record<ServiceStatus, string> = {
    checking: '#f59e0b',
    online: '#22c55e',
    offline: '#ef4444',
  }
  return (
    <span
      style={{
        display: 'inline-block',
        width: 8,
        height: 8,
        borderRadius: '50%',
        backgroundColor: colors[status],
        flexShrink: 0,
        ...(status === 'checking' ? { animation: 'pulse 1.2s infinite' } : {}),
      }}
    />
  )
}

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  // 1 = Admin, 2 = Merchant, 3 = Employee
  accountType: number
}

interface LoginPageProps {
  onLogin: (user: User, token: string) => void
}

export default function LoginPage({ onLogin }: LoginPageProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [statusOpen, setStatusOpen] = useState(false)
  const [apiStatus, setApiStatus] = useState<ServiceStatus>('checking')

  const checkApi = useCallback(async () => {
    setApiStatus('checking')
    try {
      await apiClient.get('/health', { timeout: 5000 })
      setApiStatus('online')
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number } }
      // A response (even 404/401) means the server is reachable
      setApiStatus(axiosErr.response ? 'online' : 'offline')
    }
  }, [])

  useEffect(() => {
    if (statusOpen) checkApi()
  }, [statusOpen, checkApi])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const response = await apiClient.post('/auth/admin/login', { email, password })
      const { token, ...userData } = response.data as { token: string } & User


      if (userData.accountType !== 1) {
        setError('Access reserved for administrators only.')
        return
      }

      localStorage.setItem('token', token)
      localStorage.setItem('user', JSON.stringify(userData))
      onLogin(userData, token)
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr.response?.data?.message ?? 'Login failed. Please check your credentials.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-brand">
          <div className="login-brand-icon">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
          </div>
          <div className="login-brand-title">Admin Hub</div>
          <div className="login-brand-subtitle">Platform administration panel</div>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          {error && (
            <div className="login-error">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {error}
            </div>
          )}

          <div className="form-group">
            <label className="form-label" htmlFor="email">Email address</label>
            <input
              id="email"
              type="email"
              className="form-input"
              placeholder="admin@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>

          <button type="submit" className="btn-login" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="login-footer">Access restricted to platform administrators</p>
      </div>

      {/* System Status panel */}
      <div className="login-status">
        <button
          className="login-status-toggle"
          onClick={() => setStatusOpen(o => !o)}
          type="button"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          System Status
          <svg
            width="11"
            height="11"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth={2}
            style={{ transform: statusOpen ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform 0.2s' }}
          >
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </button>

        {statusOpen && (
          <div className="login-status-panel">
            <div className="login-status-row">
              <span className="login-status-label">API endpoint</span>
              <span className="login-status-value" title={apiURL}>{apiURL}</span>
            </div>
            {Object.entries(safeEnvVars).map(([k, v]) => (
              <div className="login-status-row" key={k}>
                <span className="login-status-label">{k}</span>
                <span className="login-status-value">{v}</span>
              </div>
            ))}
            <div className="login-status-row login-status-check">
              <span className="login-status-label">API reachable</span>
              <span className="login-status-indicator">
                <StatusDot status={apiStatus} />
                <span>{apiStatus}</span>
              </span>
              <button className="login-status-ping" onClick={checkApi} type="button">
                Ping
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
