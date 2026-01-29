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
    path: '/merchants',
    label: 'Esercenti',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
      </svg>
    )
  }
]

const menuSections = [
  {
    title: 'Gestione',
    items: [
      { path: '/merchants', label: 'Merchant', icon: '🏢' }
    ]
  },
  {
    title: 'Reportistica',
    items: [
      { path: '/reports', label: 'Report Globali', icon: '📊' }
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
                  ? 'text-neon-purple bg-neon-purple/10'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              <div className={isActive(item.path) ? 'text-neon-purple' : ''}>{item.icon}</div>
              <span className="text-xs font-medium">{item.label}</span>
              {isActive(item.path) && (
                <div className="w-1 h-1 bg-neon-purple rounded-full animate-pulse"></div>
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
                <div className="w-10 h-10 bg-gradient-to-br from-neon-purple to-neon-pink rounded-xl flex items-center justify-center">
                  <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                </div>
                <div>
                  <h2 className="font-bold gradient-text">Hub Amministratore</h2>
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
                  <h3 className="text-sm font-bold text-neon-purple mb-3 px-2">{section.title}</h3>
                  <div className="space-y-1">
                    {section.items.map((item) => (
                      <Link
                        key={item.path}
                        to={item.path}
                        onClick={() => setIsMenuOpen(false)}
                        className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
                          isActive(item.path)
                            ? 'glass-card border border-neon-purple/30 text-white'
                            : 'glass-card-dark text-gray-400 hover:text-white hover:border-white/10'
                        }`}
                      >
                        <span className="text-2xl">{item.icon}</span>
                        <span className="font-medium">{item.label}</span>
                        {isActive(item.path) && (
                          <div className="ml-auto w-2 h-2 bg-neon-purple rounded-full animate-pulse"></div>
                        )}
                      </Link>
                    ))}
                  </div>
                </div>
              ))}
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
