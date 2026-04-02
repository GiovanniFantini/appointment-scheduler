import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../../lib/axios'
import { MerchantUser } from '../../App'
import './DashboardPage.css'

interface DashboardPageProps {
  user: MerchantUser
}

interface Stats {
  totalEmployees: number
  upcomingEvents: number
  pendingRequests: number
}

interface QuickLink {
  path: string
  icon: string
  label: string
  desc: string
  feature?: string
}

const QUICK_LINKS: QuickLink[] = [
  { path: '/calendario', icon: '📅', label: 'Calendario', desc: 'Gestisci eventi e turni', feature: 'Calendario' },
  { path: '/richieste', icon: '📋', label: 'Richieste', desc: 'Approva richieste dipendenti', feature: 'Richieste' },
  { path: '/risorse', icon: '👥', label: 'Risorse', desc: 'Gestisci i dipendenti', feature: 'Risorse' },
  { path: '/ruoli', icon: '🔑', label: 'Ruoli', desc: 'Configura ruoli e permessi', feature: 'Ruoli' },
  { path: '/documenti', icon: '📁', label: 'Documenti', desc: 'Documenti HR e payroll', feature: 'Documenti' },
  { path: '/report', icon: '📊', label: 'Report', desc: 'Statistiche aziendali', feature: 'Report' },
]

export default function DashboardPage({ user }: DashboardPageProps) {
  const [stats, setStats] = useState<Stats>({ totalEmployees: 0, upcomingEvents: 0, pendingRequests: 0 })
  const [loadingStats, setLoadingStats] = useState(true)

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const today = new Date()
        const weekEnd = new Date(today)
        weekEnd.setDate(today.getDate() + 7)
        const from = today.toISOString().split('T')[0]
        const to = weekEnd.toISOString().split('T')[0]

        const [empRes, evtRes, reqRes] = await Promise.allSettled([
          apiClient.get('/employees'),
          apiClient.get(`/events?from=${from}&to=${to}`),
          apiClient.get('/employee-requests?status=0'),
        ])

        const employees = empRes.status === 'fulfilled' ? (empRes.value.data as unknown[]) : []
        const events = evtRes.status === 'fulfilled' ? (evtRes.value.data as unknown[]) : []
        const requests = reqRes.status === 'fulfilled' ? (reqRes.value.data as unknown[]) : []

        setStats({
          totalEmployees: Array.isArray(employees) ? employees.length : 0,
          upcomingEvents: Array.isArray(events) ? events.length : 0,
          pendingRequests: Array.isArray(requests) ? requests.length : 0,
        })
      } catch {
        // silently fail - stats are informational only
      } finally {
        setLoadingStats(false)
      }
    }
    fetchStats()
  }, [])

  const visibleLinks = QUICK_LINKS.filter(link => {
    if (!link.feature) return true
    return user.activeFeatures?.includes(link.feature)
  })

  const greeting = () => {
    const h = new Date().getHours()
    if (h < 12) return 'Buongiorno'
    if (h < 18) return 'Buon pomeriggio'
    return 'Buonasera'
  }

  return (
    <div className="dashboard-page">
      <div className="dashboard-welcome">
        <h1>{greeting()}, {user.firstName}!</h1>
        <p>{user.companyName ? `Benvenuto su ${user.companyName}` : 'Benvenuto nella tua dashboard aziendale'}</p>
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue">👥</div>
          <div className="stat-info">
            <div className="stat-value">{loadingStats ? '—' : stats.totalEmployees}</div>
            <div className="stat-label">Dipendenti totali</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon indigo">📅</div>
          <div className="stat-info">
            <div className="stat-value">{loadingStats ? '—' : stats.upcomingEvents}</div>
            <div className="stat-label">Eventi questa settimana</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon amber">📋</div>
          <div className="stat-info">
            <div className="stat-value">{loadingStats ? '—' : stats.pendingRequests}</div>
            <div className="stat-label">Richieste in attesa</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green">✅</div>
          <div className="stat-info">
            <div className="stat-value">{visibleLinks.length}</div>
            <div className="stat-label">Funzionalità attive</div>
          </div>
        </div>
      </div>

      <div className="section-title">Accesso rapido</div>
      <div className="quick-links-grid">
        {visibleLinks.map(link => (
          <Link key={link.path} to={link.path} className="quick-link-card">
            <span className="quick-link-icon">{link.icon}</span>
            <span className="quick-link-label">{link.label}</span>
            <span className="quick-link-desc">{link.desc}</span>
          </Link>
        ))}
      </div>
    </div>
  )
}
