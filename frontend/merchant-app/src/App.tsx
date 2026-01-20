import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import Dashboard from './pages/Dashboard'
import AdminPanel from './pages/AdminPanel'
import Services from './pages/Services'
import Bookings from './pages/Bookings'
import Availabilities from './pages/Availabilities'
import Employees from './pages/Employees'
import BusinessHours from './pages/BusinessHours'
import ClosurePeriods from './pages/ClosurePeriods'
import Shifts from './pages/Shifts'
import ShiftTemplates from './pages/ShiftTemplates'
import TimbratureValidation from './pages/TimbratureValidation'
import HRDocuments from './pages/HRDocuments'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  roles: string[]
  isAdmin: boolean
  isConsumer: boolean
  isMerchant: boolean
  isEmployee: boolean
  merchantId?: number
  // Note: employeeId rimosso - employee può lavorare per multipli merchant
}

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')

    if (token && userData) {
      const parsedUser = JSON.parse(userData) as User
      // Merchant app: permette accesso solo a Merchant o Admin
      if (parsedUser.isMerchant || parsedUser.isAdmin) {
        setUser(parsedUser)
      } else {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    if (!userData.isMerchant && !userData.isAdmin) {
      alert('Accesso riservato solo a merchant e admin')
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
          <Route path="/admin" element={
            user && user.isAdmin ? <AdminPanel onLogout={handleLogout} /> : <Navigate to="/" />
          } />
          <Route path="/services" element={
            user ? <Services onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/bookings" element={
            user ? <Bookings onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/availabilities" element={
            user ? <Availabilities onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/employees" element={
            user ? <Employees onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/business-hours" element={
            user ? <BusinessHours onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/closures" element={
            user ? <ClosurePeriods onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/shifts" element={
            user ? <Shifts onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/shift-templates" element={
            user ? <ShiftTemplates onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/timbrature" element={
            user ? <TimbratureValidation /> : <Navigate to="/login" />
          } />
          <Route path="/hr-documents" element={
            user ? <HRDocuments onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
          <Route path="/dashboard" element={
            user ? <Dashboard user={user} onLogout={handleLogout} /> : <Navigate to="/login" />
          } />
        </Routes>
      </Router>
  )
}

export default App
