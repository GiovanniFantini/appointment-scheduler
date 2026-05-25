import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { AuthProvider, PreferencesPage, ProfilePage, Toaster, useAuth } from '@scheduler/ui'
import apiClient from './lib/axios'
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
import EmployeeShell from './components/EmployeeShell'
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
  featureLevels?: Record<string, FeatureAccessLevel>
  companies: Array<{ merchantId: number; companyName: string; city?: string; roleId: number; roleName: string }>
}

const LEVEL_RANK: Record<FeatureAccessLevel, number> = {
  ReadOnly: 1,
  Operator: 2,
  Manager: 3
}

export function hasFeatureLevel(user: EmployeeUser, feature: string, min: FeatureAccessLevel): boolean {
  const lvl = user.featureLevels?.[feature] ?? 'ReadOnly'
  return LEVEL_RANK[lvl] >= LEVEL_RANK[min]
}

const LoadingScreen = (
  <div className="loading-screen">
    <div className="loading-spinner" />
  </div>
)

function PublicLogin() {
  const { user, login } = useAuth<EmployeeUser>()
  if (user?.merchantId) return <Navigate to="/" replace />
  if (user) return <Navigate to="/select-company" replace />
  return <LoginPage onLogin={login} />
}

function PublicOnly({ children }: { children: React.ReactNode }) {
  const { user } = useAuth<EmployeeUser>()
  if (user?.merchantId) return <Navigate to="/" replace />
  if (user) return <Navigate to="/select-company" replace />
  return <>{children}</>
}

function SelectCompanyRoute() {
  const { user, updateUser, logout } = useAuth<EmployeeUser>()
  if (!user) return <Navigate to="/login" replace />
  if (user.merchantId) return <Navigate to="/" replace />
  return <SelectCompanyPage user={user} onCompanySelected={updateUser} onLogout={logout} />
}

function Protected() {
  const { user } = useAuth<EmployeeUser>()
  const location = useLocation()
  if (!user) return <Navigate to="/login" replace state={{ from: location }} />
  if (!user.merchantId) return <Navigate to="/select-company" replace />
  return (
    <BranchProvider>
      <EmployeeShell />
    </BranchProvider>
  )
}

function FeatureRoute({
  feature,
  minLevel,
  redirectTo = '/',
  children
}: {
  feature: string
  minLevel?: FeatureAccessLevel
  redirectTo?: string
  children: React.ReactNode
}) {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return <Navigate to="/login" replace />
  if (!user.activeFeatures?.includes(feature)) return <Navigate to={redirectTo} replace />
  if (minLevel && !hasFeatureLevel(user, feature, minLevel)) return <Navigate to={redirectTo} replace />
  return <>{children}</>
}

function App() {
  return (
    <AuthProvider<EmployeeUser>
      isAuthenticatedPredicate={(u) => !!u.merchantId}
      loadingFallback={LoadingScreen}
    >
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
            <Route path="/select-company" element={<SelectCompanyRoute />} />

            <Route element={<Protected />}>
              <Route index element={<DashboardWrapper />} />
              <Route
                path="timbratura"
                element={
                  <FeatureRoute feature="Timbratura">
                    <TimbraturaWrapper />
                  </FeatureRoute>
                }
              />
              <Route
                path="timbratura-gestione"
                element={
                  <FeatureRoute feature="Timbratura" minLevel="Manager" redirectTo="/timbratura">
                    <TimbraturaGestionePage />
                  </FeatureRoute>
                }
              />
              <Route
                path="calendario"
                element={
                  <FeatureRoute feature="Calendario">
                    <CalendarioWrapper />
                  </FeatureRoute>
                }
              />
              <Route
                path="pianificazione"
                element={
                  <FeatureRoute feature="Calendario" minLevel="Operator">
                    <PianificazioneWrapper />
                  </FeatureRoute>
                }
              />
              <Route
                path="richieste"
                element={
                  <FeatureRoute feature="Richieste">
                    <RichiestePage />
                  </FeatureRoute>
                }
              />
              <Route
                path="risorse"
                element={
                  <FeatureRoute feature="Risorse">
                    <RisorsePage />
                  </FeatureRoute>
                }
              />
              <Route
                path="mansioni"
                element={
                  <FeatureRoute feature="Mansioni">
                    <MansioniPage />
                  </FeatureRoute>
                }
              />
              <Route
                path="filiali"
                element={
                  <FeatureRoute feature="Filiali">
                    <FilialiPage />
                  </FeatureRoute>
                }
              />
              <Route
                path="documenti"
                element={
                  <FeatureRoute feature="Documenti">
                    <DocumentiWrapper />
                  </FeatureRoute>
                }
              />
              <Route
                path="magazzino"
                element={
                  <FeatureRoute feature="Magazzino">
                    <MagazzinoWrapper />
                  </FeatureRoute>
                }
              />
              <Route path="notifiche" element={<NotifichePage />} />
              <Route path="profile" element={<ProfileWrapper />} />
              <Route path="preferences" element={<PreferencesPage />} />
            </Route>

            <Route path="*" element={<UnknownRoute />} />
          </Routes>
        </Router>
      </Toaster>
    </AuthProvider>
  )
}

function DashboardWrapper() {
  const { user } = useAuth<EmployeeUser>()
  return user ? <DashboardPage user={user} /> : null
}
function ProfileWrapper() {
  return <ProfilePage apiClient={apiClient} readOnlyProfile />
}
function TimbraturaWrapper() {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return null
  return <TimbraturaPage accessLevel={user.featureLevels?.['Timbratura'] ?? 'ReadOnly'} />
}
function CalendarioWrapper() {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return null
  return <CalendarioPage accessLevel={user.featureLevels?.['Calendario'] ?? 'ReadOnly'} />
}
function PianificazioneWrapper() {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return null
  return <PianificazionePage accessLevel={user.featureLevels?.['Calendario'] ?? 'ReadOnly'} />
}
function DocumentiWrapper() {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return null
  return <DocumentiPage accessLevel={user.featureLevels?.['Documenti'] ?? 'ReadOnly'} />
}
function MagazzinoWrapper() {
  const { user } = useAuth<EmployeeUser>()
  if (!user) return null
  return <MagazzinoPage accessLevel={user.featureLevels?.['Magazzino'] ?? 'ReadOnly'} />
}

function UnknownRoute() {
  const { isAuthenticated } = useAuth<EmployeeUser>()
  return <Navigate to={isAuthenticated ? '/' : '/login'} replace />
}

export default App
