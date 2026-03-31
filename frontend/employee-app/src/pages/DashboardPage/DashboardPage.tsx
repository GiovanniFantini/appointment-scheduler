import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { EmployeeUser } from '../../App'
import apiClient from '../../lib/axios'
import './DashboardPage.css'

interface CalendarEvent {
  id: number | string
  title: string
  start: string
  end?: string
  eventType?: string
  allDay?: boolean
}

interface Props {
  user: EmployeeUser
}

function getEventTypeLabel(type?: string): string {
  const map: Record<string, string> = {
    Turno: 'Turno',
    Ferie: 'Ferie',
    Permessi: 'Permesso',
    Malattia: 'Malattia',
    ChiusuraAziendale: 'Chiusura aziendale',
  }
  return type ? (map[type] ?? type) : 'Evento'
}

function getEventTypeColor(type?: string): string {
  const map: Record<string, string> = {
    Turno: '#3b82f6',
    Ferie: '#ec4899',
    Permessi: '#8b5cf6',
    Malattia: '#f59e0b',
    ChiusuraAziendale: '#475569',
  }
  return type ? (map[type] ?? '#6366f1') : '#6366f1'
}

export default function DashboardPage({ user }: Props) {
  const [todayEvents, setTodayEvents] = useState<CalendarEvent[]>([])
  const [weekEvents, setWeekEvents] = useState<CalendarEvent[]>([])
  const [pendingRequests, setPendingRequests] = useState<CalendarEvent[]>([])
  const [loading, setLoading] = useState(true)

  const today = new Date()
  const todayStr = today.toISOString().split('T')[0]
  const weekEnd = new Date(today)
  weekEnd.setDate(today.getDate() + 7)
  const weekEndStr = weekEnd.toISOString().split('T')[0]

  useEffect(() => {
    fetchEvents()
  }, [])

  const fetchEvents = async () => {
    try {
      const { data } = await apiClient.get<CalendarEvent[]>('/events/employee', {
        params: { start: todayStr, end: weekEndStr },
      })
      const events = Array.isArray(data) ? data : []
      const tEvents = events.filter(e => {
        const d = e.start?.split('T')[0]
        return d === todayStr
      })
      const wEvents = events.filter(e => {
        const d = e.start?.split('T')[0]
        return d && d > todayStr && d <= weekEndStr
      })
      const pReqs = events.filter(e =>
        ['Ferie', 'Permessi', 'Malattia'].includes(e.eventType ?? '')
      )
      setTodayEvents(tEvents)
      setWeekEvents(wEvents)
      setPendingRequests(pReqs)
    } catch {
      // silently fail
    } finally {
      setLoading(false)
    }
  }

  const formatTime = (dateStr: string) => {
    const d = new Date(dateStr)
    return d.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })
  }

  const dayOfWeek = today.toLocaleDateString('it-IT', { weekday: 'long' })
  const dateStr = today.toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' })

  return (
    <div className="dashboard-page">
      {/* Welcome section */}
      <div className="dashboard-welcome">
        <div className="dashboard-welcome-text">
          <h1 className="dashboard-greeting">Ciao, {user.firstName}!</h1>
          <p className="dashboard-date">{dayOfWeek.charAt(0).toUpperCase() + dayOfWeek.slice(1)}, {dateStr}</p>
          {user.companyName && (
            <p className="dashboard-company">{user.companyName}</p>
          )}
        </div>
      </div>

      {/* Stats row */}
      <div className="dashboard-stats">
        <div className="stat-card">
          <div className="stat-value">{todayEvents.length}</div>
          <div className="stat-label">Oggi</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{weekEvents.length}</div>
          <div className="stat-label">Questa settimana</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{pendingRequests.length}</div>
          <div className="stat-label">Richieste</div>
        </div>
      </div>

      <div className="dashboard-grid">
        {/* Today's events */}
        <div className="dashboard-card">
          <div className="dashboard-card-header">
            <h2 className="dashboard-card-title">Oggi</h2>
          </div>
          {loading ? (
            <div className="dashboard-loading">
              <div className="spinner" />
            </div>
          ) : todayEvents.length === 0 ? (
            <div className="dashboard-empty">
              <p>Nessun evento per oggi</p>
            </div>
          ) : (
            <div className="event-list">
              {todayEvents.map(event => (
                <div
                  key={event.id}
                  className="event-item"
                  style={{ borderLeftColor: getEventTypeColor(event.eventType) }}
                >
                  <div className="event-item-title">{event.title}</div>
                  <div className="event-item-meta">
                    <span
                      className="event-type-badge"
                      style={{ backgroundColor: getEventTypeColor(event.eventType) + '22', color: getEventTypeColor(event.eventType) }}
                    >
                      {getEventTypeLabel(event.eventType)}
                    </span>
                    {!event.allDay && event.start && (
                      <span className="event-time">{formatTime(event.start)}</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Quick links */}
        <div className="dashboard-card">
          <div className="dashboard-card-header">
            <h2 className="dashboard-card-title">Accesso rapido</h2>
          </div>
          <div className="quick-links">
            {user.activeFeatures?.includes('Calendario') && (
              <Link to="/calendario" className="quick-link">
                <div className="quick-link-icon quick-link-icon--blue">
                  <svg viewBox="0 0 24 24" fill="none">
                    <rect x="3" y="4" width="18" height="18" rx="2" stroke="currentColor" strokeWidth="2" />
                    <path d="M16 2v4M8 2v4M3 10h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </div>
                <span>Calendario</span>
              </Link>
            )}
            {user.activeFeatures?.includes('Richieste') && (
              <Link to="/richieste" className="quick-link">
                <div className="quick-link-icon quick-link-icon--purple">
                  <svg viewBox="0 0 24 24" fill="none">
                    <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </div>
                <span>Richieste</span>
              </Link>
            )}
            {user.activeFeatures?.includes('Documenti') && (
              <Link to="/documenti" className="quick-link">
                <div className="quick-link-icon quick-link-icon--green">
                  <svg viewBox="0 0 24 24" fill="none">
                    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </div>
                <span>Documenti</span>
              </Link>
            )}
            <Link to="/notifiche" className="quick-link">
              <div className="quick-link-icon quick-link-icon--sky">
                <svg viewBox="0 0 24 24" fill="none">
                  <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </div>
              <span>Notifiche</span>
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
