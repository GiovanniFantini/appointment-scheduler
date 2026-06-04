import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  AppShell,
  IconBell,
  IconLogout,
  IconSettings,
  IconSwitch,
  IconUser,
  TiBell,
  TiCalendar,
  TiClock,
  TiHome,
  TiInbox,
  TiUsers,
  TiSparkles,
  TiBuildingStore,
  TiPackage,
  TiFileText,
  TiPlus,
  useAuth,
  type AppShellFab,
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

const ICON_SIZE = 18

const ALL_NAV: EmpNavItem[] = [
  { path: '/', label: 'Dashboard', exact: true, icon: <TiHome size={ICON_SIZE} /> },
  { path: '/timbratura', label: 'Timbratura', feature: 'Timbratura', icon: <TiClock size={ICON_SIZE} /> },
  { path: '/calendario', label: 'Calendario', feature: 'Calendario', icon: <TiCalendar size={ICON_SIZE} /> },
  { path: '/pianificazione', label: 'Pianificazione', feature: 'Calendario', minLevel: 'Operator', icon: <TiCalendar size={ICON_SIZE} /> },
  { path: '/richieste', label: 'Richieste', feature: 'Richieste', icon: <TiInbox size={ICON_SIZE} /> },
  { path: '/risorse', label: 'Risorse', feature: 'Risorse', icon: <TiUsers size={ICON_SIZE} /> },
  { path: '/mansioni', label: 'Mansioni', feature: 'Mansioni', icon: <TiSparkles size={ICON_SIZE} /> },
  { path: '/filiali', label: 'Filiali', feature: 'Filiali', icon: <TiBuildingStore size={ICON_SIZE} /> },
  { path: '/magazzino', label: 'Magazzino', feature: 'Magazzino', icon: <TiPackage size={ICON_SIZE} /> },
  { path: '/documenti', label: 'Documenti', feature: 'Documenti', icon: <TiFileText size={ICON_SIZE} /> },
  { path: '/notifiche', label: 'Notifiche', icon: <TiBell size={ICON_SIZE} /> }
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

  // FAB "nuovo turno": solo su calendario/pianificazione e con permesso di pianificare.
  const onPlanningRoute =
    location.pathname.startsWith('/calendario') || location.pathname.startsWith('/pianificazione')
  const canPlan = user.activeFeatures?.includes('Calendario') && hasFeatureLevel(user, 'Calendario', 'Operator')
  const fab: AppShellFab | undefined =
    onPlanningRoute && canPlan
      ? {
          icon: <TiPlus size={24} />,
          label: 'Nuovo turno',
          onClick: () => window.dispatchEvent(new CustomEvent('turnis:new-shift'))
        }
      : undefined

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
      bottomNav
      fab={fab}
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
