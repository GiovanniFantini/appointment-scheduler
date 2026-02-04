import { Link } from 'react-router-dom'
import AppLayout from '../components/layout/AppLayout'

interface DashboardProps {
  user: any
  onLogout: () => void
}

/**
 * Dashboard principale per merchant
 */
function Dashboard({ user, onLogout }: DashboardProps) {
  return (
    <AppLayout user={user} onLogout={onLogout}>
      <div className="container mx-auto px-4 py-8">
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
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-cyan transition-colors">Orari</h2>
            <p className="text-gray-400">Configura orari settimanali</p>
          </Link>

          <Link
            to="/closures"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-pink transition-all border border-white/10 hover:border-red-500/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.3s' }}
          >
            <div className="bg-gradient-to-br from-red-500/20 to-neon-pink/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-red-500/30 group-hover:shadow-glow-pink transition-all">
              <svg className="w-8 h-8 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-red-400 transition-colors">Chiusure</h2>
            <p className="text-gray-400">Gestisci ferie e festività</p>
          </Link>

          <Link
            to="/availabilities"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-green transition-all border border-white/10 hover:border-neon-green/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.4s' }}
          >
            <div className="bg-gradient-to-br from-neon-green/20 to-neon-cyan/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-green/30 group-hover:shadow-glow-green transition-all">
              <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-green transition-colors">Disponibilità</h2>
            <p className="text-gray-400">Override personalizzati</p>
          </Link>

          <Link
            to="/employees"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-purple transition-all border border-white/10 hover:border-neon-purple/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.5s' }}
          >
            <div className="bg-gradient-to-br from-neon-purple/20 to-neon-pink/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-purple/30 group-hover:shadow-glow-purple transition-all">
              <svg className="w-8 h-8 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-purple transition-colors">Dipendenti</h2>
            <p className="text-gray-400">Gestisci turni e staff</p>
          </Link>

          <Link
            to="/shifts"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-blue transition-all border border-white/10 hover:border-neon-blue/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.6s' }}
          >
            <div className="bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-blue/30 group-hover:shadow-glow-blue transition-all">
              <svg className="w-8 h-8 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-blue transition-colors">Turni</h2>
            <p className="text-gray-400">Pianifica turni dipendenti</p>
          </Link>

          <Link
            to="/shift-templates"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-cyan transition-all border border-white/10 hover:border-neon-cyan/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.7s' }}
          >
            <div className="bg-gradient-to-br from-neon-cyan/20 to-neon-green/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-cyan/30 group-hover:shadow-glow-cyan transition-all">
              <svg className="w-8 h-8 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-cyan transition-colors">Template Turni</h2>
            <p className="text-gray-400">Modelli turni riutilizzabili</p>
          </Link>

          <Link
            to="/hr-documents"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-pink transition-all border border-white/10 hover:border-neon-pink/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.8s' }}
          >
            <div className="bg-gradient-to-br from-neon-pink/20 to-neon-purple/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-pink/30 group-hover:shadow-glow-pink transition-all">
              <svg className="w-8 h-8 text-neon-pink" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-pink transition-colors">Documenti HR</h2>
            <p className="text-gray-400">Buste paga e documenti payroll</p>
          </Link>

          <Link
            to="/bacheca"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-green transition-all border border-white/10 hover:border-neon-green/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '0.9s' }}
          >
            <div className="bg-gradient-to-br from-neon-green/20 to-emerald-500/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-green/30 group-hover:shadow-glow-green transition-all">
              <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-green transition-colors">Bacheca</h2>
            <p className="text-gray-400">Comunicazioni aziendali ai dipendenti</p>
          </Link>

          <Link
            to="/reports"
            className="glass-card p-8 rounded-3xl hover:shadow-glow-cyan transition-all border border-white/10 hover:border-neon-cyan/50 transform hover:scale-105 hover:-translate-y-2 group animate-scale-in"
            style={{ animationDelay: '1.0s' }}
          >
            <div className="bg-gradient-to-br from-neon-cyan/20 to-neon-blue/20 w-16 h-16 rounded-2xl flex items-center justify-center mb-4 border border-neon-cyan/30 group-hover:shadow-glow-cyan transition-all">
              <svg className="w-8 h-8 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-white mb-2 group-hover:text-neon-cyan transition-colors">Report & Statistiche</h2>
            <p className="text-gray-400">Analytics e statistiche del business</p>
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
                1. Configura gli <strong>Orari</strong> standard del tuo business<br/>
                2. Aggiungi le tue <strong>Chiusure</strong> (ferie, festività)<br/>
                3. Crea i tuoi <strong>Servizi</strong><br/>
                4. I servizi erediteranno automaticamente gli orari!
              </p>
            </div>
            <div className="glass-card-dark p-6 rounded-2xl border border-white/5">
              <h3 className="text-lg font-bold text-neon-purple mb-2">💡 Nuovo Sistema</h3>
              <p className="text-gray-400">
                Gli orari sono ora centralizzati! Configura una sola volta gli orari settimanali e si applicheranno automaticamente a tutti i servizi. Usa <strong>Disponibilità</strong> solo per override specifici.
              </p>
            </div>
          </div>
        </div>
      </div>
    </AppLayout>
  )
}

export default Dashboard
