import { useState, useCallback, useEffect } from 'react'
import apiClient from '../lib/axios'
import './DebugPage.css'

type ServiceStatus = 'idle' | 'checking' | 'online' | 'offline'

interface EndpointCheck {
  label: string
  path: string
  method?: 'GET' | 'POST'
  expectedCodes?: number[]
  note?: string
  status: ServiceStatus
  statusCode?: number
  latencyMs?: number
  error?: string
}

const ENDPOINTS: Omit<EndpointCheck, 'status'>[] = [
  { label: 'Version',                  path: '/version' },
  { label: 'Auth – Admin Login',       path: '/auth/admin/login',    method: 'POST', expectedCodes: [400, 401], note: 'any HTTP response = reachable' },
  { label: 'Auth – Merchant Login',    path: '/auth/merchant/login', method: 'POST', expectedCodes: [400, 401], note: 'any HTTP response = reachable' },
  { label: 'Merchants',                path: '/merchants' },
  { label: 'Merchants – Pending',      path: '/merchants/pending' },
  { label: 'Notifications',            path: '/notifications' },
  { label: 'Employees (MerchantOnly)', path: '/employees',           expectedCodes: [400], note: 'admin token has no MerchantId claim → 400 expected' },
  { label: 'Events (MerchantOnly)',    path: '/events',              expectedCodes: [400], note: 'admin token has no MerchantId claim → 400 expected' },
]

function buildEnvRows(): Record<string, string> {
  const env = import.meta.env as Record<string, unknown>
  const rows: Record<string, string> = {}
  for (const [k, v] of Object.entries(env)) {
    rows[k] = String(v)
  }
  return rows
}

function buildSessionRows(): Record<string, string> {
  const token = localStorage.getItem('token')
  const userRaw = localStorage.getItem('user')
  let userStr = '(not set)'
  try {
    if (userRaw) userStr = JSON.stringify(JSON.parse(userRaw), null, 2)
  } catch {
    userStr = userRaw ?? '(not set)'
  }
  return {
    'token': token ? `${token.slice(0, 20)}…  (${token.length} chars)` : '(not set)',
    'user': userStr,
  }
}

function buildBrowserRows(): Record<string, string> {
  return {
    'userAgent': navigator.userAgent,
    'language': navigator.language,
    'online': String(navigator.onLine),
    'window': `${window.innerWidth} × ${window.innerHeight}`,
    'devicePixelRatio': String(window.devicePixelRatio),
    'url': window.location.href,
  }
}

function StatusBadge({ status, statusCode, latencyMs, expectedCodes }: Pick<EndpointCheck, 'status' | 'statusCode' | 'latencyMs' | 'expectedCodes'>) {
  const is2xx = statusCode !== undefined && statusCode >= 200 && statusCode < 300
  const isExpected = statusCode !== undefined && (is2xx || expectedCodes?.includes(statusCode))

  const style = ((): { bg: string; color: string; label: string; pulse?: boolean } => {
    if (status === 'idle')     return { bg: '#1e293b',  color: '#475569', label: 'idle' }
    if (status === 'checking') return { bg: '#451a03',  color: '#f59e0b', label: 'checking…', pulse: true }
    if (status === 'offline')  return { bg: '#450a0a',  color: '#ef4444', label: statusCode ? `${statusCode}` : 'offline' }
    // online: green if 2xx or expected code, amber otherwise
    return isExpected
      ? { bg: '#052e16', color: '#22c55e', label: `${statusCode}` }
      : { bg: '#451a03', color: '#f59e0b', label: `${statusCode}` }
  })()

  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
      <span
        style={{
          background: style.bg,
          color: style.color,
          border: `1px solid ${style.color}33`,
          borderRadius: 4,
          padding: '2px 8px',
          fontSize: 12,
          fontFamily: 'monospace',
          ...(style.pulse ? { animation: 'dbg-pulse 1s infinite' } : {}),
        }}
      >
        {style.label}
      </span>
      {latencyMs !== undefined && status !== 'idle' && (
        <span style={{ fontSize: 11, color: '#475569' }}>{latencyMs}ms</span>
      )}
    </span>
  )
}

function CopyButton({ value }: { value: string }) {
  const [copied, setCopied] = useState(false)
  const copy = () => {
    navigator.clipboard.writeText(value).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    })
  }
  return (
    <button className="dbg-copy-btn" onClick={copy} title="Copy">
      {copied ? '✓' : '⎘'}
    </button>
  )
}

function KVTable({ rows }: { rows: Record<string, string> }) {
  return (
    <table className="dbg-table">
      <tbody>
        {Object.entries(rows).map(([k, v]) => (
          <tr key={k}>
            <td className="dbg-table-key">{k}</td>
            <td className="dbg-table-val">
              <pre>{v}</pre>
            </td>
            <td className="dbg-table-actions">
              <CopyButton value={v} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default function DebugPage() {
  const [checks, setChecks] = useState<EndpointCheck[]>(
    ENDPOINTS.map(e => ({ ...e, status: 'idle' }))
  )
  const [running, setRunning] = useState(false)

  const runChecks = useCallback(async () => {
    setRunning(true)
    setChecks(ENDPOINTS.map(e => ({ ...e, status: 'checking' })))

    await Promise.all(
      ENDPOINTS.map(async (ep, i) => {
        const t0 = Date.now()
        try {
          const req = ep.method === 'POST'
            ? apiClient.post(ep.path, {}, { timeout: 6000 })
            : apiClient.get(ep.path, { timeout: 6000 })
          const res = await req
          const latencyMs = Date.now() - t0
          setChecks(prev => {
            const next = [...prev]
            next[i] = { ...ep, status: 'online', statusCode: res.status, latencyMs }
            return next
          })
        } catch (err: unknown) {
          const latencyMs = Date.now() - t0
          const axErr = err as { response?: { status?: number }; message?: string }
          const statusCode = axErr.response?.status
          // Any HTTP response (even 4xx) means the server is reachable
          const isReachable = !!axErr.response
          setChecks(prev => {
            const next = [...prev]
            next[i] = {
              ...ep,
              status: isReachable ? 'online' : 'offline',
              statusCode,
              latencyMs: isReachable ? latencyMs : undefined,
              error: isReachable ? undefined : (axErr.message ?? 'Network error'),
            }
            return next
          })
        }
      })
    )

    setRunning(false)
  }, [])

  // Auto-run on mount
  useEffect(() => { runChecks() }, [runChecks])

  const envRows = buildEnvRows()
  const sessionRows = buildSessionRows()
  const browserRows = buildBrowserRows()

  return (
    <div className="dbg-page">
      {/* ── Health Checks ── */}
      <section className="dbg-section">
        <div className="dbg-section-header">
          <h2 className="dbg-section-title">API Health Checks</h2>
          <button className="dbg-run-btn" onClick={runChecks} disabled={running}>
            {running ? 'Running…' : 'Run checks'}
          </button>
        </div>
        <div className="dbg-card">
          <table className="dbg-table dbg-checks-table">
            <thead>
              <tr>
                <th>Service</th>
                <th>Method</th>
                <th>Path</th>
                <th>Status</th>
                <th>Note / Error</th>
              </tr>
            </thead>
            <tbody>
              {checks.map(c => (
                <tr key={c.path}>
                  <td className="dbg-table-key">{c.label}</td>
                  <td>
                    <span className={`dbg-method dbg-method-${(c.method ?? 'GET').toLowerCase()}`}>
                      {c.method ?? 'GET'}
                    </span>
                  </td>
                  <td><code className="dbg-code">{c.path}</code></td>
                  <td><StatusBadge status={c.status} statusCode={c.statusCode} latencyMs={c.latencyMs} expectedCodes={c.expectedCodes} /></td>
                  <td style={{ fontSize: 11, color: c.error ? '#ef4444' : '#475569' }}>
                    {c.error ?? (c.statusCode == null ? c.note : null) ?? ''}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* ── Environment Variables ── */}
      <section className="dbg-section">
        <div className="dbg-section-header">
          <h2 className="dbg-section-title">Environment Variables</h2>
          <span className="dbg-badge">VITE / import.meta.env</span>
        </div>
        <div className="dbg-card">
          <KVTable rows={envRows} />
        </div>
      </section>

      {/* ── Session ── */}
      <section className="dbg-section">
        <div className="dbg-section-header">
          <h2 className="dbg-section-title">Current Session</h2>
          <span className="dbg-badge">localStorage</span>
        </div>
        <div className="dbg-card">
          <KVTable rows={sessionRows} />
        </div>
      </section>

      {/* ── Browser ── */}
      <section className="dbg-section">
        <div className="dbg-section-header">
          <h2 className="dbg-section-title">Browser & Runtime</h2>
        </div>
        <div className="dbg-card">
          <KVTable rows={browserRows} />
        </div>
      </section>
    </div>
  )
}
