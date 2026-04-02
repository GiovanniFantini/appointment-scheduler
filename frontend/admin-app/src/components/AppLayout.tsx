import { useState, ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import './AppLayout.css'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  // 1 = admin, 2 = consumer, 3 = merchant, 4 = employee
  accountType: number
}

interface AppLayoutProps {
  user: User
  onLogout: () => void
  children: ReactNode
  pageTitle?: string
}

const navItems = [
  {
    section: 'Overview',
    links: [
      {
        path: '/',
        label: 'Dashboard',
        exact: true,
        icon: (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <rect x="3" y="3" width="7" height="7" rx="1" />
            <rect x="14" y="3" width="7" height="7" rx="1" />
            <rect x="3" y="14" width="7" height="7" rx="1" />
            <rect x="14" y="14" width="7" height="7" rx="1" />
          </svg>
        ),
      },
    ],
  },
  {
    section: 'Management',
    links: [
      {
        path: '/merchants',
        label: 'Merchants',
        exact: false,
        icon: (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
            <polyline points="9 22 9 12 15 12 15 22" />
          </svg>
        ),
      },
      {
        path: '/users',
        label: 'Users',
        exact: false,
        icon: (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
          </svg>
        ),
      },
    ],
  },
  {
    section: 'Analytics',
    links: [
      {
        path: '/reports',
        label: 'Reports',
        exact: false,
        icon: (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
            <line x1="2" y1="20" x2="22" y2="20" />
          </svg>
        ),
      },
    ],
  },
  {
    section: 'Developer',
    links: [
      {
        path: '/debug',
        label: 'Debug',
        exact: false,
        icon: (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <path d="M12 22c4.97 0 9-4.03 9-9H3c0 4.97 4.03 9 9 9z" />
            <path d="M12 13V2" />
            <path d="M5 9l7-7 7 7" />
            <path d="M3 13h3M18 13h3" />
          </svg>
        ),
      },
    ],
  },
]

export default function AppLayout({ user, onLogout, children, pageTitle }: AppLayoutProps) {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const navigate = useNavigate()

  const handleLogout = () => {
    onLogout()
    navigate('/login')
  }

  const initials = `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()

  const sidebarClass = [
    'sidebar',
    collapsed ? 'collapsed' : '',
    mobileOpen ? 'mobile-open' : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className="app-layout">
      {/* Sidebar overlay (mobile) */}
      {mobileOpen && (
        <div
          className="sidebar-overlay"
          style={{ display: 'block' }}
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={sidebarClass}>
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
          </div>
          <span className="sidebar-logo-text">Admin Hub</span>
          <button
            className="sidebar-toggle"
            onClick={() => setCollapsed(!collapsed)}
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>
        </div>

        <nav className="sidebar-nav">
          {navItems.map((section) => (
            <div key={section.section}>
              <div className="nav-section-label">{section.section}</div>
              {section.links.map((link) => (
                <NavLink
                  key={link.path}
                  to={link.path}
                  end={link.exact}
                  className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
                  onClick={() => setMobileOpen(false)}
                  title={collapsed ? link.label : undefined}
                >
                  {link.icon}
                  <span className="nav-link-label">{link.label}</span>
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
      </aside>

      {/* Main area */}
      <div className={`main-area${collapsed ? ' sidebar-collapsed' : ''}`}>
        {/* Top header */}
        <header className="top-header">
          <button
            className="mobile-menu-btn"
            onClick={() => setMobileOpen(!mobileOpen)}
            aria-label="Toggle menu"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <line x1="3" y1="6" x2="21" y2="6" />
              <line x1="3" y1="12" x2="21" y2="12" />
              <line x1="3" y1="18" x2="21" y2="18" />
            </svg>
          </button>

          <div className="top-header-title">{pageTitle ?? 'Admin Hub'}</div>

          <div className="top-header-user">
            <div className="user-avatar">{initials}</div>
            <div>
              <div className="user-name">{user.firstName} {user.lastName}</div>
              <div className="user-role">Administrator</div>
            </div>
          </div>

          <button className="btn-logout" onClick={handleLogout}>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
              <polyline points="16 17 21 12 16 7" />
              <line x1="21" y1="12" x2="9" y2="12" />
            </svg>
            <span className="btn-logout-label">Logout</span>
          </button>
        </header>

        {/* Page content */}
        <main className="page-content">
          {children}
        </main>
      </div>
    </div>
  )
}
