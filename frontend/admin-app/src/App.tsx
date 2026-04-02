import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import AppLayout from './components/AppLayout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import MerchantsPage from './pages/MerchantsPage'
import MerchantDetailPage from './pages/MerchantDetailPage'
import ReportsPage from './pages/ReportsPage'
import UsersPage from './pages/UsersPage'
import DebugPage from './pages/DebugPage'

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  // 1 = Admin, 2 = Merchant, 3 = Employee
  accountType: number
}

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    const userData = localStorage.getItem('user')

    if (token && userData) {
      try {
        const parsed = JSON.parse(userData) as User
        if (parsed.accountType === 1) {
          setUser(parsed)
        } else {
          localStorage.removeItem('token')
          localStorage.removeItem('user')
        }
      } catch {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      }
    }
    setLoading(false)
  }, [])

  const handleLogin = (userData: User, token: string) => {
    if (userData.accountType !== 1) return
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
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#0f172a',
          color: '#94a3b8',
          fontSize: 14,
        }}
      >
        Loading…
      </div>
    )
  }

  return (
    <Router>
      <Routes>
        {/* Public */}
        <Route
          path="/login"
          element={user ? <Navigate to="/" replace /> : <LoginPage onLogin={handleLogin} />}
        />

        {/* Protected – the layout's onLogout is wired here via a wrapper so we always have the current handler */}
        <Route
          path="/"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Dashboard">
                <DashboardPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/merchants"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Merchants">
                <MerchantsPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/merchants/:id"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Merchant Detail">
                <MerchantDetailPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/reports"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Reports">
                <ReportsPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/users"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Users">
                <UsersPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/debug"
          element={
            user ? (
              <AppLayout user={user} onLogout={handleLogout} pageTitle="Debug">
                <DebugPage />
              </AppLayout>
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Router>
  )
}

export default App
