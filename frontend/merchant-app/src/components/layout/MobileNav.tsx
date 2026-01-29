import { Link, useLocation } from 'react-router-dom'
import { useState } from 'react'

interface MobileNavProps {
  user: any
  onLogout: () => void
}

interface QuickNavItem {
  path: string
  label: string
  icon: JSX.Element
}

const quickNavItems: QuickNavItem[] = [
  {
    path: '/',
    label: 'Home',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
      </svg>
    )
  },
  {
    path: '/bookings',
    label: 'Prenotazioni',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    )
  },
  {
    path: '/services',
    label: 'Servizi',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
      </svg>
    )
  },
  {
    path: '/employees',
    label: 'Staff',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
    )
  }
]

const menuSections = [
  {
    title: 'Business',
    items: [
      { path: '/services', label: 'Servizi', icon: '🛍️' },
      { path: '/bookings', label: 'Prenotazioni', icon: '📅' }
    ]
  },
  {
    title: 'Staff & Turni',
    items: [
      { path: '/employees', label: 'Dipendenti', icon: '👥' },
      { path: '/shifts', label: 'Turni', icon: '📋' },
      { path: '/shift-templates', label: 'Template Turni', icon: '📝' },
      { path: '/availabilities', label: 'Disponibilità', icon: '✅' }
    ]
  },
  {
    title: 'Configurazione',
    items: [
      { path: '/business-hours', label: 'Orari', icon: '🕐' },
      { path: '/closures', label: 'Chiusure', icon: '🚫' }
    ]
  },
  {
    title: 'Risorse Umane',
    items: [
      { path: '/timbrature', label: 'Timbrature', icon: '⏱️' },
      { path: '/hr-documents', label: 'Documenti HR', icon: '📄' },
      { path: '/leave-requests', label: 'Richieste Ferie', icon: '📋' },
      { path: '/leave-balances', label: 'Saldi Ferie', icon: '📁' }
    ]
  },
  {
    title: 'Reportistica',
    items: [
      { path: '/reports', label: 'Report & Statistiche', icon: '📊' }
    ]
  }
]

export default function MobileNav({ user, onLogout }: MobileNavProps) {
  const location = useLocation()
  const [isMenuOpen, setIsMenuOpen] = useState(false)

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  return (
    <>
      {/* Bottom Navigation Bar - Mobile Only */}
      <nav className="lg:hidden fixed bottom-0 left-0 right-0 glass-card-dark border-t border-white/10 backdrop-blur-xl z-50 safe-area-inset-bottom">
        <div className="flex items-center justify-around px-2 py-3">
          {quickNavItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`flex flex-col items-center gap-1 px-3 py-2 rounded-xl transition-all ${
                isActive(item.path)
                  ? 'text-neon-cyan bg-neon-cyan/10'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              <div className={isActive(item.path) ? 'text-neon-cyan' : ''}>{item.icon}</div>
              <span className="text-xs font-medium">{item.label}</span>
              {isActive(item.path) && (
                <div className="w-1 h-1 bg-neon-cyan rounded-full animate-pulse"></div>
              )}
            </Link>
          ))}
          <button
            onClick={() => setIsMenuOpen(true)}
            className="flex flex-col items-center gap-1 px-3 py-2 rounded-xl text-gray-400 hover:text-white transition-all"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <span className="text-xs font-medium">Menu</span>
          </button>
        </div>
      </nav>

      {/* Full Screen Menu Overlay */}
      {isMenuOpen && (
        <div className="lg:hidden fixed inset-0 z-[100] animate-fade-in">
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-black/80 backdrop-blur-sm"
            onClick={() => setIsMenuOpen(false)}
          ></div>

          {/* Menu Content */}
          <div className="absolute inset-0 flex flex-col bg-gradient-dark">
            {/* Header */}
            <div className="glass-card-dark border-b border-white/10 p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-gradient-to-br from-neon-blue to-neon-purple rounded-xl flex items-center justify-center">
                  <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                  </svg>
                </div>
                <div>
                  <h2 className="font-bold gradient-text">Business Hub</h2>
                  <p className="text-xs text-gray-400">{user.firstName} {user.lastName}</p>
                </div>
              </div>
              <button
                onClick={() => setIsMenuOpen(false)}
                className="p-2 hover:bg-white/10 rounded-lg transition-all text-gray-400 hover:text-white"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Navigation Sections */}
            <div className="flex-1 overflow-y-auto p-4 space-y-6">
              {menuSections.map((section) => (
                <div key={section.title} className="animate-slide-up">
                  <h3 className="text-sm font-bold text-neon-cyan mb-3 px-2">{section.title}</h3>
                  <div className="space-y-1">
                    {section.items.map((item) => (
                      <Link
                        key={item.path}
                        to={item.path}
                        onClick={() => setIsMenuOpen(false)}
                        className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
                          isActive(item.path)
                            ? 'glass-card border border-neon-blue/30 text-white'
                            : 'glass-card-dark text-gray-400 hover:text-white hover:border-white/10'
                        }`}
                      >
                        <span className="text-2xl">{item.icon}</span>
                        <span className="font-medium">{item.label}</span>
                        {isActive(item.path) && (
                          <div className="ml-auto w-2 h-2 bg-neon-blue rounded-full animate-pulse"></div>
                        )}
                      </Link>
                    ))}
                  </div>
                </div>
              ))}

              {/* Admin Panel Link (if admin) */}
              {user.role === 'Admin' && (
                <div className="animate-slide-up">
                  <h3 className="text-sm font-bold text-neon-purple mb-3 px-2">Amministrazione</h3>
                  <Link
                    to="/admin"
                    onClick={() => setIsMenuOpen(false)}
                    className="flex items-center gap-3 px-4 py-3 rounded-xl glass-card border border-neon-purple/30 text-white"
                  >
                    <span className="text-2xl">⚡</span>
                    <span className="font-medium">Admin Panel</span>
                  </Link>
                </div>
              )}
            </div>

            {/* Footer Actions */}
            <div className="glass-card-dark border-t border-white/10 p-4 space-y-2">
              <button
                onClick={() => {
                  setIsMenuOpen(false)
                  onLogout()
                }}
                className="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 hover:bg-red-500/20 transition-all font-semibold"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                Esci
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
