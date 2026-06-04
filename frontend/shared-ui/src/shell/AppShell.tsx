import { useState, type ReactNode } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar, type NavSection, type NavItem } from './Sidebar'
import { TopHeader } from './TopHeader'
import { UserMenu, type UserMenuItem } from './UserMenu'
import { BottomNav, Fab } from './BottomNav'
import './AppShell.css'

export type { NavItem, NavSection }

export interface AppShellFab {
  icon: ReactNode
  label?: string
  onClick: () => void
}

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
  /** Se true, mostra una bottom-nav (mobile <768px) ricavata dalle navSections. */
  bottomNav?: boolean
  /** Se valorizzato, mostra un FAB in basso a destra (mobile <768px). */
  fab?: AppShellFab
}

export function AppShell({
  user,
  userMenuItems,
  userMenuSubtitle,
  navSections,
  brand,
  headerTitle,
  companyName,
  headerExtras,
  bottomNav = false,
  fab
}: AppShellProps) {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <div className={`su-shell ${bottomNav ? 'su-shell--has-bottomnav' : ''}`}>
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

      {bottomNav && (
        <BottomNav sections={navSections} onOverflowClick={() => setMobileOpen(true)} />
      )}
      {fab && <Fab icon={fab.icon} label={fab.label} onClick={fab.onClick} />}
    </div>
  )
}
