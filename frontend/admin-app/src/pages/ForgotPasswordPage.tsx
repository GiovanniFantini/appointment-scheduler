import { useState, useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import './LoginPage.css'
import './ForgotPasswordPage.css'

const COOLDOWN_SECONDS = 60

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')
  const [cooldown, setCooldown] = useState(0)
  const timerRef = useRef<ReturnType<typeof setInterval>>()

  useEffect(() => {
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [])

  const startCooldown = () => {
    setCooldown(COOLDOWN_SECONDS)
    timerRef.current = setInterval(() => {
      setCooldown(prev => {
        if (prev <= 1) {
          clearInterval(timerRef.current)
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await apiClient.post('/auth/forgot-password', { email })
      setSent(true)
      startCooldown()
    } catch {
      setError('An error occurred. Please try again later.')
    } finally {
      setLoading(false)
    }
  }

  const handleResend = async () => {
    setError('')
    setLoading(true)
    try {
      await apiClient.post('/auth/forgot-password', { email })
      startCooldown()
    } catch {
      setError('An error occurred. Please try again later.')
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
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
          </div>
          <div className="login-brand-title">Password Recovery</div>
          <div className="login-brand-subtitle">Enter your email to receive a reset link</div>
        </div>

        {sent ? (
          <>
            <div className="forgot-success">
              If the email is registered, you will receive instructions to reset your password shortly.
            </div>
            {error && (
              <div className="login-error" style={{ marginTop: 12 }}>
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="12" />
                  <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                {error}
              </div>
            )}
            <div className="resend-section">
              {cooldown > 0 ? (
                <span className="resend-cooldown">
                  You can request a new link in {cooldown}s
                </span>
              ) : (
                <button
                  type="button"
                  className="resend-btn"
                  onClick={handleResend}
                  disabled={loading}
                >
                  {loading ? 'Sending…' : 'Send again'}
                </button>
              )}
            </div>
            <p className="login-footer">
              <Link to="/login">Back to login</Link>
            </p>
          </>
        ) : (
          <>
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
                  onChange={e => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
              </div>
              <button type="submit" className="btn-login" disabled={loading}>
                {loading ? 'Sending…' : 'Send reset link'}
              </button>
            </form>
            <p className="login-footer">
              <Link to="/login">Back to login</Link>
            </p>
          </>
        )}
      </div>
    </div>
  )
}
