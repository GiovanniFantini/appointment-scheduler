import { useEffect, useRef, useState, type ReactNode } from 'react'
import { useAuth } from './AuthContext'
import type { BaseUser } from './types'
import { Button } from '../ui/Button'
import './SubscriptionGate.css'

interface SubscriptionUser extends BaseUser {
  merchantId?: number
  activeFeatures: string[]
  featureLevels?: Record<string, string>
}

interface Access {
  merchantId: number
  status: 'Active' | 'Trial' | 'Expired' | 'Unassigned' | 'Inactive'
  activeFeatures: string[]
  featureLevels: Record<string, string>
  trialEndsAt: string | null
}

interface Props {
  children: ReactNode
  apiClient: { get: (path: string, config?: { signal: AbortSignal }) => Promise<{ data: Access }> }
  allowCompanySwitch?: boolean
}

export function SubscriptionGate({ children, apiClient, allowCompanySwitch }: Props) {
  const { user, updateUser, logout } = useAuth<SubscriptionUser>()
  const [resolvedAccess, setAccess] = useState<Access | null>(null)
  const access = resolvedAccess?.merchantId === user?.merchantId ? resolvedAccess : null
  const [error, setError] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const latestUser = useRef(user)
  latestUser.current = user

  useEffect(() => {
    const merchantId = user?.merchantId
    const controller = new AbortController()
    let pending = false
    let expiry: ReturnType<typeof setTimeout> | undefined
    const refresh = async () => {
      if (pending) return
      pending = true
      try {
        const { data } = await apiClient.get('/subscription', { signal: controller.signal })
        if (controller.signal.aborted) return
        const current = latestUser.current
        if (current?.merchantId !== merchantId || data.merchantId !== merchantId) return
        if (current) updateUser({ ...current, activeFeatures: data.activeFeatures, featureLevels: data.featureLevels })
        setAccess(data)
        setError(false)
        clearTimeout(expiry)
        if (data.status === 'Trial' && data.trialEndsAt) {
          const delay = Math.max(1000, new Date(data.trialEndsAt).getTime() - Date.now() + 500)
          expiry = setTimeout(refresh, Math.min(delay, 2147483647))
        }
      } catch {
        if (!controller.signal.aborted) setError(true)
      } finally {
        pending = false
      }
    }
    void refresh()
    const interval = setInterval(refresh, 30000)
    window.addEventListener('focus', refresh)
    window.addEventListener('subscription-changed', refresh)
    return () => {
      controller.abort()
      clearInterval(interval)
      clearTimeout(expiry)
      window.removeEventListener('focus', refresh)
      window.removeEventListener('subscription-changed', refresh)
    }
  }, [apiClient, user?.merchantId, updateUser, attempt])

  if (!access || error || !['Active', 'Trial'].includes(access.status)) {
    const title = error ? 'Impossibile verificare il pacchetto'
      : !access ? 'Verifica accesso…' : access.status === 'Expired' ? 'Prova terminata'
      : access.status === 'Unassigned' ? 'In attesa del pacchetto' : 'Azienda non attiva'
    return <main className="su-subscription-screen"><section className="su-subscription-card">
      <p className="su-subscription-eyebrow">ACCESSO AZIENDALE</p>
      <h1>{title}</h1>
      {(access || error) && <>
        <p>{error ? 'Riprova per riprendere il lavoro.' : 'Contatta l’amministratore per attivare o prorogare l’accesso. I dati sono conservati.'}</p>
        <div className="su-subscription-actions">
          <Button data-activity="shared-ui.auth.SubscriptionGate.1" onClick={() => setAttempt(n => n + 1)}>Riprova</Button>
          {allowCompanySwitch && user && <Button data-activity="shared-ui.auth.SubscriptionGate.2" variant="secondary" onClick={() => updateUser({ ...user, merchantId: undefined, activeFeatures: [], featureLevels: {} })}>Cambia azienda</Button>}
          <Button data-activity="shared-ui.auth.SubscriptionGate.3" variant="ghost" onClick={logout}>Esci</Button>
        </div>
      </>}
    </section></main>
  }

  // Il remount elimina dati e form dei moduli revocati, anche se la pagina era già aperta.
  return <div key={JSON.stringify([access.merchantId, access.activeFeatures, access.featureLevels])}>
    {access.status === 'Trial' && access.trialEndsAt && <div role="status" className="su-subscription-banner">
      Prova gratuita fino al {new Date(access.trialEndsAt).toLocaleString('it-IT')}
    </div>}
    {children}
  </div>
}
