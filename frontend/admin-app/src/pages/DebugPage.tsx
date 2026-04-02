import { useState, useCallback, useEffect } from 'react'
import apiClient from '../lib/axios'
import './DebugPage.css'

type ServiceStatus = 'idle' | 'checking' | 'online' | 'offline'

interface EndpointCheck {
  label: string
  path: string
  status: ServiceStatus
  statusCode?: number
  latencyMs?: number
  error?: string
}

const ENDPOINTS: Omit<EndpointCheck, 'status'>[] = [
  { label: 'Health', path: '/health' },
  { label: 'Auth – Admin Login', path: '/auth/admin/login' },
  { label: 'Merchants', path: '/merchants' },
  { label: 'Users', path: '/users' },
  { label: 'Reports', path: '/reports' },
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

function StatusBadge({ status, statusCode, latencyMs }: Pick<EndpointCheck, 'status' | 'statusCode' | 'latencyMs'>) {
  const map: Record<ServiceStatus, { bg: string; color: string; label: string }> = {
    idle: { bg: '#1e293b', color: '#475569', label: 'idle' },
    checking: { bg: '#451a03', color: '#f59e0b', label: 'checking…' },
    online: { bg: '#052e16', color: '#22c55e', label: statusCode ? `${statusCode}` : 'online' },
    offline: { bg: '#450a0a', color: '#ef4444', label: statusCode ? `${statusCode}` : 'offline' },
  }
  const { bg, color, label } = map[status]
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
      <span
        style={{
          background: bg,
          color,
          border: `1px solid ${color}33`,
          borderRadius: 4,
          padding: '2px 8px',
          fontSize: 12,
          fontFamily: 'monospace',
          ...(status === 'checking' ? { animation: 'dbg-pulse 1s infinite' } : {}),
        }}
      >
        {label}
      </span>
      {latencyMs !== undefined && status === 'online' && (
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
          const res = await apiClient.get(ep.path, { timeout: 6000 })
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
                <th>Path</th>
                <th>Status</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {checks.map(c => (
                <tr key={c.path}>
                  <td className="dbg-table-key">{c.label}</td>
                  <td><code className="dbg-code">{c.path}</code></td>
                  <td><StatusBadge status={c.status} statusCode={c.statusCode} latencyMs={c.latencyMs} /></td>
                  <td style={{ fontSize: 11, color: '#ef4444' }}>{c.error ?? ''}</td>
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
