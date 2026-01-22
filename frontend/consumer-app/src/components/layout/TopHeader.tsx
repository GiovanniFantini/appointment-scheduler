interface TopHeaderProps {
  user: any
  onLogout: () => void
  pageTitle?: string
}

export default function TopHeader({ user, onLogout, pageTitle }: TopHeaderProps) {
  return (
    <header className="glass-card-dark border-b border-white/10 backdrop-blur-xl sticky top-0 z-40">
      <div className="px-4 lg:px-6 py-4">
        <div className="flex items-center justify-between">
          {/* Page Title - Desktop */}
          <div className="hidden lg:block">
            {pageTitle ? (
              <h1 className="text-xl font-bold gradient-text">{pageTitle}</h1>
            ) : (
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 bg-gradient-to-br from-neon-cyan to-neon-green rounded-lg flex items-center justify-center">
                  <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                </div>
                <span className="text-lg font-bold gradient-text">Consumer Dashboard</span>
              </div>
            )}
          </div>

          {/* Mobile Logo/Title */}
          <div className="lg:hidden flex items-center gap-2">
            <div className="w-8 h-8 bg-gradient-to-br from-neon-cyan to-neon-green rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <span className="font-bold gradient-text">Consumer Hub</span>
          </div>

          {/* Right Actions */}
          <div className="flex items-center gap-3">
            {/* User Info - Desktop */}
            <div className="hidden md:flex items-center gap-3">
              <div className="glass-card px-4 py-2 rounded-xl border border-neon-cyan/20">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 bg-gradient-to-br from-neon-cyan to-neon-green rounded-full flex items-center justify-center">
                    <span className="text-sm font-bold text-white">
                      {user.firstName?.[0]}{user.lastName?.[0]}
                    </span>
                  </div>
                  <div className="text-left">
                    <p className="text-sm font-semibold text-white">
                      {user.firstName} {user.lastName}
                    </p>
                    <p className="text-xs text-gray-400">Consumer</p>
                  </div>
                </div>
              </div>
            </div>

            {/* User Avatar - Mobile */}
            <div className="md:hidden w-9 h-9 bg-gradient-to-br from-neon-cyan to-neon-green rounded-full flex items-center justify-center border-2 border-neon-cyan/30">
              <span className="text-sm font-bold text-white">
                {user.firstName?.[0]}{user.lastName?.[0]}
              </span>
            </div>

            {/* Logout Button - Desktop */}
            <button
              onClick={onLogout}
              className="hidden lg:flex items-center gap-2 glass-card px-4 py-2 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              Esci
            </button>
          </div>
        </div>
      </div>
    </header>
  )
}
