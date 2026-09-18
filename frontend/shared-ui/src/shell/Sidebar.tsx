import { useEffect, useRef, type ReactNode } from 'react'
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
  const sidebarRef = useRef<HTMLElement>(null)
  const closeRef = useRef(onClose)
  closeRef.current = onClose

  useEffect(() => {
    if (!open) return
    const previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const sidebar = sidebarRef.current
    sidebar?.querySelector<HTMLButtonElement>('button')?.focus()
    function handleKey(event: KeyboardEvent) {
      if (event.key === 'Escape') closeRef.current()
      if (event.key !== 'Tab' || !sidebar) return
      const items = Array.from(sidebar.querySelectorAll<HTMLElement>('a[href], button:not(:disabled)'))
      const first = items[0]
      const last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last?.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first?.focus()
      }
    }
    document.addEventListener('keydown', handleKey)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKey)
      if (previousFocus?.isConnected) previousFocus.focus()
    }
  }, [open])

  return (
    <aside ref={sidebarRef} id="app-navigation" className={`su-sidebar ${open ? 'su-sidebar--open' : ''}`}
      role={open ? 'dialog' : undefined} aria-modal={open ? true : undefined} aria-label="Menu principale">
      <div className="su-sidebar__brand">
        <div className="su-sidebar__brand-icon" aria-hidden>
          {brand.initial ?? brand.title.charAt(0).toUpperCase()}
        </div>
        <div className="su-sidebar__brand-text">
          <span className="su-sidebar__brand-name">{brand.title}</span>
          {brand.subtitle && <span className="su-sidebar__brand-sub">{brand.subtitle}</span>}
        </div>
        <button data-activity="shared-ui.shell.Sidebar.1" type="button" className="su-sidebar__close" onClick={onClose} aria-label="Chiudi menu">
          <IconClose size={18} />
        </button>
      </div>

      <nav className="su-sidebar__nav" aria-label="Navigazione principale">
        {sections.map((section, sIdx) => {
          const visibleItems = section.items.filter((i) => i.visible !== false)
          if (visibleItems.length === 0) return null
          return (
            <div key={`s-${sIdx}`}>
              {section.label && <div className="su-sidebar__section-label">{section.label}</div>}
              {visibleItems.map((item) => (
                <NavLink data-activity="shared-ui.shell.Sidebar.2"
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
