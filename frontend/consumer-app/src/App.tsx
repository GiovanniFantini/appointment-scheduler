import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import Home from './pages/Home'
import Bookings from './pages/Bookings'

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
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')

    if (token && userData) {
      setUser(JSON.parse(userData))
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    localStorage.setItem('token', token)
    localStorage.setItem('user', JSON.stringify(userData))
    setUser(userData)
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    setUser(null)
  }

  if (loading) {
    return <div className="flex items-center justify-center h-screen">Caricamento...</div>
  }

  return (
    <Router>
      <Routes>
        <Route path="/login" element={
          user ? <Navigate to="/" /> : <Login onLogin={handleLogin} />
        } />
        <Route path="/register" element={
          user ? <Navigate to="/" /> : <Register onRegister={handleLogin} />
        } />
        <Route path="/" element={
          user ? <Home user={user} onLogout={handleLogout} /> : <Navigate to="/login" />
        } />
        <Route path="/bookings" element={
          user ? <Bookings onLogout={handleLogout} /> : <Navigate to="/login" />
        } />
      </Routes>
    </Router>
  )
}

export default App
