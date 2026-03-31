import { useState, FormEvent } from 'react'
import { EmployeeUser } from '../../App'
import apiClient from '../../lib/axios'
import './LoginPage.css'

interface AuthResponse {
  token: string
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
  employeeId?: number
  merchantId?: number
  companyName?: string
  companies: Array<{ merchantId: number; companyName: string; city?: string; roleId: number; roleName: string }>
  activeFeatures: string[]
}

interface Props {
  onLogin: (user: EmployeeUser, token: string) => void
}

export default function LoginPage({ onLogin }: Props) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const { data } = await apiClient.post<AuthResponse>('/auth/employee/login', { email, password })

      if (!data.companies || data.companies.length === 0) {
        setError('Nessuna azienda associata al tuo account')
        setLoading(false)
        return
      }

      const user: EmployeeUser = {
        userId: data.userId,
        email: data.email,
        firstName: data.firstName,
        lastName: data.lastName,
        accountType: data.accountType,
        employeeId: data.employeeId,
        merchantId: data.merchantId,
        companyName: data.companyName,
        activeFeatures: data.activeFeatures ?? [],
        companies: data.companies,
      }

      if (data.companies.length === 1 && data.merchantId) {
        // Auto-selected: full token with merchantId
        onLogin(user, data.token)
      } else {
        // Multiple companies: partial token, go to select-company
        onLogin(user, data.token)
      }
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr.response?.data?.message ?? 'Credenziali non valide')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z" fill="currentColor" />
            </svg>
          </div>
          <h1 className="login-title">Portale Dipendenti</h1>
          <p className="login-subtitle">Accedi al tuo account</p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          {error && <div className="login-error">{error}</div>}

          <div className="form-group">
            <label htmlFor="email" className="form-label">Email</label>
            <input
              id="email"
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
            <label htmlFor="password" className="form-label">Password</label>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>

          <button type="submit" className="login-btn" disabled={loading}>
            {loading ? <span className="btn-spinner" /> : 'Accedi'}
          </button>
        </form>
      </div>
    </div>
  )
}
