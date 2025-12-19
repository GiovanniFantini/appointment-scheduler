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
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Dashboard</h1>
          <div className="flex items-center gap-4">
            <span className="text-gray-600">{user.firstName} {user.lastName}</span>
            {user.role === 'Admin' && (
              <Link
                to="/admin"
                className="bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700"
              >
                Admin Panel
              </Link>
            )}
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Esci
            </button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <Link to="/services" className="bg-white p-6 rounded-lg shadow-lg hover:shadow-xl transition">
            <div className="text-4xl mb-4">📋</div>
            <h2 className="text-xl font-bold mb-2">I Miei Servizi</h2>
            <p className="text-gray-600">Gestisci i servizi che offri</p>
          </Link>

          <Link to="/bookings" className="bg-white p-6 rounded-lg shadow-lg hover:shadow-xl transition">
            <div className="text-4xl mb-4">📅</div>
            <h2 className="text-xl font-bold mb-2">Prenotazioni</h2>
            <p className="text-gray-600">Visualizza e gestisci le prenotazioni</p>
          </Link>

          <div className="bg-white p-6 rounded-lg shadow-lg">
            <div className="text-4xl mb-4">📊</div>
            <h2 className="text-xl font-bold mb-2">Statistiche</h2>
            <p className="text-gray-600">Analizza le performance</p>
            <p className="text-sm text-gray-400 mt-2">Prossimamente</p>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-lg p-6">
          <h2 className="text-2xl font-bold mb-4">Benvenuto!</h2>
          <p className="text-gray-700">
            Usa il menu sopra per gestire i tuoi servizi e le prenotazioni.
          </p>
        </div>
      </div>
    </div>
  )
}

export default Dashboard
