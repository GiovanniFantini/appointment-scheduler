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
    label: 'Dashboard',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
      </svg>
    ),
    items: [
      { path: '/', label: 'Home', icon: <span className="text-neon-cyan">●</span> }
    ]
  },
  {
    label: 'Il Mio Lavoro',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
      </svg>
    ),
    items: [
      {
        path: '/timbratura',
        label: 'Timbratura',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        )
      },
      {
        path: '/my-shifts',
        label: 'I Miei Turni',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        )
      },
      {
        path: '/leave-requests',
        label: 'Ferie e Permessi',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
          </svg>
        )
      }
    ]
  },
  {
    label: 'Team',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
    ),
    items: [
      {
        path: '/colleagues',
        label: 'Colleghi',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
        )
      }
    ]
  },
  {
    label: 'Documenti',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
    ),
    items: [
      {
        path: '/documents',
        label: 'I Miei Documenti',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
        )
      }
    ]
  }
]

export default function Sidebar({ isCollapsed, onToggle }: SidebarProps) {
  const location = useLocation()
  const [expandedCategories, setExpandedCategories] = useState<string[]>(['Dashboard', 'Il Mio Lavoro'])

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
              <div className="w-8 h-8 bg-gradient-to-br from-neon-green to-neon-cyan rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>
              <span className="font-bold text-lg gradient-text">Area Dipendenti</span>
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
                          ? 'bg-gradient-to-r from-neon-blue/20 to-neon-purple/20 text-white border-l-2 border-neon-blue'
                          : 'text-gray-400 hover:text-white hover:bg-white/5 border-l-2 border-transparent'
                      }`}
                    >
                      <div
                        className={`transition-colors ${
                          isActive(item.path) ? 'text-neon-blue' : 'text-gray-500 group-hover:text-neon-cyan'
                        }`}
                      >
                        {item.icon}
                      </div>
                      <span className="text-sm font-medium">{item.label}</span>
                      {isActive(item.path) && (
                        <div className="ml-auto w-2 h-2 bg-neon-blue rounded-full animate-pulse"></div>
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
                          ? 'bg-gradient-to-r from-neon-blue/20 to-neon-purple/20 text-neon-blue'
                          : 'text-gray-400 hover:text-white hover:bg-white/5'
                      }`}
                      title={item.label}
                    >
                      {item.icon}
                      {isActive(item.path) && (
                        <div className="absolute right-2 w-1.5 h-1.5 bg-neon-blue rounded-full animate-pulse"></div>
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
