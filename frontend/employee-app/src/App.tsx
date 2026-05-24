import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import LoginPage from './pages/LoginPage/LoginPage'
import RegisterPage from './pages/RegisterPage/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage'
import SelectCompanyPage from './pages/SelectCompanyPage/SelectCompanyPage'
import DashboardPage from './pages/DashboardPage/DashboardPage'
import CalendarioPage from './pages/CalendarioPage/CalendarioPage'
import PianificazionePage from './pages/PianificazionePage/PianificazionePage'
import RichiestePage from './pages/RichiestePage/RichiestePage'
import DocumentiPage from './pages/DocumentiPage/DocumentiPage'
import NotifichePage from './pages/NotifichePage/NotifichePage'
import TimbraturaPage from './pages/TimbraturaPage/TimbraturaPage'
import MagazzinoPage from './pages/MagazzinoPage/MagazzinoPage'
import FilialiPage from './pages/FilialiPage/FilialiPage'
import MansioniPage from './pages/MansioniPage/MansioniPage'
import RisorsePage from './pages/RisorsePage/RisorsePage'
import TimbraturaGestionePage from './pages/TimbraturaGestionePage/TimbraturaGestionePage'
import AppLayout from './components/AppLayout/AppLayout'
import { BranchProvider } from './contexts/BranchContext'

export type FeatureAccessLevel = 'ReadOnly' | 'Operator' | 'Manager'

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
  // Livello di accesso per feature (post company-switch).
  // Valorizzato solo per le feature che usano i livelli (es. Magazzino, Documenti).
  featureLevels?: Record<string, FeatureAccessLevel>
  companies: Array<{ merchantId: number; companyName: string; city?: string; roleId: number; roleName: string }>
}

const LEVEL_RANK: Record<FeatureAccessLevel, number> = {
  ReadOnly: 1,
  Operator: 2,
  Manager: 3,
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
          path="/forgot-password"
          element={
            isAuthenticated
              ? <Navigate to="/" replace />
              : <ForgotPasswordPage />
          }
        />
        <Route
          path="/reset-password"
          element={
            isAuthenticated
              ? <Navigate to="/" replace />
              : <ResetPasswordPage />
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
                : (
                  <BranchProvider>
                    <AppLayout user={user} onLogout={handleLogout} onUserUpdate={handleUserUpdate} />
                  </BranchProvider>
                )
          }
        >
          <Route index element={<DashboardPage user={user!} />} />
          <Route
            path="timbratura"
            element={
              user?.activeFeatures?.includes('Timbratura')
                ? <TimbraturaPage accessLevel={user.featureLevels?.['Timbratura'] ?? 'ReadOnly'} />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="timbratura-gestione"
            element={
              user?.activeFeatures?.includes('Timbratura') && LEVEL_RANK[user.featureLevels?.['Timbratura'] ?? 'ReadOnly'] >= LEVEL_RANK.Manager
                ? <TimbraturaGestionePage />
                : <Navigate to="/timbratura" replace />
            }
          />
          <Route
            path="calendario"
            element={
              user?.activeFeatures?.includes('Calendario')
                ? <CalendarioPage accessLevel={user.featureLevels?.['Calendario'] ?? 'ReadOnly'} />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="pianificazione"
            element={
              user?.activeFeatures?.includes('Calendario') && LEVEL_RANK[user.featureLevels?.['Calendario'] ?? 'ReadOnly'] >= LEVEL_RANK.Operator
                ? <PianificazionePage accessLevel={user.featureLevels?.['Calendario'] ?? 'ReadOnly'} />
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
            path="risorse"
            element={
              user?.activeFeatures?.includes('Risorse')
                ? <RisorsePage />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="mansioni"
            element={
              user?.activeFeatures?.includes('Mansioni')
                ? <MansioniPage />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="filiali"
            element={
              user?.activeFeatures?.includes('Filiali')
                ? <FilialiPage />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="documenti"
            element={
              user?.activeFeatures?.includes('Documenti')
                ? <DocumentiPage accessLevel={user.featureLevels?.['Documenti'] ?? 'ReadOnly'} />
                : <Navigate to="/" replace />
            }
          />
          <Route
            path="magazzino"
            element={
              user?.activeFeatures?.includes('Magazzino')
                ? <MagazzinoPage accessLevel={user.featureLevels?.['Magazzino'] ?? 'ReadOnly'} />
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
