import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import AppLayout from './components/AppLayout/AppLayout'
import LoginPage from './pages/LoginPage/LoginPage'
import RegisterPage from './pages/RegisterPage/RegisterPage'
import DashboardPage from './pages/DashboardPage/DashboardPage'
import CalendarioPage from './pages/CalendarioPage/CalendarioPage'
import RichiestePage from './pages/RichiestePage/RichiestePage'
import RisorsePage from './pages/RisorsePage/RisorsePage'
import RuoliPage from './pages/RuoliPage/RuoliPage'
import DocumentiPage from './pages/DocumentiPage/DocumentiPage'
import ReportPage from './pages/ReportPage/ReportPage'

export interface MerchantUser {
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: string
  merchantId: number
  companyName?: string
  activeFeatures: string[]
}

interface AppProps {}

function App(_props: AppProps) {
  const [user, setUser] = useState<MerchantUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')
    if (token && userData) {
      try {
        const parsed = JSON.parse(userData) as MerchantUser
        setUser(parsed)
      } catch {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: MerchantUser, token: string) => {
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
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', background: '#0f172a', color: '#f1f5f9' }}>
        Caricamento...
      </div>
    )
  }

  return (
    <Router>
      <Routes>
        <Route path="/login" element={user ? <Navigate to="/" /> : <LoginPage onLogin={handleLogin} />} />
        <Route path="/register" element={user ? <Navigate to="/" /> : <RegisterPage />} />
        <Route element={user ? <AppLayout user={user} onLogout={handleLogout} /> : <Navigate to="/login" />}>
          <Route path="/" element={<DashboardPage user={user!} />} />
          <Route path="/calendario" element={<CalendarioPage user={user!} />} />
          <Route path="/richieste" element={<RichiestePage />} />
          <Route path="/risorse" element={<RisorsePage user={user!} />} />
          <Route path="/ruoli" element={<RuoliPage />} />
          <Route path="/documenti" element={<DocumentiPage />} />
          <Route path="/report" element={<ReportPage />} />
        </Route>
        <Route path="*" element={<Navigate to={user ? '/' : '/login'} />} />
      </Routes>
    </Router>
  )
}

export default App
