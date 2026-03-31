import { useState, FormEvent } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../../lib/axios'
import './RegisterPage.css'

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    companyName: '',
    vatNumber: '',
    city: '',
    address: '',
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
      await apiClient.post('/auth/merchant/register', formData)
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
          <div className="register-logo-icon">🏢</div>
          <h1 className="register-title">Registra la tua azienda</h1>
          <p className="register-subtitle">Crea un account merchant per gestire i tuoi dipendenti</p>
        </div>

        {success ? (
          <div className="register-success">
            <div className="success-icon">✅</div>
            <h2 className="success-title">Registrazione inviata</h2>
            <p className="success-message">
              Registrazione inviata. L'account sarà attivato dall'amministratore.
            </p>
            <Link to="/login" className="btn-primary" style={{ display: 'inline-block', textDecoration: 'none', padding: '10px 24px', borderRadius: '8px' }}>
              Torna al Login
            </Link>
          </div>
        ) : (
          <form className="register-form" onSubmit={handleSubmit}>
            {error && <div className="register-error">{error}</div>}

            <div className="section-divider">Dati Personali</div>
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
                minLength={6}
              />
            </div>

            <div className="section-divider">Dati Aziendali</div>
            <div className="form-group">
              <label className="form-label">Ragione Sociale *</label>
              <input
                type="text"
                className="form-input"
                placeholder="Azienda S.r.l."
                value={formData.companyName}
                onChange={e => handleChange('companyName', e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label className="form-label">Partita IVA *</label>
              <input
                type="text"
                className="form-input"
                placeholder="IT12345678901"
                value={formData.vatNumber}
                onChange={e => handleChange('vatNumber', e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Città *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Milano"
                  value={formData.city}
                  onChange={e => handleChange('city', e.target.value)}
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Indirizzo *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Via Roma 1"
                  value={formData.address}
                  onChange={e => handleChange('address', e.target.value)}
                  required
                />
              </div>
            </div>

            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Invio in corso...' : 'Registra Azienda'}
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
