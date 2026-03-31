import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { MerchantUser } from '../../App'
import './AppLayout.css'

interface AppLayoutProps {
  user: MerchantUser
  onLogout: () => void
}

interface NavItem {
  path: string
  label: string
  icon: string
  feature?: string
}

const ALL_NAV_ITEMS: NavItem[] = [
  { path: '/', label: 'Dashboard', icon: '⊞' },
  { path: '/calendario', label: 'Calendario', icon: '📅', feature: 'Calendario' },
  { path: '/richieste', label: 'Richieste', icon: '📋', feature: 'Richieste' },
  { path: '/risorse', label: 'Risorse', icon: '👥', feature: 'Risorse' },
  { path: '/ruoli', label: 'Ruoli', icon: '🔑', feature: 'Ruoli' },
  { path: '/documenti', label: 'Documenti', icon: '📁', feature: 'Documenti' },
  { path: '/report', label: 'Report', icon: '📊', feature: 'Report' },
]

export default function AppLayout({ user, onLogout }: AppLayoutProps) {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()

  const visibleItems = ALL_NAV_ITEMS.filter(item => {
    if (!item.feature) return true
    return user.activeFeatures?.includes(item.feature)
  })

  const currentPage = ALL_NAV_ITEMS.find(item => {
    if (item.path === '/') return location.pathname === '/'
    return location.pathname.startsWith(item.path)
  })

  const initials = `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()

  const toggleSidebar = () => {
    if (window.innerWidth <= 768) {
      setMobileOpen(prev => !prev)
    } else {
      setCollapsed(prev => !prev)
    }
  }

  const closeMobile = () => setMobileOpen(false)

  return (
    <div className="app-layout">
      <div
        className={`sidebar-overlay ${mobileOpen ? 'visible' : ''}`}
        onClick={closeMobile}
      />
      <aside className={`sidebar ${collapsed ? 'collapsed' : ''} ${mobileOpen ? 'mobile-open' : ''}`}>
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">M</div>
          <span className="sidebar-logo-text">Merchant App</span>
        </div>
        <nav className="sidebar-nav">
          <div className="nav-section-label">Navigazione</div>
          {visibleItems.map(item => (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.path === '/'}
              className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
              onClick={closeMobile}
            >
              <span className="nav-icon">{item.icon}</span>
              <span className="nav-label">{item.label}</span>
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="main-area">
        <header className="top-header">
          <button className="hamburger-btn" onClick={toggleSidebar} aria-label="Toggle menu">
            ☰
          </button>
          <div className="header-title">
            {currentPage?.label ?? 'Dashboard'}
          </div>
          {user.companyName && (
            <span className="header-company">{user.companyName}</span>
          )}
          <div className="header-user">
            <div className="user-avatar">{initials}</div>
            <span className="user-name">{user.firstName} {user.lastName}</span>
            <button className="logout-btn" onClick={onLogout}>Esci</button>
          </div>
        </header>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
