import './DocumentiPage.css'

const PLANNED_FEATURES = [
  'Caricamento e gestione buste paga',
  'Documenti HR per dipendente',
  'Archivio contratti e accordi',
  'Firma digitale documenti',
  'Export PDF e integrazione payroll',
]

export default function DocumentiPage() {
  return (
    <div className="documenti-page">
      <h1 className="page-title">Documenti</h1>

      <div className="placeholder-card">
        <div className="placeholder-icon">📁</div>
        <h2 className="placeholder-title">Sistema documenti HR - Payroll</h2>
        <p className="placeholder-subtitle">
          Questa funzionalità è in sviluppo. Presto potrai gestire tutti i documenti aziendali direttamente dalla piattaforma.
        </p>
        <div className="placeholder-badge">
          🚧 In sviluppo
        </div>

        <div className="features-preview">
          <div className="features-preview-title">Funzionalità previste</div>
          {PLANNED_FEATURES.map(f => (
            <div key={f} className="feature-preview-item">
              <span className="feature-preview-dot" />
              {f}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
