import { useLocation, useNavigate } from 'react-router-dom'
import {
  AppShell,
  IconLogout,
  IconSettings,
  IconUser,
  useAuth,
  type NavItem,
  type NavSection,
  type UserMenuItem
} from '@scheduler/ui'
import type { AdminUser } from '../App'

/** Item piatti — esposti per riusarli nella derivazione del page title. */
export const ADMIN_NAV: Array<NavItem & { section: string }> = [
  {
    section: 'Overview',
    path: '/',
    label: 'Dashboard',
    exact: true,
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <rect x="3" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="3" width="7" height="7" rx="1" />
        <rect x="3" y="14" width="7" height="7" rx="1" />
        <rect x="14" y="14" width="7" height="7" rx="1" />
      </svg>
    )
  },
  {
    section: 'Management',
    path: '/merchants',
    label: 'Merchants',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
        <polyline points="9 22 9 12 15 12 15 22" />
      </svg>
    )
  },
  {
    section: 'Management',
    path: '/users',
    label: 'Users',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
        <circle cx="9" cy="7" r="4" />
        <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
      </svg>
    )
  },
  {
    section: 'Management',
    path: '/employees',
    label: 'Employees',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <path d="M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2" />
        <circle cx="9" cy="7" r="4" />
        <path d="M22 11l-3-3-3 3" />
        <path d="M19 8v8" />
      </svg>
    )
  },
  {
    section: 'Analytics',
    path: '/reports',
    label: 'Reports',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <line x1="18" y1="20" x2="18" y2="10" />
        <line x1="12" y1="20" x2="12" y2="4" />
        <line x1="6" y1="20" x2="6" y2="14" />
        <line x1="2" y1="20" x2="22" y2="20" />
      </svg>
    )
  },
  {
    section: 'Developer',
    path: '/debug',
    label: 'Debug',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <path d="M12 22c4.97 0 9-4.03 9-9H3c0 4.97 4.03 9 9 9z" />
        <path d="M12 13V2" />
        <path d="M5 9l7-7 7 7" />
        <path d="M3 13h3M18 13h3" />
      </svg>
    )
  },
  {
    section: 'Developer',
    path: '/tools/email',
    label: 'Email Test',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
        <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
        <polyline points="22,6 12,13 2,6" />
      </svg>
    )
  }
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
      headerTitle={deriveTitle(location.pathname)}
    />
  )
}
