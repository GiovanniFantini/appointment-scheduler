import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import Dashboard from './pages/Dashboard'
import AdminPanel from './pages/AdminPanel'
import Services from './pages/Services'
import Bookings from './pages/Bookings'
import Availabilities from './pages/Availabilities'
import ApiDebugConsole from './components/ApiDebugConsole'
import { STORAGE_KEYS, ROUTES, USER_ROLE_NAMES } from './constants'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  role: string
  merchantId?: number
}

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN)
    const userData = localStorage.getItem(STORAGE_KEYS.USER)

    if (token && userData) {
      const parsedUser = JSON.parse(userData)
      if (parsedUser.role === USER_ROLE_NAMES[2] || parsedUser.role === USER_ROLE_NAMES[0]) {
        setUser(parsedUser)
      } else {
        localStorage.removeItem(STORAGE_KEYS.TOKEN)
        localStorage.removeItem(STORAGE_KEYS.USER)
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    if (userData.role !== USER_ROLE_NAMES[2] && userData.role !== USER_ROLE_NAMES[0]) {
      alert('Accesso riservato solo a merchant e admin')
      return
    }
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
            user ? <Dashboard user={user} onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
          <Route path="/admin" element={
            user && user.role === USER_ROLE_NAMES[0] ? <AdminPanel onLogout={handleLogout} /> : <Navigate to={ROUTES.HOME} />
          } />
          <Route path={ROUTES.SERVICES} element={
            user ? <Services onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
          <Route path={ROUTES.BOOKINGS} element={
            user ? <Bookings onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
          <Route path="/availabilities" element={
            user ? <Availabilities onLogout={handleLogout} /> : <Navigate to={ROUTES.LOGIN} />
          } />
        </Routes>
      </Router>
    </>
  )
}

export default App
