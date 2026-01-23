import { Link, useLocation } from 'react-router-dom'
import { useState, useEffect } from 'react'

interface NavItem {
  path: string
  label: string
  icon: JSX.Element
}

interface NavCategory {
  label: string
  icon: JSX.Element
  items: NavItem[]
}

interface SidebarProps {
  isCollapsed: boolean
  onToggle: () => void
}

const navigationConfig: NavCategory[] = [
  {
    label: 'Pannello',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
      </svg>
    ),
    items: [
      {
        path: '/',
        label: 'Home',
        icon: <span className="text-neon-cyan">●</span>
      }
    ]
  },
  {
    label: 'Servizi',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
      </svg>
    ),
    items: [
      {
        path: '/',
        label: 'Prenota',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
        )
      },
      {
        path: '/',
        label: 'Catalogo',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
          </svg>
        )
      }
    ]
  },
  {
    label: 'Le Mie Prenotazioni',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    ),
    items: [
      {
        path: '/bookings',
        label: 'Prenotazioni',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
        )
      }
    ]
  }
]

export default function Sidebar({ isCollapsed, onToggle }: SidebarProps) {
  const location = useLocation()
  const [expandedCategories, setExpandedCategories] = useState<string[]>(['Pannello', 'Servizi'])

  const toggleCategory = (label: string) => {
    setExpandedCategories(prev =>
      prev.includes(label) ? prev.filter(l => l !== label) : [...prev, label]
    )
  }

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  // Auto-expand category containing active page
  useEffect(() => {
    // Find which category contains the active page
    for (const category of navigationConfig) {
      const hasActivePage = category.items.some(item => isActive(item.path))

      if (hasActivePage && !expandedCategories.includes(category.label)) {
        setExpandedCategories(prev => [...prev, category.label])
      }
    }
  }, [location.pathname])

  return (
    <>
      {/* Desktop Sidebar */}
      <aside
        className={`fixed left-0 top-0 h-screen glass-card-dark border-r border-white/10 backdrop-blur-xl z-50 transition-all duration-300 ease-in-out hidden lg:block ${
          isCollapsed ? 'w-20' : 'w-72'
        }`}
      >
        {/* Logo & Toggle */}
        <div className="h-16 flex items-center justify-between px-4 border-b border-white/10">
          {!isCollapsed && (
            <div className="flex items-center gap-2 animate-fade-in">
              <div className="w-8 h-8 bg-gradient-to-br from-neon-cyan to-neon-green rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
              </div>
              <span className="font-bold text-lg gradient-text">Area Cliente</span>
            </div>
          )}
          <button
            onClick={onToggle}
            className="p-2 hover:bg-white/10 rounded-lg transition-all hover:text-neon-cyan"
          >
            <svg
              className={`w-5 h-5 transition-transform duration-300 ${isCollapsed ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 19l-7-7 7-7m8 14l-7-7 7-7" />
            </svg>
          </button>
        </div>

        {/* Navigation */}
        <nav className="overflow-y-auto h-[calc(100vh-4rem)] py-4 px-2 scrollbar-thin scrollbar-thumb-white/10 scrollbar-track-transparent">
          {navigationConfig.map((category) => (
            <div key={category.label} className="mb-2">
              {/* Category Header */}
              <button
                onClick={() => toggleCategory(category.label)}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all ${
                  isCollapsed ? 'justify-center' : 'justify-between'
                } ${
                  expandedCategories.includes(category.label)
                    ? 'text-neon-cyan bg-neon-cyan/10'
                    : 'text-gray-400 hover:text-white hover:bg-white/5'
                }`}
                title={isCollapsed ? category.label : undefined}
              >
                <div className="flex items-center gap-3">
                  <div className={expandedCategories.includes(category.label) ? 'text-neon-cyan' : ''}>
                    {category.icon}
                  </div>
                  {!isCollapsed && (
                    <span className="font-semibold text-sm">{category.label}</span>
                  )}
                </div>
                {!isCollapsed && (
                  <svg
                    className={`w-4 h-4 transition-transform duration-200 ${
                      expandedCategories.includes(category.label) ? 'rotate-90' : ''
                    }`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                  </svg>
                )}
              </button>

              {/* Category Items */}
              {expandedCategories.includes(category.label) && !isCollapsed && (
                <div className="ml-2 mt-1 space-y-1 animate-slide-down">
                  {category.items.map((item) => (
                    <Link
                      key={item.path}
                      to={item.path}
                      className={`flex items-center gap-3 px-3 py-2 rounded-lg transition-all group ${
                        isActive(item.path)
                          ? 'bg-gradient-to-r from-neon-cyan/20 to-neon-green/20 text-white border-l-2 border-neon-cyan'
                          : 'text-gray-400 hover:text-white hover:bg-white/5 border-l-2 border-transparent'
                      }`}
                    >
                      <div
                        className={`transition-colors ${
                          isActive(item.path) ? 'text-neon-cyan' : 'text-gray-500 group-hover:text-neon-cyan'
                        }`}
                      >
                        {item.icon}
                      </div>
                      <span className="text-sm font-medium">{item.label}</span>
                      {isActive(item.path) && (
                        <div className="ml-auto w-2 h-2 bg-neon-cyan rounded-full animate-pulse"></div>
                      )}
                    </Link>
                  ))}
                </div>
              )}

              {/* Collapsed Icons Only */}
              {isCollapsed && expandedCategories.includes(category.label) && (
                <div className="mt-1 space-y-1">
                  {category.items.map((item) => (
                    <Link
                      key={item.path}
                      to={item.path}
                      className={`flex items-center justify-center p-2 rounded-lg transition-all ${
                        isActive(item.path)
                          ? 'bg-gradient-to-r from-neon-cyan/20 to-neon-green/20 text-neon-cyan'
                          : 'text-gray-400 hover:text-white hover:bg-white/5'
                      }`}
                      title={item.label}
                    >
                      {item.icon}
                      {isActive(item.path) && (
                        <div className="absolute right-2 w-1.5 h-1.5 bg-neon-cyan rounded-full animate-pulse"></div>
                      )}
                    </Link>
                  ))}
                </div>
              )}
            </div>
          ))}
        </nav>
      </aside>
    </>
  )
}
