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
import type { MerchantUser } from '../App'

interface MerchantNavItem extends NavItem {
  feature?: string
}

const ALL_NAV: MerchantNavItem[] = [
  { path: '/', label: 'Dashboard', exact: true, icon: <span style={{ fontSize: 16 }}>⊞</span> },
  { path: '/ruoli', label: 'Ruoli', feature: 'Ruoli', icon: <span style={{ fontSize: 16 }}>🔑</span> },
  { path: '/report', label: 'Report', feature: 'Report', icon: <span style={{ fontSize: 16 }}>📊</span> }
]

function pageTitleFor(pathname: string): string {
  if (pathname === '/') return 'Dashboard'
  const it = ALL_NAV.find((i) => i.path !== '/' && pathname.startsWith(i.path))
  return it?.label ?? 'Merchant App'
}

export default function MerchantShell() {
  const { user, logout } = useAuth<MerchantUser>()
  const location = useLocation()
  const navigate = useNavigate()

  if (!user) return null

  const visibleItems = ALL_NAV.map((i) => ({
    ...i,
    visible: !i.feature || user.activeFeatures?.includes(i.feature)
  }))

  const sections: NavSection[] = [{ label: 'Navigazione', items: visibleItems }]

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
      userMenuSubtitle={user.companyName ? `Merchant · ${user.companyName}` : 'Merchant'}
      brand={{
        title: user.companyName ?? 'Merchant App',
        subtitle: 'Area gestione',
        initial: (user.companyName ?? 'M').charAt(0).toUpperCase()
      }}
      navSections={sections}
      headerTitle={pageTitleFor(location.pathname)}
      companyName={user.companyName}
    />
  )
}
