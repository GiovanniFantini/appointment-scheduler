type AppName = 'admin' | 'merchant' | 'employee'
type ClientEvent = { eventId: string; operationId: string; action: string; target: string; page: string; clientTime: number }
let app: AppName = 'admin'
let endpoint = ''
let queue: ClientEvent[] = []
let session = crypto.randomUUID()
let operation = crypto.randomUUID()
let identity = ''
let sending = false
let started = false
let dropped = 0
let lastInteractionAt = 0
const storageKey = 'scheduler.activity.queue.v1'

function ownerKey(value = identity) {
  try {
    const payload = JSON.parse(atob(value.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return `${payload.sub ?? ''}:${payload.MerchantId ?? ''}:${payload.jti ?? ''}`
  } catch { return value ? 'invalid-token' : 'anonymous' }
}
function persist() {
  try { sessionStorage.setItem(storageKey, JSON.stringify({ owner: ownerKey(), session, queue, dropped })) } catch { /* Storage non disponibile: resta la coda in memoria. */ }
}

function token() { try { return localStorage.getItem('token') ?? '' } catch { return '' } }
function safe(value: string) { return value.replace(/[^a-zA-Z0-9_./:#-]/g, '_').slice(0, 160) || 'unknown' }
function page() {
  // Le query possono contenere token di reset; gli identificativi dinamici non servono alla navigazione.
  return safe(location.pathname.split('/').map(p => /^\d+$/.test(p) ? ':id' : p).join('/'))
}
function context() {
  const current = token()
  if (current !== identity) {
    // Mai spedire eventi del vecchio account con le credenziali del nuovo.
    dropped += queue.length
    queue = []
    identity = current
    session = crypto.randomUUID()
    operation = crypto.randomUUID()
    persist()
  }
}
export function activityHeaders() {
  context()
  const recentInteraction = Date.now() - lastInteractionAt < 2000
  return { 'X-Scheduler-App': app, 'X-Session-Id': session,
    'X-Operation-Id': recentInteraction ? operation : crypto.randomUUID(),
    'X-Activity-Trigger': recentInteraction ? 'interaction' : 'background' }
}
export function trackActivity(action: string, target: string) {
  context()
  if (queue.length >= 500) { queue.shift(); dropped++ }
  queue.push({ eventId: crypto.randomUUID(), operationId: operation, action, target: safe(target), page: page(), clientTime: Date.now() })
  persist()
}
export function endActivityContext() {
  trackActivity('session.end', 'logout')
  void flush()
}
async function flush() {
  context()
  if (sending || !endpoint || !navigator.onLine || !queue.length) return
  if (dropped) { const count = dropped; dropped = 0; trackActivity('telemetry.dropped', String(count)) }
  const batch = queue.slice(0, 50)
  const owner = identity
  sending = true
  try {
    const response = await fetch(`${endpoint}/activity/collect`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', ...(owner ? { Authorization: `Bearer ${owner}` } : {}) },
      body: JSON.stringify({ app, sessionId: session, events: batch }), keepalive: true,
      signal: AbortSignal.timeout(10000)
    })
    if (owner === identity && (response.ok || response.status === 400 || response.status === 413)) {
      const sent = new Set(batch.map(e => e.eventId))
      queue = queue.filter(e => !sent.has(e.eventId))
      if (!response.ok) dropped += batch.length
      persist()
    }
  } catch { /* La coda limitata viene ritentata senza bloccare il flusso utente. */ }
  finally { sending = false }
}
export function initActivity(name: AppName, apiBase: string) {
  if (started) return
  started = true
  app = name
  endpoint = apiBase.replace(/\/$/, '')
  identity = token()
  try {
    const saved = JSON.parse(sessionStorage.getItem(storageKey) ?? 'null')
    if (saved?.owner === ownerKey() && Array.isArray(saved.queue)) {
      queue = saved.queue.filter((e: ClientEvent) => typeof e.clientTime === 'number' && e.clientTime > Date.now() - 86400000).slice(-500)
      session = typeof saved.session === 'string' ? saved.session : session
      dropped = Number(saved.dropped) || 0
    }
  } catch { /* Una coda corrotta non deve impedire l'avvio dell'app. */ }
  const capture = (event: Event) => {
    const element = event.target instanceof Element ? event.target.closest('[data-activity],button,a,input,select,textarea,[role="button"],form,summary') : null
    if (!element || element.closest('[data-no-activity]')) return
    if (event.type === 'click' || Date.now() - lastInteractionAt >= 2000) operation = crypto.randomUUID()
    lastInteractionAt = Date.now()
    // Posizione e tag sostituiscono testo e valori del DOM, che possono contenere dati personali.
    const parts: string[] = []
    let node: Element | null = element
    while (node && node !== document.body && parts.length < 8) {
      const parent: Element | null = node.parentElement
      parts.unshift(`${node.tagName.toLowerCase()}:${parent ? Array.from(parent.children).indexOf(node) : 0}`)
      node = parent
    }
    const target = element.getAttribute('data-activity') ?? parts.join('/')
    trackActivity(event.type === 'click' ? 'ui.click' : `form.${event.type}`, target)
  }
  const startedForms = new WeakSet<Element>()
  document.addEventListener('focusin', event => {
    const form = event.target instanceof Element ? event.target.closest('form') : null
    if (form && !form.closest('[data-no-activity]') && !startedForms.has(form)) {
      startedForms.add(form)
      trackActivity('form.start', form.getAttribute('data-activity') ?? 'form')
    }
  })
  for (const type of ['click', 'change', 'submit', 'invalid']) document.addEventListener(type, capture, true)
  for (const type of ['error', 'unhandledrejection']) window.addEventListener(type, () => trackActivity('ui.error', type))
  window.addEventListener('online', () => void flush())
  document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') void flush() })
  window.addEventListener('pagehide', () => { trackActivity('session.end', 'pagehide'); void flush() })
  window.setInterval(() => void flush(), 5000)
}
