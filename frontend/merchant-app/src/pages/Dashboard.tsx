interface DashboardProps {
  user: any
  onLogout: () => void
}

function Dashboard({ user, onLogout }: DashboardProps) {
  return (
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Merchant Dashboard</h1>
          <div className="flex items-center gap-4">
            <span className="text-gray-600">
              {user.firstName} {user.lastName}
            </span>
            <button
              onClick={onLogout}
              className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300"
            >
              Esci
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="container mx-auto px-4 py-8">
        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white p-6 rounded-lg shadow-lg">
            <div className="text-gray-500 text-sm font-semibold">Prenotazioni Oggi</div>
            <div className="text-3xl font-bold text-gray-800 mt-2">12</div>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-lg">
            <div className="text-gray-500 text-sm font-semibold">Prenotazioni Mese</div>
            <div className="text-3xl font-bold text-gray-800 mt-2">248</div>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-lg">
            <div className="text-gray-500 text-sm font-semibold">Servizi Attivi</div>
            <div className="text-3xl font-bold text-gray-800 mt-2">5</div>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-lg">
            <div className="text-gray-500 text-sm font-semibold">Tasso Occupazione</div>
            <div className="text-3xl font-bold text-gray-800 mt-2">78%</div>
          </div>
        </div>

        {/* Recent Bookings */}
        <div className="bg-white rounded-lg shadow-lg p-6">
          <h2 className="text-2xl font-bold text-gray-800 mb-4">Prenotazioni Recenti</h2>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Cliente</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Servizio</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data/Ora</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Stato</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Azioni</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                <tr>
                  <td className="px-6 py-4 text-sm text-gray-500" colSpan={5}>
                    Nessuna prenotazione al momento
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-8">
          <button className="bg-green-600 text-white p-6 rounded-lg shadow-lg hover:bg-green-700 transition">
            <div className="text-xl font-bold">Nuovo Servizio</div>
            <div className="text-sm mt-2">Aggiungi un nuovo servizio alla tua offerta</div>
          </button>
          <button className="bg-blue-600 text-white p-6 rounded-lg shadow-lg hover:bg-blue-700 transition">
            <div className="text-xl font-bold">Gestisci Calendario</div>
            <div className="text-sm mt-2">Visualizza e modifica disponibilità</div>
          </button>
          <button className="bg-purple-600 text-white p-6 rounded-lg shadow-lg hover:bg-purple-700 transition">
            <div className="text-xl font-bold">Report</div>
            <div className="text-sm mt-2">Analizza le performance del tuo business</div>
          </button>
        </div>
      </div>
    </div>
  )
}

export default Dashboard
