import { useState, useEffect, useCallback } from 'react'
import apiClient from '../../lib/axios'
import './NotifichePage.css'

interface Notification {
  id: number
  title: string
  message: string
  isRead: boolean
  createdAt: string
  type?: string
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
  return d.toLocaleDateString('it-IT', { day: 'numeric', month: 'short' })
}

export default function NotifichePage() {
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
        <div className="notifiche-loading">
          <div className="spinner" />
        </div>
      ) : notifications.length === 0 ? (
        <div className="notifiche-empty">
          <div className="empty-icon">
            <svg viewBox="0 0 24 24" fill="none">
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </div>
          <p className="empty-title">Nessuna notifica</p>
          <p className="empty-subtitle">Sei aggiornato! Non ci sono nuove notifiche.</p>
        </div>
      ) : (
        <div className="notifications-list">
          {notifications.map(notif => (
            <div
              key={notif.id}
              className={`notification-item ${!notif.isRead ? 'notification-item--unread' : ''}`}
              onClick={() => !notif.isRead && handleMarkRead(notif.id)}
            >
              <div className="notification-dot-wrapper">
                {!notif.isRead && <span className="notification-dot" />}
              </div>
              <div className="notification-body">
                <div className="notification-top">
                  <span className="notification-title">{notif.title}</span>
                  <span className="notification-time">{formatDate(notif.createdAt)}</span>
                </div>
                {notif.message && (
                  <p className="notification-message">{notif.message}</p>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
