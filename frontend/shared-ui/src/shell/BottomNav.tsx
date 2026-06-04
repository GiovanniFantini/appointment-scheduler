import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { TiMenu2 } from '../icons/tabler'
import type { NavSection } from './Sidebar'
import './BottomNav.css'

interface BottomNavProps {
  sections: NavSection[]
  /** Numero massimo di voci dirette prima della voce "Altro". */
  maxItems?: number
  /** Apre il drawer/sidebar per le voci in overflow. */
  onOverflowClick: () => void
}

/**
 * Barra di navigazione inferiore (mobile, <768px). Appiattisce le navSections,
 * mostra le prime N voci visibili + "Altro" che apre il drawer esistente per
 * il resto. È puramente presentazionale: stessi dati nav della sidebar.
 */
export function BottomNav({ sections, maxItems = 4, onOverflowClick }: BottomNavProps) {
  const allItems = sections.flatMap((s) => s.items.filter((i) => i.visible !== false))
  const hasOverflow = allItems.length > maxItems
  const direct = hasOverflow ? allItems.slice(0, maxItems) : allItems.slice(0, maxItems + 1)

  return (
    <nav className="su-bottomnav" aria-label="Navigazione">
      {direct.map((item) => (
        <NavLink
          key={item.path}
          to={item.path}
          end={item.exact ?? item.path === '/'}
          className={({ isActive }) =>
            `su-bottomnav__item ${isActive ? 'su-bottomnav__item--active' : ''}`
          }
        >
          <span className="su-bottomnav__icon" aria-hidden>
            {item.icon}
            {item.badge !== undefined && item.badge > 0 && (
              <span className="su-bottomnav__badge">{item.badge > 99 ? '99+' : item.badge}</span>
            )}
          </span>
          <span className="su-bottomnav__label">{item.label}</span>
        </NavLink>
      ))}
      {hasOverflow && (
        <button type="button" className="su-bottomnav__item" onClick={onOverflowClick}>
          <span className="su-bottomnav__icon" aria-hidden>
            <TiMenu2 size={20} />
          </span>
          <span className="su-bottomnav__label">Altro</span>
        </button>
      )}
    </nav>
  )
}

interface FabProps {
  icon: ReactNode
  label?: string
  onClick: () => void
}

/** Floating Action Button (mobile, <768px) — es. "nuovo turno". */
export function Fab({ icon, label, onClick }: FabProps) {
  return (
    <button type="button" className="su-fab" onClick={onClick} aria-label={label ?? 'Aggiungi'}>
      {icon}
    </button>
  )
}
