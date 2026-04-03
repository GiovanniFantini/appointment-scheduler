import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import LoginPage from './pages/LoginPage/LoginPage'
import RegisterPage from './pages/RegisterPage/RegisterPage'
import SelectCompanyPage from './pages/SelectCompanyPage/SelectCompanyPage'
import DashboardPage from './pages/DashboardPage/DashboardPage'
import CalendarioPage from './pages/CalendarioPage/CalendarioPage'
import RichiestePage from './pages/RichiestePage/RichiestePage'
import DocumentiPage from './pages/DocumentiPage/DocumentiPage'
import NotifichePage from './pages/NotifichePage/NotifichePage'
import AppLayout from './components/AppLayout/AppLayout'

export interface EmployeeUser {
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
  employeeId?: number
  merchantId?: number
  companyName?: string
  activeFeatures: string[]
  companies: Array<{ merchantId: number; companyName: string; city?: string; roleId: number; roleName: string }>
}

function App() {
  const [user, setUser] = useState<EmployeeUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')
    if (token && userData) {
      try {
        const parsed = JSON.parse(userData) as EmployeeUser
        setUser(parsed)
      } catch {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: EmployeeUser, token: string) => {
    localStorage.setItem('token', token)
    localStorage.setItem('user', JSON.stringify(userData))
    setUser(userData)
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    setUser(null)
  }

  const handleUserUpdate = (updatedUser: EmployeeUser, token: string) => {
    localStorage.setItem('token', token)
    localStorage.setItem('user', JSON.stringify(updatedUser))
    setUser(updatedUser)
  }

  if (loading) {
    return (
      <div className="loading-screen">
        <div className="loading-spinner" />
      </div>
    )
  }

  const isAuthenticated = !!(user && user.merchantId)
  const needsCompanySelection = !!(user && !user.merchantId)

  return (
    <Router>
      <Routes>
        {/* Public routes */}
        <Route
          path="/login"
          element={
            isAuthenticated
              ? <Navigate to="/" replace />
              : needsCompanySelection
                ? <Navigate to="/select-company" replace />
                : <LoginPage onLogin={handleLogin} />
          }
        />
        <Route
          path="/register"
          element={
            isAuthenticated
              ? <Navigate to="/" replace />
              : needsCompanySelection
                ? <Navigate to="/select-company" replace />
                : <RegisterPage />
          }
        />
        <Route
          path="/select-company"
          element={
            !user
              ? <Navigate to="/login" replace />
              : isAuthenticated
                ? <Navigate to="/" replace />
                : <SelectCompanyPage user={user} onCompanySelected={handleUserUpdate} onLogout={handleLogout} />
          }
        />

        {/* Protected routes */}
        <Route
          path="/"
          element={
            !user
              ? <Navigate to="/login" replace />
              : needsCompanySelection
                ? <Navigate to="/select-company" replace />
                : <AppLayout user={user} onLogout={handleLogout} onUserUpdate={handleUserUpdate} />
          }
        >
          <Route index element={<DashboardPage user={user!} />} />
          <Route
            path="calendario"
            element={
              user?.activeFeatures?.includes('Calendario')
                ? <CalendarioPage />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="richieste"
            element={
              user?.activeFeatures?.includes('Richieste')
                ? <RichiestePage />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="documenti"
            element={
              user?.activeFeatures?.includes('Documenti')
                ? <DocumentiPage />
                : <Navigate to="/" replace />
            }
          />
          <Route path="notifiche" element={<NotifichePage />} />
        </Route>

        <Route path="*" element={<Navigate to={isAuthenticated ? '/' : '/login'} replace />} />
      </Routes>
    </Router>
  )
}

export default App
