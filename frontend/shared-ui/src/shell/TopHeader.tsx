import type { ReactNode } from 'react'
import { IconMenu } from '../icons'

interface TopHeaderProps {
  title?: string
  companyName?: string
  onHamburger: () => void
  menuOpen?: boolean
  /** Slot azioni a destra (es. bell, UserMenu). */
  actions?: ReactNode
}

export function TopHeader({ title, companyName, onHamburger, menuOpen = false, actions }: TopHeaderProps) {
  return (
    <header className="su-header">
      <button data-activity="shared-ui.shell.TopHeader.1"
        type="button"
        className="su-header__hamburger"
        onClick={onHamburger}
        aria-label="Apri menu"
        aria-expanded={menuOpen}
        aria-controls="app-navigation"
      >
        <IconMenu size={20} />
      </button>

      <div className="su-header__title">{title}</div>

      {companyName && <span className="su-header__company">{companyName}</span>}

      <div className="su-header__actions">{actions}</div>
    </header>
  )
}
