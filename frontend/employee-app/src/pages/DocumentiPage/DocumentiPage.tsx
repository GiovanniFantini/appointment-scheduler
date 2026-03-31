import './DocumentiPage.css'

export default function DocumentiPage() {
  return (
    <div className="documenti-page">
      <div className="documenti-header">
        <h1 className="documenti-title">I miei documenti HR</h1>
      </div>

      <div className="documenti-empty">
        <div className="empty-illustration">
          <svg viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect width="80" height="80" rx="20" fill="rgba(99,102,241,0.08)" />
            <path
              d="M50 20H28a4 4 0 00-4 4v32a4 4 0 004 4h24a4 4 0 004-4V30l-6-10z"
              stroke="#6366f1"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <path
              d="M50 20v10h10M35 40h10M35 46h10M35 52h6"
              stroke="#6366f1"
              strokeWidth="2"
              strokeLinecap="round"
            />
          </svg>
        </div>
        <h2 className="empty-title">Nessun documento disponibile</h2>
        <p className="empty-subtitle">
          I documenti verranno pubblicati dal tuo responsabile.
          <br />
          Verrai notificato quando saranno disponibili.
        </p>
      </div>
    </div>
  )
}
