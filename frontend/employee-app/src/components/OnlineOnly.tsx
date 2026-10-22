import { useEffect, useRef, useState, type ReactNode } from 'react'
import './OnlineOnly.css'

/** Richiede il server raggiungibile e conserva i moduli aperti durante le disconnessioni. */
export function OnlineOnly({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<'checking' | 'online' | 'offline'>('checking')
  const [hasConnected, setHasConnected] = useState(false)
  const retry = useRef<() => void>(() => {})
  const retryButton = useRef<HTMLButtonElement>(null)
  const connectionDialog = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    let disposed = false
    let pending: AbortController | undefined

    const check = async () => {
      // Sostituisce controlli precedenti: un risultato vecchio non deve riaprire l'app.
      pending?.abort()
      const controller = new AbortController()
      pending = controller
      const timeout = window.setTimeout(() => controller.abort(), 8000)
      let reachable = false
      try {
        const response = await fetch(`/connectivity.json?check=${Date.now()}`, {
          cache: 'no-store',
          signal: controller.signal,
        })
        const health = response.ok ? await response.json() : null
        reachable = health?.status === 'healthy' && health?.app === 'employee-app'
      } catch {
        // Assenza di rete, timeout e portali Wi-Fi non consentono l'uso dell'app.
      } finally {
        window.clearTimeout(timeout)
      }
      if (disposed || pending !== controller) return
      pending = undefined
      if (reachable) setHasConnected(true)
      setStatus(reachable ? 'online' : 'offline')
    }
    const offline = () => {
      pending?.abort()
      pending = undefined
      setStatus('offline')
    }
    const visible = () => {
      if (document.visibilityState === 'visible') void check()
    }
    retry.current = () => { void check() }
    void check()
    window.addEventListener('offline', offline)
    window.addEventListener('online', check)
    document.addEventListener('visibilitychange', visible)
    const interval = window.setInterval(visible, 30000)
    return () => {
      disposed = true
      pending?.abort()
      window.clearInterval(interval)
      window.removeEventListener('offline', offline)
      window.removeEventListener('online', check)
      document.removeEventListener('visibilitychange', visible)
    }
  }, [])

  useEffect(() => {
    const dialog = connectionDialog.current
    if (status !== 'online' && dialog && !dialog.open) dialog.showModal()
    if (status === 'offline') retryButton.current?.focus()
    return () => { dialog?.close() }
  }, [status])

  return <>
    {hasConnected && <div hidden={status !== 'online'}>{children}</div>}
    {status !== 'online' && <dialog ref={connectionDialog} className="online-only"
      aria-label="Connessione al portale" aria-busy={status === 'checking'} onCancel={event => event.preventDefault()}>
      <section className="online-only-card" role="status" aria-live="polite">
        <h1>{status === 'checking' ? 'Verifica connessione' : 'Connessione non disponibile'}</h1>
        <p>{status === 'checking'
          ? 'Attendi un momento.'
          : 'Per usare il portale serve una connessione Internet e il servizio deve essere raggiungibile. Controlla la connessione e riprova.'}</p>
        {status === 'offline' && <button ref={retryButton} type="button" data-activity="employee.connection.retry" onClick={() => retry.current()}>Riprova</button>}
      </section>
    </dialog>}
  </>
}
