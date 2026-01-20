import { useNavigate } from 'react-router-dom'
import VersionInfo from '../components/VersionInfo'

interface DashboardProps {
  user: {
    userId: number
    firstName: string
    lastName: string
    email: string
    roles: string[]
    isAdmin: boolean
    isConsumer: boolean
    isMerchant: boolean
    isEmployee: boolean
  }
  onLogout: () => void
}

export default function Dashboard({ user, onLogout }: DashboardProps) {
  const navigate = useNavigate()

  return (
    <div className="min-h-screen bg-gradient-to-br from-dark-bg via-cyber-950 to-dark-surface">
      {/* Header */}
      <header className="glass-card border-b border-white/10 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div>
              <h1 className="text-2xl font-bold gradient-text">Employee App</h1>
              <p className="text-gray-400 text-sm">Benvenuto, {user.firstName} {user.lastName}</p>
            </div>
            <button
              onClick={onLogout}
              className="px-4 py-2 bg-red-500/20 hover:bg-red-500/30 text-red-300 rounded-lg transition-colors border border-red-500/50"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {/* Card Timbratura - FEATURED */}
          <button
            onClick={() => navigate('/timbratura')}
            className="glass-card p-6 rounded-2xl hover:shadow-glow-green transition-all duration-300 transform hover:scale-105 text-left border border-neon-green/30"
          >
            <div className="flex items-center gap-4 mb-4">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-green/20 to-emerald-500/20 border border-neon-green/50">
                <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">Timbratura</h3>
                <p className="text-gray-400 text-sm">Entra/Esci - Sistema intelligente</p>
              </div>
            </div>
            <div className="flex items-center text-neon-green text-sm font-semibold">
              Vai alla timbratura
              <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </button>

          {/* Card Colleghi */}
          <button
            onClick={() => navigate('/colleagues')}
            className="glass-card p-6 rounded-2xl hover:shadow-glow-cyan transition-all duration-300 transform hover:scale-105 text-left border border-white/10"
          >
            <div className="flex items-center gap-4 mb-4">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-cyan/20 to-neon-blue/20 border border-neon-cyan/30">
                <svg className="w-8 h-8 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">I Miei Colleghi</h3>
                <p className="text-gray-400 text-sm">Visualizza i tuoi colleghi</p>
              </div>
            </div>
            <div className="flex items-center text-neon-cyan text-sm font-semibold">
              Vai alla lista
              <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </button>

          {/* Placeholder Cards */}
          <div className="glass-card p-6 rounded-2xl border border-white/10 opacity-50 cursor-not-allowed">
            <div className="flex items-center gap-4 mb-4">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-green/20 to-neon-cyan/20 border border-neon-green/30">
                <svg className="w-8 h-8 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">I Miei Turni</h3>
                <p className="text-gray-400 text-sm">Prossimamente</p>
              </div>
            </div>
          </div>

          <button
            onClick={() => navigate('/documents')}
            className="glass-card p-6 rounded-2xl hover:shadow-glow-purple transition-all duration-300 transform hover:scale-105 text-left border border-white/10"
          >
            <div className="flex items-center gap-4 mb-4">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-purple/20 to-neon-pink/20 border border-neon-purple/30">
                <svg className="w-8 h-8 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">I Miei Documenti</h3>
                <p className="text-gray-400 text-sm">Buste paga e documenti HR</p>
              </div>
            </div>
            <div className="flex items-center text-neon-purple text-sm font-semibold">
              Vai ai documenti
              <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </button>
        </div>

        {/* Welcome Message */}
        <div className="mt-8 glass-card p-6 rounded-2xl border border-white/10">
          <h2 className="text-2xl font-bold gradient-text mb-4">Benvenuto nell'Employee App!</h2>
          <p className="text-gray-300 mb-4">
            Questa è la tua area personale dove puoi:
          </p>
          <ul className="list-disc list-inside text-gray-300 space-y-2">
            <li><strong className="text-neon-green">Timbratura intelligente</strong> - Check-in/out con sistema empatico e auto-validazione</li>
            <li>Visualizzare i tuoi colleghi</li>
            <li>Vedere le informazioni di contatto dei tuoi colleghi</li>
            <li><strong className="text-neon-purple">I tuoi documenti HR</strong> - Buste paga, contratti e documenti payroll</li>
            <li>Gestire il tuo profilo (prossimamente)</li>
            <li>Visualizzare i tuoi turni (prossimamente)</li>
          </ul>
        </div>

        <VersionInfo />
      </main>
    </div>
  )
}
