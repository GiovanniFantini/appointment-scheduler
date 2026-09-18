import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { trackActivity } from './activity'

export function ActivityObserver() {
  const location = useLocation()
  useEffect(() => { trackActivity('page.view', 'router') }, [location.pathname])
  return null
}
