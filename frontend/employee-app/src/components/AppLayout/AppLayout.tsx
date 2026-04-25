import { useState, useEffect } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { EmployeeUser } from '../../App'
import apiClient from '../../lib/axios'
import './AppLayout.css'

interface Props {
  user: EmployeeUser
  onLogout: () => void
  onUserUpdate: (user: EmployeeUser, token: string) => void
}

interface NavItem {
  to: string
  label: string
  feature?: string
  icon: React.ReactNode
}

function CalendarIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <rect x="3" y="4" width="18" height="18" rx="2" stroke="currentColor" strokeWidth="2" />
      <path d="M16 2v4M8 2v4M3 10h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
function RequestsIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
function DocumentsIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
function BellIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}
function HomeIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M9 22V12h6v10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}
function LogoutIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}
function SwitchIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none">
      <path d="M17 1l4 4-4 4M3 11V9a4 4 0 014-4h14M7 23l-4-4 4-4M21 13v2a4 4 0 01-4 4H3" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export default function AppLayout({ user, onLogout, onUserUpdate }: Props) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [unreadCount, setUnreadCount] = useState(0)
  const navigate = useNavigate()

  useEffect(() => {
    fetchUnreadCount()
    const interval = setInterval(fetchUnreadCount, 60000)
    return () => clearInterval(interval)
  }, [])

  const fetchUnreadCount = async () => {
    try {
      const { data } = await apiClient.get<{ unreadCount: number }>('/notifications/summary')
      setUnreadCount(data.unreadCount ?? 0)
    } catch {
      // silently fail
    }
  }

  const navItems: NavItem[] = [
    { to: '/', label: 'Dashboard', icon: <HomeIcon /> },
    { to: '/calendario', label: 'Calendario', feature: 'Calendario', icon: <CalendarIcon /> },
    { to: '/richieste', label: 'Richieste', feature: 'Richieste', icon: <RequestsIcon /> },
    { to: '/documenti', label: 'Documenti', feature: 'Documenti', icon: <DocumentsIcon /> },
    { to: '/notifiche', label: 'Notifiche', icon: <BellIcon /> },
  ]

  const visibleNavItems = navItems.filter(item =>
    !item.feature || user.activeFeatures?.includes(item.feature)
  )

  const handleSwitchCompany = () => {
    // Clear merchantId from user to force company selection
    const userWithoutMerchant = { ...user, merchantId: undefined, companyName: undefined, activeFeatures: [] }
    onUserUpdate(userWithoutMerchant, localStorage.getItem('token') ?? '')
    navigate('/select-company')
  }

  const closeSidebar = () => setSidebarOpen(false)

  return (
    <div className="app-layout">
      {/* Overlay for mobile */}
      {sidebarOpen && <div className="sidebar-overlay" onClick={closeSidebar} />}

      {/* Sidebar */}
      <aside className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''}`}>
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-brand-icon">
              {user.companyName ? user.companyName.charAt(0).toUpperCase() : 'E'}
            </div>
            <div className="sidebar-brand-text">
              <span className="sidebar-brand-name">{user.companyName ?? 'Employee Portal'}</span>
              <span className="sidebar-company-badge">Area dipendente</span>
            </div>
          </div>
          <button className="sidebar-close" onClick={closeSidebar}>
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
          </button>
        </div>

        <nav className="sidebar-nav">
          {visibleNavItems.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) => `sidebar-nav-item ${isActive ? 'sidebar-nav-item--active' : ''}`}
              onClick={closeSidebar}
            >
              <span className="sidebar-nav-icon">{item.icon}</span>
              <span className="sidebar-nav-label">{item.label}</span>
              {item.to === '/notifiche' && unreadCount > 0 && (
                <span className="sidebar-badge">{unreadCount}</span>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="sidebar-user-avatar">
              {user.firstName.charAt(0)}{user.lastName.charAt(0)}
            </div>
            <div className="sidebar-user-info">
              <span className="sidebar-user-name">{user.firstName} {user.lastName}</span>
              <span className="sidebar-user-email">{user.email}</span>
            </div>
          </div>

          {user.companies.length > 1 && (
            <button className="sidebar-action-btn" onClick={handleSwitchCompany}>
              <SwitchIcon />
              <span>Cambia azienda</span>
            </button>
          )}

          <button className="sidebar-action-btn sidebar-action-btn--logout" onClick={onLogout}>
            <LogoutIcon />
            <span>Esci</span>
          </button>
        </div>
      </aside>

      {/* Main area */}
      <div className="main-wrapper">
        {/* Top header */}
        <header className="top-header">
          <button className="hamburger" onClick={() => setSidebarOpen(true)}>
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M3 12h18M3 6h18M3 18h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
          </button>

          <div className="header-title">
            {user.companyName && <span className="header-company">{user.companyName}</span>}
          </div>

          <div className="header-actions">
            <NavLink to="/notifiche" className="header-bell">
              <BellIcon />
              {unreadCount > 0 && <span className="header-bell-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>}
            </NavLink>

            <div className="header-user">
              <div className="header-user-avatar">
                {user.firstName.charAt(0)}{user.lastName.charAt(0)}
              </div>
              <span className="header-user-name">{user.firstName} {user.lastName}</span>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
