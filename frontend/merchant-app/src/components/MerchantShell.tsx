import { PageGuide, accountPageGuides, type PageGuideDefinition } from '@scheduler/ui'
import DashboardGuide from '../pages/DashboardPage/DashboardPage.guide'
import RuoliGuide from '../pages/RuoliPage/RuoliPage.guide'
import ReportGuide from '../pages/ReportPage/ReportPage.guide'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  AppShell,
  IconLogout,
  IconSettings,
  IconUser,
  TiHome,
  TiBriefcase,
  TiFileText,
  useAuth,
  type NavItem,
  type NavSection,
  type UserMenuItem
} from '@scheduler/ui'
import type { MerchantUser } from '../App'

const PAGE_GUIDES: Record<string, PageGuideDefinition> = {
  ...accountPageGuides,
  '/': DashboardGuide,
  '/ruoli': RuoliGuide,
  '/report': ReportGuide
}

interface MerchantNavItem extends NavItem {
  feature?: string
}

const ALL_NAV: MerchantNavItem[] = [
  { path: '/', label: 'Dashboard', exact: true, icon: <TiHome size={18} /> },
  { path: '/ruoli', label: 'Ruoli', feature: 'Ruoli', icon: <TiBriefcase size={18} /> },
  { path: '/report', label: 'Report', feature: 'Report', icon: <TiFileText size={18} /> }
]

function pageTitleFor(pathname: string): string {
  if (pathname === '/activity') return 'Registro attività'
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
      pageGuide={PAGE_GUIDES[location.pathname] && <PageGuide
        key={JSON.stringify([location.pathname, user.userId, user.merchantId, user.activeFeatures, undefined])}
        app="merchant" page={PAGE_GUIDES[location.pathname]} user={user}
      />}
      navSections={sections}
      bottomNav
      headerTitle={pageTitleFor(location.pathname)}
      companyName={user.companyName}
    />
  )
}
