import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import Dashboard from './pages/Dashboard'
import Colleagues from './pages/Colleagues'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  role: string
  employeeId?: number
  merchantId?: number
}

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')

    if (token && userData) {
      const parsedUser = JSON.parse(userData)
      if (parsedUser.role === 'Employee') {
        setUser(parsedUser)
      } else {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    if (userData.role !== 'Employee') {
      alert('Accesso riservato solo ai dipendenti')
      return
    }
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
          user ? <Dashboard user={user} onLogout={handleLogout} /> : <Navigate to="/login" />
        } />
        <Route path="/colleagues" element={
          user ? <Colleagues onLogout={handleLogout} /> : <Navigate to="/login" />
        } />
      </Routes>
    </Router>
  )
}

export default App
