import { useState, useEffect, useRef, FormEvent } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../../lib/axios'
import '../LoginPage/LoginPage.css'
import './ForgotPasswordPage.css'

const COOLDOWN_SECONDS = 60
// Chiave sessionStorage: persiste l'istante dell'ultima richiesta così il
// cooldown sopravvive a un reload della pagina (il backend ha lo stesso
// rate-limit di 60s: ricaricare la pagina non deve aggirarlo).
const COOLDOWN_KEY = 'forgotPasswordLastRequestAt'

/** Secondi di cooldown ancora attivi in base al timestamp persistito. */
function remainingCooldown(): number {
  const raw = sessionStorage.getItem(COOLDOWN_KEY)
  if (!raw) return 0
  const elapsed = (Date.now() - Number(raw)) / 1000
  return Math.max(0, Math.ceil(COOLDOWN_SECONDS - elapsed))
}

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(() => remainingCooldown() > 0)
  const [error, setError] = useState('')
  const [cooldown, setCooldown] = useState(() => remainingCooldown())
  const timerRef = useRef<ReturnType<typeof setInterval>>()

  const tick = () => {
    timerRef.current = setInterval(() => {
      const left = remainingCooldown()
      setCooldown(left)
      if (left <= 0 && timerRef.current) clearInterval(timerRef.current)
    }, 1000)
  }

  useEffect(() => {
    // Riprende il countdown se al mount c'è ancora cooldown attivo (post-reload).
    if (remainingCooldown() > 0) tick()
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [])

  const startCooldown = () => {
    sessionStorage.setItem(COOLDOWN_KEY, String(Date.now()))
    setCooldown(COOLDOWN_SECONDS)
    if (timerRef.current) clearInterval(timerRef.current)
    tick()
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await apiClient.post('/auth/forgot-password', { email })
      setSent(true)
      startCooldown()
    } catch {
      setError('Si è verificato un errore. Riprova più tardi.')
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
      setError('Si è verificato un errore. Riprova più tardi.')
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
              <path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zM12 17c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zM15 8H9V6c0-1.66 1.34-3 3-3s3 1.34 3 3v2z" fill="currentColor"/>
            </svg>
          </div>
          <h1 className="login-title">Recupero Password</h1>
          <p className="login-subtitle">Inserisci la tua email per ricevere il link di reset</p>
        </div>

        {sent ? (
          <>
            <div className="forgot-success">
              Se l'email è registrata, riceverai a breve le istruzioni per reimpostare la password.
            </div>
            {error && <div className="login-error" style={{ marginTop: 12 }}>{error}</div>}
            <div className="resend-section">
              {cooldown > 0 ? (
                <span className="resend-cooldown">
                  Puoi richiedere un nuovo link tra {cooldown}s
                </span>
              ) : (
                <button
                  type="button"
                  className="resend-btn"
                  onClick={handleResend}
                  disabled={loading}
                >
                  {loading ? <span className="btn-spinner" /> : 'Invia di nuovo'}
                </button>
              )}
            </div>
            <div className="login-footer">
              <Link to="/login">Torna al login</Link>
            </div>
          </>
        ) : (
          <>
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
              <button type="submit" className="login-btn" disabled={loading}>
                {loading ? <span className="btn-spinner" /> : 'Invia link di reset'}
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
