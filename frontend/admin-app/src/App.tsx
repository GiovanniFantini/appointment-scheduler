import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { AuthProvider, Toaster, useAuth } from '@scheduler/ui'
import AdminShell from './components/AdminShell'
import LoginPage from './pages/LoginPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import DashboardPage from './pages/DashboardPage'
import MerchantsPage from './pages/MerchantsPage'
import MerchantDetailPage from './pages/MerchantDetailPage'
import ReportsPage from './pages/ReportsPage'
import UsersPage from './pages/UsersPage'
import DebugPage from './pages/DebugPage'
import EmailTestPage from './pages/EmailTestPage'

export interface AdminUser {
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
}

const LoadingScreen = (
  <div
    style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: '#0a0a0f',
      color: '#94a3b8',
      fontSize: 14
    }}
  >
    Loading…
  </div>
)

function PublicLogin() {
  const { isAuthenticated, login } = useAuth<AdminUser>()
  if (isAuthenticated) return <Navigate to="/" replace />
  return (
    <LoginPage
      onLogin={(u, t) => {
        if (u.accountType === 1) login(u, t)
      }}
    />
  )
}

function PublicOnly({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth<AdminUser>()
  if (isAuthenticated) return <Navigate to="/" replace />
  return <>{children}</>
}

function Protected() {
  const { isAuthenticated } = useAuth<AdminUser>()
  const location = useLocation()
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />
  return <AdminShell />
}

function App() {
  return (
    <AuthProvider<AdminUser>
      isAuthenticatedPredicate={(u) => u.accountType === 1}
      loadingFallback={LoadingScreen}
    >
      <Toaster>
        <Router>
          <Routes>
            <Route path="/login" element={<PublicLogin />} />
            <Route
              path="/forgot-password"
              element={
                <PublicOnly>
                  <ForgotPasswordPage />
                </PublicOnly>
              }
            />
            <Route
              path="/reset-password"
              element={
                <PublicOnly>
                  <ResetPasswordPage />
                </PublicOnly>
              }
            />

            <Route element={<Protected />}>
              <Route index element={<DashboardPage />} />
              <Route path="merchants" element={<MerchantsPage />} />
              <Route path="merchants/:id" element={<MerchantDetailPage />} />
              <Route path="reports" element={<ReportsPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="debug" element={<DebugPage />} />
              <Route path="tools/email" element={<EmailTestPage />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Router>
      </Toaster>
    </AuthProvider>
  )
}

export default App
