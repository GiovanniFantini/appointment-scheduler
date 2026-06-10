import './passwordRequirements.css'

/** Singola regola di policy password, valutata su una stringa. */
export interface PasswordRule {
  /** Etichetta mostrata all'utente. */
  label: string
  /** Predicato: true se la password soddisfa la regola. */
  test: (password: string) => boolean
}

/**
 * Regole della password — fonte unica lato frontend, allineata alla policy
 * backend (AuthService.ValidatePassword / ResetPasswordRequest):
 * ≥12 caratteri, almeno una maiuscola, una minuscola e una cifra.
 */
export const passwordRules: PasswordRule[] = [
  { label: 'Almeno 12 caratteri', test: (p) => p.length >= 12 },
  { label: 'Una lettera maiuscola', test: (p) => /[A-Z]/.test(p) },
  { label: 'Una lettera minuscola', test: (p) => /[a-z]/.test(p) },
  { label: 'Un numero', test: (p) => /\d/.test(p) },
]

/** True se la password soddisfa tutte le regole. */
export function passwordRulesMet(password: string): boolean {
  return passwordRules.every((rule) => rule.test(password))
}

/** Etichette in inglese, per le app non localizzate in italiano (es. admin). */
export const passwordRulesEn: Record<string, string> = {
  'Almeno 12 caratteri': 'At least 12 characters',
  'Una lettera maiuscola': 'One uppercase letter',
  'Una lettera minuscola': 'One lowercase letter',
  'Un numero': 'One number',
}

interface PasswordRequirementsProps {
  /** Password corrente da valutare. */
  password: string
  /**
   * Mappa opzionale etichetta-italiana → etichetta-tradotta. Se assente o senza
   * la chiave, viene usata l'etichetta italiana di default. Es. passare
   * `passwordRulesEn` nell'admin-app.
   */
  labels?: Record<string, string>
}

/**
 * Checklist live dei requisiti password: ogni regola si spunta in verde man
 * mano che la password digitata la soddisfa. Va mostrata sotto il campo
 * "nuova password" nei flussi di reset, cambio e registrazione.
 */
export function PasswordRequirements({ password, labels }: PasswordRequirementsProps) {
  return (
    <ul className="su-pwd-rules" aria-live="polite">
      {passwordRules.map((rule) => {
        const ok = rule.test(password)
        return (
          <li
            key={rule.label}
            className={`su-pwd-rules__item ${ok ? 'su-pwd-rules__item--ok' : ''}`}
          >
            <span className="su-pwd-rules__icon" aria-hidden="true">
              {ok ? '✓' : '○'}
            </span>
            <span>{labels?.[rule.label] ?? rule.label}</span>
          </li>
        )
      })}
    </ul>
  )
}
