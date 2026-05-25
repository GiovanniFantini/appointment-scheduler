import { useEffect, useRef, useState, type ReactNode } from 'react'
import { IconChevronDown } from '../icons'
import './UserMenu.css'

export interface UserMenuItem {
  /** Identificatore univoco — usato come key. */
  id: string
  label: string
  icon?: ReactNode
  /** Se true renderizza in rosso (es. Logout). */
  danger?: boolean
  /** Inserisce un separatore PRIMA di questo item. */
  separatorBefore?: boolean
  onClick: () => void
}

interface UserMenuProps {
  user: { firstName: string; lastName: string; email: string }
  items: UserMenuItem[]
  /** Label sotto il nome nel pannello (es. "Administrator" / "Manager"). */
  subtitle?: string
}

function initials(first: string, last: string): string {
  return `${first?.[0] ?? ''}${last?.[0] ?? ''}`.toUpperCase() || '?'
}

export function UserMenu({ user, items, subtitle }: UserMenuProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    function onDocClick(e: MouseEvent) {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false)
    }
    function onEsc(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    document.addEventListener('keydown', onEsc)
    return () => {
      document.removeEventListener('mousedown', onDocClick)
      document.removeEventListener('keydown', onEsc)
    }
  }, [open])

  return (
    <div className={`su-usermenu ${open ? 'su-usermenu--open' : ''}`} ref={rootRef}>
      <button
        type="button"
        className="su-usermenu__trigger"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <span className="su-usermenu__avatar" aria-hidden>
          {initials(user.firstName, user.lastName)}
        </span>
        <span className="su-usermenu__name">
          {user.firstName} {user.lastName}
        </span>
        <IconChevronDown size={14} className="su-usermenu__chev" />
      </button>

      {open && (
        <div className="su-usermenu__panel" role="menu">
          <div className="su-usermenu__header">
            <div className="su-usermenu__header-name">
              {user.firstName} {user.lastName}
            </div>
            <div className="su-usermenu__header-email">{user.email}</div>
            {subtitle && (
              <div className="su-usermenu__header-email" style={{ marginTop: 4, opacity: 0.7 }}>
                {subtitle}
              </div>
            )}
          </div>
          {items.map((item) => (
            <div key={item.id}>
              {item.separatorBefore && <div className="su-usermenu__sep" />}
              <button
                type="button"
                role="menuitem"
                className={`su-usermenu__item ${item.danger ? 'su-usermenu__item--danger' : ''}`}
                onClick={() => {
                  setOpen(false)
                  item.onClick()
                }}
              >
                {item.icon && <span aria-hidden>{item.icon}</span>}
                <span>{item.label}</span>
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
