import './ReportsPage.css'

const mockStats = [
  { label: 'Total Appointments', value: '—', trend: null },
  { label: 'Monthly Revenue', value: '—', trend: null },
  { label: 'Platform Uptime', value: '99.9%', trend: '↑ stable' },
]

export default function ReportsPage() {
  return (
    <div className="reports-page">
      <div className="page-header">
        <h1 className="page-title">Reports</h1>
        <p className="page-subtitle">Platform usage analytics and insights</p>
      </div>

      {/* Mock stats */}
      <div className="mock-stats-grid">
        {mockStats.map((s) => (
          <div key={s.label} className="mock-stat-card">
            <div className="mock-stat-label">{s.label}</div>
            <div className="mock-stat-value">{s.value}</div>
            {s.trend && <div className="mock-stat-trend">{s.trend}</div>}
          </div>
        ))}
      </div>

      {/* Placeholder */}
      <div className="reports-placeholder">
        <div className="reports-placeholder-icon">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
            <line x1="2" y1="20" x2="22" y2="20" />
          </svg>
        </div>
        <div className="reports-placeholder-title">Reports in sviluppo</div>
        <p className="reports-placeholder-text">
          Advanced analytics and reporting features are currently being developed.
          Full platform usage reports, appointment statistics, and revenue insights
          will be available in an upcoming release.
        </p>
        <span className="reports-placeholder-badge">Coming soon</span>
      </div>
    </div>
  )
}
