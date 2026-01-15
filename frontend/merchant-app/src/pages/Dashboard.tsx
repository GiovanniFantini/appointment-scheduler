import { Link } from 'react-router-dom'

interface DashboardProps {
  user: any
  onLogout: () => void
}

/**
 * Dashboard principale per merchant
 */
function Dashboard({ user, onLogout }: DashboardProps) {
  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Futuristic Header */}
      <header className="glass-card border-b border-white/10 sticky top-0 z-40 backdrop-blur-xl">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold gradient-text flex items-center gap-2">
              <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
              Business Dashboard
            </h1>
            <div className="flex items-center gap-3">
              <div className="glass-card-dark px-4 py-2 rounded-xl border border-neon-green/30">
                <span className="text-gray-300">👤 <span className="text-neon-green font-semibold">{user.firstName} {user.lastName}</span></span>
              </div>
              {user.role === 'Admin' && (
                <Link
                  to="/admin"
                  className="bg-gradient-to-r from-neon-purple to-neon-pink text-white px-5 py-2.5 rounded-xl hover:shadow-glow-purple transition-all font-semibold transform hover:scale-105"
                >
                  ⚡ Admin Panel
                </Link>
              )}
              <button onClick={onLogout} className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10">
                Esci
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Background Animation */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-20 left-20 w-96 h-96 bg-neon-green opacity-10 rounded-full blur-3xl animate-float"></div>
        <div className="absolute bottom-20 right-20 w-96 h-96 bg-neon-cyan opacity-10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }}></div>
      </div>

      <div className="container mx-auto px-4 py-12 relative z-10">
        {/* Welcome Section */}
        <div className="mb-12 text-center animate-fade-in">
          <h2 className="text-5xl font-bold gradient-text mb-4">Benvenuto nel tuo Centro di Controllo</h2>
          <p className="text-xl text-gray-400">Gestisci il tuo business con efficienza e stile</p>
        </div>

        {/* Dashboard Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          <Link
            to="/services"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-blue transition-all border border-white/10 hover:border-neon-blue/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
          >
            <div className="bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-blue/30 group-hover:shadow-glow-blue transition-all">
              <svg className="w-8 h-8 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-blue transition-colors">I Miei Servizi</h2>
            <p className="text-gray-400">Gestisci i servizi che offri</p>
          </Link>

          <Link
            to="/bookings"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-purple transition-all border border-white/10 hover:border-neon-purple/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.1s' }}
          >
            <div className="bg-gradient-to-br from-neon-purple/20 to-neon-pink/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-purple/30 group-hover:shadow-glow-purple transition-all">
              <svg className="w-8 h-8 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-purple transition-colors">Prenotazioni</h2>
            <p className="text-gray-400">Visualizza e gestisci le prenotazioni</p>
          </Link>

          <Link
            to="/business-hours"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-cyan transition-all border border-white/10 hover:border-neon-cyan/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.2s' }}
          >
            <div className="bg-gradient-to-br from-neon-cyan/20 to-neon-blue/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-cyan/30 group-hover:shadow-glow-cyan transition-all">
              <svg className="w-8 h-8 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-cyan transition-colors">Gestione Disponibilità</h2>
            <p className="text-gray-400">Orari settimanali, eccezioni e chiusure</p>
          </Link>

          <Link
            to="/employees"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-cyan transition-all border border-white/10 hover:border-neon-green/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.3s' }}
          >
            <div className="bg-gradient-to-br from-neon-green/20 to-neon-cyan/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-green/30 group-hover:shadow-glow-cyan transition-all">
              <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-green transition-colors">Dipendenti</h2>
            <p className="text-gray-400">Gestisci turni, badge e staff</p>
          </Link>
        </div>

        {/* Info Panel */}
        <div className="glass-card rounded-3xl p-8 border border-white/10 animate-slide-up">
          <div className="flex items-center gap-4 mb-6">
            <div className="bg-gradient-to-br from-neon-blue/20 to-neon-purple/20 p-4 rounded-2xl border border-neon-blue/30">
              <svg className="w-8 h-8 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <h2 className="text-3xl font-bold gradient-text">Centro Informazioni</h2>
              <p className="text-gray-400">La tua guida rapida al dashboard</p>
            </div>
          </div>
          <div className="grid md:grid-cols-2 gap-6">
            <div className="glass-card-dark p-6 rounded-2xl border border-white/5">
              <h3 className="text-lg font-bold text-neon-cyan mb-2">🚀 Quick Start</h3>
              <p className="text-gray-400">
                Inizia configurando i tuoi servizi e la disponibilità. Poi gestisci le prenotazioni in arrivo.
              </p>
            </div>
            <div className="glass-card-dark p-6 rounded-2xl border border-white/5">
              <h3 className="text-lg font-bold text-neon-purple mb-2">💡 Suggerimento</h3>
              <p className="text-gray-400">
                Mantieni aggiornata la tua disponibilità per massimizzare le prenotazioni.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard
