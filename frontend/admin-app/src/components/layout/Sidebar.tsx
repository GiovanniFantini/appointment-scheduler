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
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
      </svg>
    ),
    items: [
      { path: '/', label: 'Panoramica', icon: <span className="text-neon-purple">●</span> }
    ]
  },
  {
    label: 'Gestione',
    icon: (
      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
      </svg>
    ),
    items: [
      {
        path: '/merchants',
        label: 'Merchant',
        icon: (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
        )
      }
    ]
  }
]

export default function Sidebar({ isCollapsed, onToggle }: SidebarProps) {
  const location = useLocation()
  const [expandedCategories, setExpandedCategories] = useState<string[]>(['Pannello', 'Gestione'])

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
              <div className="w-8 h-8 bg-gradient-to-br from-neon-purple to-neon-pink rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
              </div>
              <span className="font-bold text-lg gradient-text">Hub Amministratore</span>
            </div>
          )}
          <button
            onClick={onToggle}
            className="p-2 hover:bg-white/10 rounded-lg transition-all hover:text-neon-purple"
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
                    ? 'text-neon-purple bg-neon-purple/10'
                    : 'text-gray-400 hover:text-white hover:bg-white/5'
                }`}
                title={isCollapsed ? category.label : undefined}
              >
                <div className="flex items-center gap-3">
                  <div className={expandedCategories.includes(category.label) ? 'text-neon-purple' : ''}>
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
                          ? 'bg-gradient-to-r from-neon-purple/20 to-neon-pink/20 text-white border-l-2 border-neon-purple'
                          : 'text-gray-400 hover:text-white hover:bg-white/5 border-l-2 border-transparent'
                      }`}
                    >
                      <div
                        className={`transition-colors ${
                          isActive(item.path) ? 'text-neon-purple' : 'text-gray-500 group-hover:text-neon-purple'
                        }`}
                      >
                        {item.icon}
                      </div>
                      <span className="text-sm font-medium">{item.label}</span>
                      {isActive(item.path) && (
                        <div className="ml-auto w-2 h-2 bg-neon-purple rounded-full animate-pulse"></div>
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
                          ? 'bg-gradient-to-r from-neon-purple/20 to-neon-pink/20 text-neon-purple'
                          : 'text-gray-400 hover:text-white hover:bg-white/5'
                      }`}
                      title={item.label}
                    >
                      {item.icon}
                      {isActive(item.path) && (
                        <div className="absolute right-2 w-1.5 h-1.5 bg-neon-purple rounded-full animate-pulse"></div>
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
