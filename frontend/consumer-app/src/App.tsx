import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import Home from './pages/Home'
import Bookings from './pages/Bookings'
import ApiDebugConsole from './components/ApiDebugConsole'
import { STORAGE_KEYS, ROUTES } from './constants'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  role: string
}

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Controlla se c'è un token salvato
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN)
    const userData = localStorage.getItem(STORAGE_KEYS.USER)

    if (token && userData) {
      setUser(JSON.parse(userData))
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    localStorage.setItem(STORAGE_KEYS.TOKEN, token)
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(userData))
    setUser(userData)
  }

  const handleLogout = () => {
    localStorage.removeItem(STORAGE_KEYS.TOKEN)
    localStorage.removeItem(STORAGE_KEYS.USER)
    setUser(null)
  }

  if (loading) {
    return <div className="flex items-center justify-center h-screen">Caricamento...</div>
  }

  return (
    <>
      <ApiDebugConsole />
      <Router>
        <Routes>
          <Route path={ROUTES.LOGIN} element={
            user ? <Navigate to={ROUTES.HOME} /> : <Login onLogin={handleLogin} />
          } />
          <Route path={ROUTES.REGISTER} element={
            user ? <Navigate to={ROUTES.HOME} /> : <Register onRegister={handleLogin} />
          } />
          <Route path={ROUTES.HOME} element={
            user ? <Home user={user} onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
          <Route path={ROUTES.BOOKINGS} element={
            user ? <Bookings onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
        </Routes>
      </Router>
    </>
  )
}

export default App
