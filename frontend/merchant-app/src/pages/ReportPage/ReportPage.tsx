import './ReportPage.css'

interface MockStat {
  label: string
  value: string
  change: string
  icon: string
  colorClass: string
}

const MOCK_STATS: MockStat[] = [
  { label: 'Turni questo mese', value: '142', change: '+12% rispetto al mese scorso', icon: '📅', colorClass: 'blue' },
  { label: 'Presenza media', value: '94%', change: '+2.3% rispetto al mese scorso', icon: '✅', colorClass: 'green' },
  { label: 'Richieste gestite', value: '28', change: '5 in attesa di approvazione', icon: '📋', colorClass: 'amber' },
]

export default function ReportPage() {
  return (
    <div className="report-page">
      <h1 className="page-title">Report</h1>

      <div className="report-notice">
        ⚠️ Report in sviluppo — i dati mostrati sono di esempio
      </div>

      <div className="stat-cards-grid">
        {MOCK_STATS.map(stat => (
          <div key={stat.label} className="report-stat-card">
            <div className="report-stat-header">
              <span className="report-stat-label">{stat.label}</span>
              <div className={`report-stat-icon ${stat.colorClass}`}>{stat.icon}</div>
            </div>
            <div className="report-stat-value">{stat.value}</div>
            <div className="report-stat-change">{stat.change}</div>
          </div>
        ))}
      </div>

      <div className="report-placeholder-card">
        <div className="report-placeholder-icon">📊</div>
        <h2 className="report-placeholder-title">Report avanzati in arrivo</h2>
        <p className="report-placeholder-text">
          Grafici, esportazione dati e analisi dettagliate saranno disponibili nella prossima versione.
        </p>
      </div>
    </div>
  )
}
