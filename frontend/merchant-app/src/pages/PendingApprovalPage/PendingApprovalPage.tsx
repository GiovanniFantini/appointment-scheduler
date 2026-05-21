import { MerchantUser } from '../../App'
import './PendingApprovalPage.css'

interface PendingApprovalPageProps {
  user: MerchantUser
  onLogout: () => void
}

/**
 * Schermata mostrata quando l'azienda non è operativa: in attesa della prima
 * approvazione dell'admin, oppure disattivata da un admin. Il merchant può
 * autenticarsi ma non operare: tutte le rotte applicative sono sostituite da
 * questa pagina finché il backend non conferma che l'azienda è operativa.
 */
export default function PendingApprovalPage({ user, onLogout }: PendingApprovalPageProps) {
  return (
    <div className="pending-page">
      <div className="pending-card">
        <div className="pending-icon">⏳</div>
        <h1 className="pending-title">Azienda non ancora attiva</h1>
        <p className="pending-message">
          L'account di <strong>{user.companyName ?? 'la tua azienda'}</strong> non è
          al momento operativo. Un amministratore deve attivarlo prima che tu possa
          usare il gestionale.
        </p>
        <p className="pending-hint">
          Riceverai accesso completo non appena l'azienda sarà attivata. Riprova ad
          accedere più tardi.
        </p>
        <button type="button" className="pending-logout" onClick={onLogout}>
          Esci
        </button>
      </div>
    </div>
  )
}
