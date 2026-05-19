import { useCallback, useEffect, useState } from 'react'
import apiClient from '../lib/axios'
import './EmailTestPage.css'

interface EmailServiceStatus {
  isConfigured: boolean
  senderAddress: string
  senderDisplayName: string
  endpointHost: string | null
}

interface SendTestSuccess {
  success: true
  sentAt: string
  recipient: string
}

interface SendTestErrorBody {
  success: false
  message?: string
  errorCode?: string
  status?: number
  isConfigured?: boolean
}

type ResultState =
  | { kind: 'idle' }
  | { kind: 'success'; data: SendTestSuccess }
  | { kind: 'error'; data: SendTestErrorBody; httpStatus?: number }

function getInitialRecipient(): string {
  try {
    const raw = localStorage.getItem('user')
    if (!raw) return ''
    const parsed = JSON.parse(raw) as { email?: string }
    return parsed.email ?? ''
  } catch {
    return ''
  }
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export default function EmailTestPage() {
  const [status, setStatus] = useState<EmailServiceStatus | null>(null)
  const [statusLoading, setStatusLoading] = useState(true)
  const [statusError, setStatusError] = useState<string | null>(null)

  const [recipient, setRecipient] = useState<string>(getInitialRecipient())
  const [sending, setSending] = useState(false)
  const [result, setResult] = useState<ResultState>({ kind: 'idle' })

  const loadStatus = useCallback(async () => {
    setStatusLoading(true)
    setStatusError(null)
    try {
      const res = await apiClient.get<EmailServiceStatus>('/admin/tools/email/status')
      setStatus(res.data)
    } catch (err: unknown) {
      const e = err as { message?: string; response?: { status?: number } }
      setStatusError(
        e.response?.status
          ? `Errore HTTP ${e.response.status} nel recupero stato.`
          : e.message ?? 'Errore di rete nel recupero stato.'
      )
    } finally {
      setStatusLoading(false)
    }
  }, [])

  useEffect(() => {
    loadStatus()
  }, [loadStatus])

  const recipientValid = EMAIL_REGEX.test(recipient.trim())
  const canSend = !!status?.isConfigured && recipientValid && !sending

  const handleSend = async () => {
    setSending(true)
    setResult({ kind: 'idle' })
    try {
      const res = await apiClient.post<SendTestSuccess>('/admin/tools/email/test', {
        toAddress: recipient.trim(),
      })
      setResult({ kind: 'success', data: res.data })
    } catch (err: unknown) {
      const e = err as { response?: { status?: number; data?: SendTestErrorBody }; message?: string }
      const body: SendTestErrorBody = e.response?.data ?? {
        success: false,
        message: e.message ?? 'Errore di rete',
      }
      setResult({ kind: 'error', data: body, httpStatus: e.response?.status })
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="email-test-page">
      {/* Stato configurazione */}
      <section className="et-section">
        <div className="et-section-header">
          <h2 className="et-section-title">Stato servizio email</h2>
          <button className="et-refresh-btn" onClick={loadStatus} disabled={statusLoading}>
            {statusLoading ? 'Caricamento…' : 'Aggiorna'}
          </button>
        </div>

        <div className="et-card">
          {statusLoading && <div className="et-muted">Caricamento stato in corso…</div>}

          {!statusLoading && statusError && (
            <div className="et-banner et-banner-error">
              <strong>Impossibile recuperare lo stato:</strong> {statusError}
            </div>
          )}

          {!statusLoading && status && (
            <>
              <div className="et-row">
                <span className="et-label">Stato</span>
                <span className={`et-badge ${status.isConfigured ? 'et-badge-ok' : 'et-badge-err'}`}>
                  {status.isConfigured ? 'Configurato' : 'Non configurato'}
                </span>
              </div>
              <div className="et-row">
                <span className="et-label">Sender address</span>
                <code className="et-code">{status.senderAddress}</code>
              </div>
              <div className="et-row">
                <span className="et-label">Sender display name</span>
                <span className="et-value">{status.senderDisplayName}</span>
              </div>
              <div className="et-row">
                <span className="et-label">Endpoint host</span>
                <code className="et-code">{status.endpointHost ?? '(non disponibile)'}</code>
              </div>

              {!status.isConfigured && (
                <div className="et-banner et-banner-error et-mt">
                  <strong>Il servizio non è configurato.</strong>
                  <div className="et-mt-sm">
                    Impostare <code>AzureCommunicationServices:ConnectionString</code> con una
                    access key valida in <code>appsettings.Development.json</code> (locale) o
                    nelle <em>App Settings</em> di Azure (produzione). Riavviare l'API dopo la
                    modifica.
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </section>

      {/* Invio test */}
      <section className="et-section">
        <div className="et-section-header">
          <h2 className="et-section-title">Invia email di test</h2>
        </div>

        <div className="et-card">
          <label className="et-field">
            <span className="et-label">Destinatario</span>
            <input
              type="email"
              className="et-input"
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
              placeholder="nome@dominio.it"
              autoComplete="off"
              spellCheck={false}
            />
            {!recipientValid && recipient.length > 0 && (
              <span className="et-hint et-hint-warn">Indirizzo email non valido.</span>
            )}
          </label>

          <div className="et-actions">
            <button className="et-send-btn" onClick={handleSend} disabled={!canSend}>
              {sending ? 'Invio in corso…' : 'Invia test'}
            </button>
            {!status?.isConfigured && (
              <span className="et-hint">Servizio non configurato: invio disabilitato.</span>
            )}
          </div>

          {result.kind === 'success' && (
            <div className="et-banner et-banner-ok et-mt">
              <strong>Email inviata.</strong>
              <div className="et-mt-sm">
                Destinatario: <code>{result.data.recipient}</code><br />
                Inviata alle: <code>{result.data.sentAt}</code>
              </div>
            </div>
          )}

          {result.kind === 'error' && (
            <div className="et-banner et-banner-error et-mt">
              <strong>
                Invio fallito{result.httpStatus ? ` (HTTP ${result.httpStatus})` : ''}.
              </strong>
              <div className="et-mt-sm">
                {result.data.errorCode && (
                  <div>Error code: <code>{result.data.errorCode}</code></div>
                )}
                {result.data.status !== undefined && (
                  <div>ACS status: <code>{result.data.status}</code></div>
                )}
                {result.data.message && <div>{result.data.message}</div>}
              </div>
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
