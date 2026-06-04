import { useLocation, useNavigate } from 'react-router-dom'
import {
  AppShell,
  IconLogout,
  IconSettings,
  IconUser,
  TiHome,
  TiBuildingStore,
  TiUsers,
  TiUserPlus,
  TiFileText,
  TiSparkles,
  TiSend,
  useAuth,
  type NavItem,
  type NavSection,
  type UserMenuItem
} from '@scheduler/ui'
import type { AdminUser } from '../App'

/** Item piatti — esposti per riusarli nella derivazione del page title. */
export const ADMIN_NAV: Array<NavItem & { section: string }> = [
  { section: 'Overview', path: '/', label: 'Dashboard', exact: true, icon: <TiHome size={18} /> },
  { section: 'Management', path: '/merchants', label: 'Merchants', icon: <TiBuildingStore size={18} /> },
  { section: 'Management', path: '/users', label: 'Users', icon: <TiUsers size={18} /> },
  { section: 'Management', path: '/employees', label: 'Employees', icon: <TiUserPlus size={18} /> },
  { section: 'Analytics', path: '/reports', label: 'Reports', icon: <TiFileText size={18} /> },
  { section: 'Developer', path: '/debug', label: 'Debug', icon: <TiSparkles size={18} /> },
  { section: 'Developer', path: '/tools/email', label: 'Email Test', icon: <TiSend size={18} /> }
]

function buildSections(): NavSection[] {
  const sectionOrder = ['Overview', 'Management', 'Analytics', 'Developer']
  return sectionOrder.map((section) => ({
    label: section,
    items: ADMIN_NAV.filter((i) => i.section === section)
  }))
}

function deriveTitle(pathname: string): string {
  if (pathname === '/') return 'Dashboard'
  if (pathname.startsWith('/merchants/')) return 'Merchant Detail'
  if (pathname.startsWith('/users/')) return 'User Detail'
  if (pathname.startsWith('/employees/')) return 'Employee Detail'
  const item = ADMIN_NAV.find((i) => i.path !== '/' && pathname.startsWith(i.path))
  return item?.label ?? 'Admin Hub'
}

export default function AdminShell() {
  const { user, logout } = useAuth<AdminUser>()
  const location = useLocation()
  const navigate = useNavigate()

  if (!user) return null

  const userMenuItems: UserMenuItem[] = [
    {
      id: 'profile',
      label: 'Il mio profilo',
      icon: <IconUser size={16} />,
      onClick: () => navigate('/profile')
    },
    {
      id: 'preferences',
      label: 'Preferenze',
      icon: <IconSettings size={16} />,
      onClick: () => navigate('/preferences')
    },
    {
      id: 'logout',
      label: 'Esci',
      icon: <IconLogout size={16} />,
      danger: true,
      separatorBefore: true,
      onClick: () => {
        logout()
        navigate('/login', { replace: true })
      }
    }
  ]

  return (
    <AppShell
      user={user}
      userMenuItems={userMenuItems}
      userMenuSubtitle="Administrator"
      brand={{ title: 'Admin Hub', subtitle: 'Console', initial: 'A' }}
      navSections={buildSections()}
      bottomNav
      headerTitle={deriveTitle(location.pathname)}
    />
  )
}
