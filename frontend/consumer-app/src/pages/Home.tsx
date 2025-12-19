import { Link } from 'react-router-dom'

interface HomeProps {
  user: any
  onLogout: () => void
}

function Home({ user, onLogout }: HomeProps) {
  const categories = [
    { id: 1, name: 'Ristoranti', icon: '🍽️', color: 'bg-red-500' },
    { id: 2, name: 'Sport', icon: '⚽', color: 'bg-green-500' },
    { id: 3, name: 'Wellness', icon: '💆', color: 'bg-purple-500' },
    { id: 4, name: 'Healthcare', icon: '🏥', color: 'bg-blue-500' },
    { id: 5, name: 'Professionisti', icon: '💼', color: 'bg-yellow-500' },
  ]

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Appointment Scheduler</h1>
          <div className="flex items-center gap-4">
            <span className="text-gray-600">
              Ciao, {user.firstName}!
            </span>
            <Link
              to="/bookings"
              className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
            >
              Le mie prenotazioni
            </Link>
            <button
              onClick={onLogout}
              className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300"
            >
              Esci
            </button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="bg-gradient-to-r from-blue-600 to-purple-600 text-white py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-5xl font-bold mb-4">
            Prenota i tuoi servizi preferiti
          </h2>
          <p className="text-xl mb-8">
            Un'unica piattaforma per ristoranti, sport, wellness e molto altro
          </p>
        </div>
      </section>

      {/* Categories */}
      <section className="container mx-auto px-4 py-12">
        <h3 className="text-3xl font-bold text-gray-800 mb-8">Esplora per categoria</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-6">
          {categories.map((category) => (
            <div
              key={category.id}
              className="bg-white rounded-lg shadow-lg p-6 hover:shadow-xl transition duration-200 cursor-pointer"
            >
              <div className={`${category.color} w-16 h-16 rounded-full flex items-center justify-center text-4xl mb-4 mx-auto`}>
                {category.icon}
              </div>
              <h4 className="text-xl font-bold text-center text-gray-800">
                {category.name}
              </h4>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}

export default Home
