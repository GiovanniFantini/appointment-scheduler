import { useState, type ReactNode } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar, type NavSection, type NavItem } from './Sidebar'
import { TopHeader } from './TopHeader'
import { UserMenu, type UserMenuItem } from './UserMenu'
import './AppShell.css'

export type { NavItem, NavSection }

export interface AppShellProps {
  user: { firstName: string; lastName: string; email: string }
  /** Menu items del dropdown utente (Profilo, Cambia azienda, Preferenze, Logout). */
  userMenuItems: UserMenuItem[]
  /** Subtitle nel pannello del UserMenu (es. "Administrator"). */
  userMenuSubtitle?: string
  /** Sezioni della sidebar (un'unica sezione senza label, oppure più sezioni raggruppate). */
  navSections: NavSection[]
  /** Brand visualizzato in cima alla sidebar. */
  brand: { title: string; subtitle?: string; initial?: string }
  /** Titolo dinamico dell'header. */
  headerTitle?: string
  /** Nome azienda mostrato come chip nell'header. */
  companyName?: string
  /** Slot extra a sinistra del UserMenu (es. campanella notifiche). */
  headerExtras?: ReactNode
}

export function AppShell({
  user,
  userMenuItems,
  userMenuSubtitle,
  navSections,
  brand,
  headerTitle,
  companyName,
  headerExtras
}: AppShellProps) {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <div className="su-shell">
      <div
        className={`su-shell__overlay ${mobileOpen ? 'su-shell__overlay--visible' : ''}`}
        onClick={() => setMobileOpen(false)}
      />

      <Sidebar
        sections={navSections}
        brand={brand}
        open={mobileOpen}
        onClose={() => setMobileOpen(false)}
        onItemClick={() => setMobileOpen(false)}
      />

      <div className="su-shell__main">
        <TopHeader
          title={headerTitle}
          companyName={companyName}
          onHamburger={() => setMobileOpen(true)}
          actions={
            <>
              {headerExtras}
              <UserMenu user={user} items={userMenuItems} subtitle={userMenuSubtitle} />
            </>
          }
        />
        <main className="su-shell__content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
