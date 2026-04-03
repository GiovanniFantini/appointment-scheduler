import { useState, FormEvent } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../../lib/axios'
import './RegisterPage.css'

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    phoneNumber: '',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await apiClient.post('/auth/employee/register', {
        ...formData,
        phoneNumber: formData.phoneNumber || undefined,
      })
      setSuccess(true)
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante la registrazione')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="register-page">
      <div className="register-card">
        <div className="register-logo">
          <div className="register-logo-icon">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z" fill="currentColor" />
            </svg>
          </div>
          <h1 className="register-title">Crea il tuo account</h1>
          <p className="register-subtitle">Registrati per accedere al portale dipendenti</p>
        </div>

        {success ? (
          <div className="register-success">
            <div className="success-icon">✅</div>
            <h2 className="success-title">Registrazione completata</h2>
            <p className="success-message">
              Account creato con successo. Puoi ora accedere con le tue credenziali.
            </p>
            <Link to="/login" className="btn-primary" style={{ display: 'inline-block', textDecoration: 'none', padding: '10px 24px', borderRadius: '8px' }}>
              Accedi
            </Link>
          </div>
        ) : (
          <form className="register-form" onSubmit={handleSubmit}>
            {error && <div className="register-error">{error}</div>}

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Nome *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Mario"
                  value={formData.firstName}
                  onChange={e => handleChange('firstName', e.target.value)}
                  required
                  autoComplete="given-name"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Cognome *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Rossi"
                  value={formData.lastName}
                  onChange={e => handleChange('lastName', e.target.value)}
                  required
                  autoComplete="family-name"
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Email *</label>
              <input
                type="email"
                className="form-input"
                placeholder="mario.rossi@azienda.it"
                value={formData.email}
                onChange={e => handleChange('email', e.target.value)}
                required
                autoComplete="email"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Password *</label>
              <input
                type="password"
                className="form-input"
                placeholder="••••••••"
                value={formData.password}
                onChange={e => handleChange('password', e.target.value)}
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Telefono</label>
              <input
                type="tel"
                className="form-input"
                placeholder="+39 333 1234567"
                value={formData.phoneNumber}
                onChange={e => handleChange('phoneNumber', e.target.value)}
                autoComplete="tel"
              />
            </div>

            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Registrazione in corso...' : 'Crea account'}
            </button>
          </form>
        )}

        <div className="register-footer">
          Hai già un account?{' '}
          <Link to="/login">Accedi</Link>
        </div>
      </div>
    </div>
  )
}
