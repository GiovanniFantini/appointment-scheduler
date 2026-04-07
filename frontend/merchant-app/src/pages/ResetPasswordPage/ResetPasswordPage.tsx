import { useState, FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import apiClient from '../../lib/axios'
import '../LoginPage/LoginPage.css'
import './ResetPasswordPage.css'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (newPassword.length < 8) {
      setError('La password deve essere di almeno 8 caratteri.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Le password non coincidono.')
      return
    }

    setLoading(true)
    try {
      await apiClient.post('/auth/reset-password', { token, newPassword })
      setSuccess(true)
    } catch {
      setError('Il link non è più valido o è scaduto. Richiedi un nuovo recupero password.')
    } finally {
      setLoading(false)
    }
  }

  if (!token) {
    return (
      <div className="login-page">
        <div className="login-card">
          <div className="login-logo">
            <div className="login-logo-icon">🔑</div>
            <h1 className="login-title">Link non valido</h1>
            <p className="login-subtitle">Il link di reset password non è valido o è incompleto.</p>
          </div>
          <div className="login-footer">
            <Link to="/forgot-password">Richiedi un nuovo link</Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <div className="login-logo-icon">🔑</div>
          <h1 className="login-title">Nuova Password</h1>
          <p className="login-subtitle">Inserisci la tua nuova password</p>
        </div>

        {success ? (
          <>
            <div className="reset-success">
              Password aggiornata con successo!
            </div>
            <div className="login-footer">
              <Link to="/login">Vai al login</Link>
            </div>
          </>
        ) : (
          <>
            <form className="login-form" onSubmit={handleSubmit}>
              {error && <div className="login-error">{error}</div>}
              <div className="form-group">
                <label className="form-label">Nuova password</label>
                <input
                  type="password"
                  className="form-input"
                  placeholder="••••••••"
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  required
                  minLength={8}
                  autoComplete="new-password"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Conferma password</label>
                <input
                  type="password"
                  className="form-input"
                  placeholder="••••••••"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  required
                  minLength={8}
                  autoComplete="new-password"
                />
              </div>
              <button type="submit" className="btn-primary" disabled={loading}>
                {loading ? 'Aggiornamento...' : 'Aggiorna password'}
              </button>
            </form>
            <div className="login-footer">
              <Link to="/login">Torna al login</Link>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
