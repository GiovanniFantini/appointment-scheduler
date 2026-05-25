import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { AuthProvider, PreferencesPage, ProfilePage, Toaster, useAuth } from '@scheduler/ui'
import MerchantShell from './components/MerchantShell'
import apiClient from './lib/axios'
import LoginPage from './pages/LoginPage/LoginPage'
import RegisterPage from './pages/RegisterPage/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage'
import DashboardPage from './pages/DashboardPage/DashboardPage'
import RuoliPage from './pages/RuoliPage/RuoliPage'
import ReportPage from './pages/ReportPage/ReportPage'
import PendingApprovalPage from './pages/PendingApprovalPage/PendingApprovalPage'
import { BranchProvider } from './contexts/BranchContext'

export interface MerchantUser {
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
  merchantId: number
  companyName?: string
  /** True se l'azienda è stata approvata dall'admin. Se false l'app è bloccata. */
  isApproved: boolean
  activeFeatures: string[]
}

const LoadingScreen = (
  <div
    style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      height: '100vh',
      background: '#0a0a0f',
      color: '#f1f5f9'
    }}
  >
    Caricamento...
  </div>
)

function PublicLogin() {
  const { isAuthenticated, user, login } = useAuth<MerchantUser>()
  if (isAuthenticated) return <Navigate to="/" replace />
  if (user && !user.isApproved) return <Navigate to="/pending-approval" replace />
  return <LoginPage onLogin={login} />
}

function PublicOnly({ children }: { children: React.ReactNode }) {
  const { user } = useAuth<MerchantUser>()
  if (user) return <Navigate to={user.isApproved ? '/' : '/pending-approval'} replace />
  return <>{children}</>
}

function PendingApprovalRoute() {
  const { user, logout } = useAuth<MerchantUser>()
  if (!user) return <Navigate to="/login" replace />
  if (user.isApproved) return <Navigate to="/" replace />
  return <PendingApprovalPage user={user} onLogout={logout} />
}

function Protected() {
  const { user } = useAuth<MerchantUser>()
  const location = useLocation()
  if (!user) return <Navigate to="/login" replace state={{ from: location }} />
  if (!user.isApproved) return <Navigate to="/pending-approval" replace />
  return (
    <BranchProvider>
      <MerchantShell />
    </BranchProvider>
  )
}

function App() {
  return (
    <AuthProvider<MerchantUser> loadingFallback={LoadingScreen}>
      <Toaster>
        <Router>
          <Routes>
            <Route path="/login" element={<PublicLogin />} />
            <Route
              path="/register"
              element={
                <PublicOnly>
                  <RegisterPage />
                </PublicOnly>
              }
            />
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
            <Route path="/pending-approval" element={<PendingApprovalRoute />} />

            <Route element={<Protected />}>
              <Route path="/" element={<DashboardPageWrapper />} />
              <Route path="/ruoli" element={<RuoliPage />} />
              <Route path="/report" element={<ReportPage />} />
              <Route path="/profile" element={<ProfileWrapper />} />
              <Route path="/preferences" element={<PreferencesPage />} />
            </Route>

            <Route path="*" element={<UnknownRoute />} />
          </Routes>
        </Router>
      </Toaster>
    </AuthProvider>
  )
}

function DashboardPageWrapper() {
  const { user } = useAuth<MerchantUser>()
  if (!user) return null
  return <DashboardPage user={user} />
}

function ProfileWrapper() {
  return <ProfilePage apiClient={apiClient} readOnlyProfile />
}

function UnknownRoute() {
  const { user } = useAuth<MerchantUser>()
  return <Navigate to={!user ? '/login' : user.isApproved ? '/' : '/pending-approval'} replace />
}

export default App
