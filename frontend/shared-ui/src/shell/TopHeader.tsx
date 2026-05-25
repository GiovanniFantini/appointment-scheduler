import type { ReactNode } from 'react'
import { IconMenu } from '../icons'

interface TopHeaderProps {
  title?: string
  companyName?: string
  onHamburger: () => void
  /** Slot azioni a destra (es. bell, UserMenu). */
  actions?: ReactNode
}

export function TopHeader({ title, companyName, onHamburger, actions }: TopHeaderProps) {
  return (
    <header className="su-header">
      <button
        type="button"
        className="su-header__hamburger"
        onClick={onHamburger}
        aria-label="Apri menu"
      >
        <IconMenu size={20} />
      </button>

      <div className="su-header__title">{title}</div>

      {companyName && <span className="su-header__company">{companyName}</span>}

      <div className="su-header__actions">{actions}</div>
    </header>
  )
}
