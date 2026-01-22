import { useState, ReactNode } from 'react'
import Sidebar from './Sidebar'
import TopHeader from './TopHeader'
import MobileNav from './MobileNav'

interface AppLayoutProps {
  user: any
  onLogout: () => void
  children: ReactNode
  pageTitle?: string
}

export default function AppLayout({ user, onLogout, children, pageTitle }: AppLayoutProps) {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Sidebar - Desktop Only */}
      <Sidebar isCollapsed={isSidebarCollapsed} onToggle={() => setIsSidebarCollapsed(!isSidebarCollapsed)} />

      {/* Main Content Area */}
      <div
        className={`min-h-screen transition-all duration-300 ${
          isSidebarCollapsed ? 'lg:ml-20' : 'lg:ml-72'
        }`}
      >
        {/* Top Header */}
        <TopHeader user={user} onLogout={onLogout} pageTitle={pageTitle} />

        {/* Page Content */}
        <main className="pb-20 lg:pb-0">
          {/* Background Animation */}
          <div className="fixed inset-0 pointer-events-none overflow-hidden">
            <div className="absolute top-20 left-20 w-96 h-96 bg-neon-green opacity-10 rounded-full blur-3xl animate-float"></div>
            <div
              className="absolute bottom-20 right-20 w-96 h-96 bg-neon-cyan opacity-10 rounded-full blur-3xl animate-float"
              style={{ animationDelay: '2s' }}
            ></div>
            <div
              className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-neon-purple opacity-5 rounded-full blur-3xl animate-float"
              style={{ animationDelay: '4s' }}
            ></div>
          </div>

          {/* Content Container */}
          <div className="relative z-10">
            {children}
          </div>
        </main>
      </div>

      {/* Mobile Navigation - Bottom Bar */}
      <MobileNav user={user} onLogout={onLogout} />
    </div>
  )
}
