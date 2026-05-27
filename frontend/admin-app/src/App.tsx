import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { AuthProvider, ConfirmProvider, PreferencesPage, ProfilePage, Toaster, useAuth } from '@scheduler/ui'
import AdminShell from './components/AdminShell'
import apiClient from './lib/axios'
import LoginPage from './pages/LoginPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import DashboardPage from './pages/DashboardPage'
import MerchantsPage from './pages/MerchantsPage'
import MerchantDetailPage from './pages/MerchantDetailPage'
import ReportsPage from './pages/ReportsPage'
import UsersPage from './pages/UsersPage'
import UserDetailPage from './pages/UserDetailPage'
import EmployeesPage from './pages/EmployeesPage'
import EmployeeDetailPage from './pages/EmployeeDetailPage'
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

function ProfileWrapper() {
  const { user, updateUser } = useAuth<AdminUser>()
  return (
    <ProfilePage
      apiClient={apiClient}
      onProfileUpdated={(p) => {
        if (user) updateUser({ ...user, firstName: p.firstName, lastName: p.lastName, email: p.email })
      }}
    />
  )
}

function App() {
  return (
    <AuthProvider<AdminUser>
      isAuthenticatedPredicate={(u) => u.accountType === 1}
      loadingFallback={LoadingScreen}
    >
      <Toaster>
        <ConfirmProvider>
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
              <Route path="users/:id" element={<UserDetailPage />} />
              <Route path="employees" element={<EmployeesPage />} />
              <Route path="employees/:id" element={<EmployeeDetailPage />} />
              <Route path="debug" element={<DebugPage />} />
              <Route path="tools/email" element={<EmailTestPage />} />
              <Route path="profile" element={<ProfileWrapper />} />
              <Route path="preferences" element={<PreferencesPage />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Router>
        </ConfirmProvider>
      </Toaster>
    </AuthProvider>
  )
}

export default App
