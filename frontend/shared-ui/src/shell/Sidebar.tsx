import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { IconClose } from '../icons'

export interface NavItem {
  path: string
  label: string
  icon?: ReactNode
  /** Se valorizzato, l'item è visibile solo se la funzione ritorna true (es. feature flag). */
  visible?: boolean
  /** Badge numerico (es. notifiche non lette). */
  badge?: number
  /** Match esatto (default true se path === '/'). */
  exact?: boolean
}

export interface NavSection {
  label?: string
  items: NavItem[]
}

interface SidebarProps {
  sections: NavSection[]
  brand: {
    title: string
    subtitle?: string
    initial?: string
  }
  open: boolean
  onClose: () => void
  onItemClick?: () => void
}

export function Sidebar({ sections, brand, open, onClose, onItemClick }: SidebarProps) {
  return (
    <aside className={`su-sidebar ${open ? 'su-sidebar--open' : ''}`}>
      <div className="su-sidebar__brand">
        <div className="su-sidebar__brand-icon" aria-hidden>
          {brand.initial ?? brand.title.charAt(0).toUpperCase()}
        </div>
        <div className="su-sidebar__brand-text">
          <span className="su-sidebar__brand-name">{brand.title}</span>
          {brand.subtitle && <span className="su-sidebar__brand-sub">{brand.subtitle}</span>}
        </div>
        <button type="button" className="su-sidebar__close" onClick={onClose} aria-label="Chiudi menu">
          <IconClose size={18} />
        </button>
      </div>

      <nav className="su-sidebar__nav">
        {sections.map((section, sIdx) => {
          const visibleItems = section.items.filter((i) => i.visible !== false)
          if (visibleItems.length === 0) return null
          return (
            <div key={`s-${sIdx}`}>
              {section.label && <div className="su-sidebar__section-label">{section.label}</div>}
              {visibleItems.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end={item.exact ?? item.path === '/'}
                  className={({ isActive }) =>
                    `su-sidebar__item ${isActive ? 'su-sidebar__item--active' : ''}`
                  }
                  onClick={onItemClick}
                >
                  {item.icon && <span className="su-sidebar__icon" aria-hidden>{item.icon}</span>}
                  <span className="su-sidebar__label">{item.label}</span>
                  {item.badge !== undefined && item.badge > 0 && (
                    <span className="su-sidebar__badge">{item.badge > 99 ? '99+' : item.badge}</span>
                  )}
                </NavLink>
              ))}
            </div>
          )
        })}
      </nav>
    </aside>
  )
}
