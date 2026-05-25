import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { EmptyState, Skeleton } from '@scheduler/ui'
import apiClient from '../../lib/axios'
import { formatBrowserDate } from '../../lib/dateUtils'
import './NotifichePage.css'

// Allineato all'enum C# NotificationType (serializzato come numero dall'API).
const NOTIFICATION_TYPE_DOCUMENT_PUBLISHED = 7

interface Notification {
  id: number
  title: string
  message: string
  isRead: boolean
  createdAt: string
  type?: number
  relatedEntityId?: number
}

/** Emoji rappresentativa per tipo di notifica. */
function getNotificationIcon(type?: number): string {
  switch (type) {
    case NOTIFICATION_TYPE_DOCUMENT_PUBLISHED:
      return '📄'
    default:
      return '🔔'
  }
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Adesso'
  if (diffMins < 60) return `${diffMins} min fa`
  if (diffHours < 24) return `${diffHours} ore fa`
  if (diffDays === 1) return 'Ieri'
  return formatBrowserDate(d)
}

export default function NotifichePage() {
  const navigate = useNavigate()
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [markingAll, setMarkingAll] = useState(false)

  const fetchNotifications = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await apiClient.get<Notification[]>('/notifications')
      setNotifications(Array.isArray(data) ? data : [])
    } catch {
      setError('Errore nel caricamento delle notifiche')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchNotifications()
  }, [fetchNotifications])

  const handleMarkRead = async (id: number) => {
    try {
      await apiClient.patch(`/notifications/${id}/read`)
      setNotifications(prev =>
        prev.map(n => n.id === id ? { ...n, isRead: true } : n)
      )
    } catch {
      // silently fail
    }
  }

  const handleMarkAllRead = async () => {
    setMarkingAll(true)
    try {
      await apiClient.patch('/notifications/read-all')
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
    } catch {
      // silently fail
    } finally {
      setMarkingAll(false)
    }
  }

  /** Rotta di destinazione per una notifica, se ne ha una. */
  const getNotificationLink = (notif: Notification): string | null => {
    if (notif.type === NOTIFICATION_TYPE_DOCUMENT_PUBLISHED) {
      return '/documenti'
    }
    return null
  }

  const handleNotificationClick = (notif: Notification) => {
    if (!notif.isRead) {
      handleMarkRead(notif.id)
    }
    const link = getNotificationLink(notif)
    if (link) {
      navigate(link)
    }
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  return (
    <div className="notifiche-page">
      <div className="notifiche-header">
        <div className="notifiche-header-left">
          <h1 className="notifiche-title">Notifiche</h1>
          {unreadCount > 0 && (
            <span className="unread-count-badge">{unreadCount} non lette</span>
          )}
        </div>
        {unreadCount > 0 && (
          <button
            className="btn-mark-all"
            onClick={handleMarkAllRead}
            disabled={markingAll}
          >
            {markingAll ? 'Segno come lette...' : 'Segna tutte come lette'}
          </button>
        )}
      </div>

      {error && <div className="notifiche-error">{error}</div>}

      {loading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {[0, 1, 2, 3].map(i => <Skeleton key={i} variant="box" height={64} />)}
        </div>
      ) : notifications.length === 0 ? (
        <EmptyState
          title="Nessuna notifica"
          description="Sei aggiornato! Non ci sono nuove notifiche."
        />
      ) : (
        <div className="notifications-list">
          {notifications.map(notif => {
            const isLinked = getNotificationLink(notif) !== null
            return (
              <div
                key={notif.id}
                className={`notification-item ${!notif.isRead ? 'notification-item--unread' : ''} ${isLinked ? 'notification-item--linked' : ''}`}
                onClick={() => handleNotificationClick(notif)}
              >
                <div className="notification-dot-wrapper">
                  {!notif.isRead && <span className="notification-dot" />}
                </div>
                <span className="notification-icon" aria-hidden="true">
                  {getNotificationIcon(notif.type)}
                </span>
                <div className="notification-body">
                  <div className="notification-top">
                    <span className="notification-title">{notif.title}</span>
                    <span className="notification-time">{formatDate(notif.createdAt)}</span>
                  </div>
                  {notif.message && (
                    <p className="notification-message">{notif.message}</p>
                  )}
                  {isLinked && (
                    <span className="notification-link-hint">Apri documento →</span>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
