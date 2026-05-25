import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  AppShell,
  IconBell,
  IconLogout,
  IconSettings,
  IconSwitch,
  IconUser,
  useAuth,
  type NavItem,
  type NavSection,
  type UserMenuItem
} from '@scheduler/ui'
import apiClient from '../lib/axios'
import { hasFeatureLevel, type EmployeeUser } from '../App'

interface EmpNavItem extends NavItem {
  feature?: string
  /** Se valorizzato, l'item richiede livello >= a questo per la feature. */
  minLevel?: 'Operator' | 'Manager'
}

function makeIcon(d: React.ReactNode) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} width={18} height={18}>
      {d}
    </svg>
  )
}

const HomeIcon = makeIcon(
  <>
    <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" strokeLinecap="round" strokeLinejoin="round" />
    <path d="M9 22V12h6v10" strokeLinecap="round" strokeLinejoin="round" />
  </>
)
const ClockIcon = makeIcon(
  <>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 7v5l3 3" strokeLinecap="round" strokeLinejoin="round" />
  </>
)
const CalendarIcon = makeIcon(
  <>
    <rect x="3" y="4" width="18" height="18" rx="2" />
    <path d="M16 2v4M8 2v4M3 10h18" strokeLinecap="round" />
  </>
)
const RequestsIcon = makeIcon(
  <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" strokeLinecap="round" />
)
const TeamIcon = makeIcon(
  <>
    <path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" strokeLinecap="round" strokeLinejoin="round" />
    <circle cx="8.5" cy="7" r="4" />
    <path d="M20 8v6M23 11h-6" strokeLinecap="round" />
  </>
)
const SkillsIcon = makeIcon(
  <>
    <path d="M20.59 13.41L11 3.83V2h-1.83L3.41 7.76a2 2 0 000 2.83l9.59 9.59a2 2 0 002.83 0l4.76-4.76a2 2 0 000-2.83z" strokeLinecap="round" strokeLinejoin="round" />
    <circle cx="7.5" cy="7.5" r="1.5" fill="currentColor" />
  </>
)
const BranchIcon = makeIcon(
  <path d="M3 21h18M5 21V7l7-4 7 4v14M9 10h6M9 14h6" strokeLinecap="round" strokeLinejoin="round" />
)
const InventoryIcon = makeIcon(
  <>
    <path d="M21 8a2 2 0 01-1 1.73l-7 4a2 2 0 01-2 0l-7-4A2 2 0 013 8V6a2 2 0 011-1.73l7-4a2 2 0 012 0l7 4A2 2 0 0121 6v2z" strokeLinecap="round" strokeLinejoin="round" />
    <path d="M3.27 6.96L12 12l8.73-5.04M12 22V12" strokeLinecap="round" strokeLinejoin="round" />
  </>
)
const DocumentsIcon = makeIcon(
  <>
    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" strokeLinecap="round" strokeLinejoin="round" />
    <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" strokeLinecap="round" />
  </>
)
const BellIcon = makeIcon(
  <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" strokeLinecap="round" strokeLinejoin="round" />
)

const ALL_NAV: EmpNavItem[] = [
  { path: '/', label: 'Dashboard', exact: true, icon: HomeIcon },
  { path: '/timbratura', label: 'Timbratura', feature: 'Timbratura', icon: ClockIcon },
  { path: '/calendario', label: 'Calendario', feature: 'Calendario', icon: CalendarIcon },
  { path: '/pianificazione', label: 'Pianificazione', feature: 'Calendario', minLevel: 'Operator', icon: CalendarIcon },
  { path: '/richieste', label: 'Richieste', feature: 'Richieste', icon: RequestsIcon },
  { path: '/risorse', label: 'Risorse', feature: 'Risorse', icon: TeamIcon },
  { path: '/mansioni', label: 'Mansioni', feature: 'Mansioni', icon: SkillsIcon },
  { path: '/filiali', label: 'Filiali', feature: 'Filiali', icon: BranchIcon },
  { path: '/magazzino', label: 'Magazzino', feature: 'Magazzino', icon: InventoryIcon },
  { path: '/documenti', label: 'Documenti', feature: 'Documenti', icon: DocumentsIcon },
  { path: '/notifiche', label: 'Notifiche', icon: BellIcon }
]

function deriveTitle(pathname: string): string {
  if (pathname === '/') return 'Dashboard'
  const it = ALL_NAV.find((i) => i.path !== '/' && pathname.startsWith(i.path))
  return it?.label ?? 'Employee Portal'
}

export default function EmployeeShell() {
  const { user, logout, updateUser } = useAuth<EmployeeUser>()
  const navigate = useNavigate()
  const location = useLocation()
  const [unreadCount, setUnreadCount] = useState(0)

  useEffect(() => {
    let cancelled = false
    async function fetchUnread() {
      try {
        const { data } = await apiClient.get<{ unreadCount: number }>('/notifications/summary')
        if (!cancelled) setUnreadCount(data.unreadCount ?? 0)
      } catch {
        /* silent */
      }
    }
    fetchUnread()
    const interval = setInterval(fetchUnread, 60_000)
    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [])

  if (!user) return null

  const sections: NavSection[] = [
    {
      items: ALL_NAV.map((i) => {
        let visible = true
        if (i.feature && !user.activeFeatures?.includes(i.feature)) visible = false
        if (visible && i.minLevel && i.feature) {
          if (!hasFeatureLevel(user, i.feature, i.minLevel)) visible = false
        }
        return {
          ...i,
          visible,
          badge: i.path === '/notifiche' ? unreadCount : undefined
        }
      })
    }
  ]

  const handleSwitchCompany = () => {
    const cleared: EmployeeUser = { ...user, merchantId: undefined, companyName: undefined, activeFeatures: [] }
    updateUser(cleared)
    navigate('/select-company')
  }

  const userMenuItems: UserMenuItem[] = [
    {
      id: 'profile',
      label: 'Il mio profilo',
      icon: <IconUser size={16} />,
      onClick: () => navigate('/profile')
    },
    ...(user.companies && user.companies.length > 1
      ? [
          {
            id: 'switch',
            label: 'Cambia azienda',
            icon: <IconSwitch size={16} />,
            onClick: handleSwitchCompany
          } satisfies UserMenuItem
        ]
      : []),
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
      userMenuSubtitle={user.companyName ? `Dipendente · ${user.companyName}` : 'Dipendente'}
      brand={{
        title: user.companyName ?? 'Employee Portal',
        subtitle: 'Area dipendente',
        initial: (user.companyName ?? 'E').charAt(0).toUpperCase()
      }}
      navSections={sections}
      headerTitle={deriveTitle(location.pathname)}
      companyName={user.companyName}
      headerExtras={
        <button
          type="button"
          className="su-header__bell"
          aria-label="Notifiche"
          onClick={() => navigate('/notifiche')}
        >
          <IconBell size={18} />
          {unreadCount > 0 && (
            <span className="su-header__bell-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
          )}
        </button>
      }
    />
  )
}
