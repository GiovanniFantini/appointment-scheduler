import type { ReactNode } from 'react'
import { Button } from './Button'
import { IconInbox } from '../icons'
import './misc.css'

interface EmptyStateProps {
  title: string
  description?: ReactNode
  icon?: ReactNode
  action?: { label: string; onClick: () => void }
}

export function EmptyState({ title, description, icon, action }: EmptyStateProps) {
  return (
    <div className="su-empty">
      <div className="su-empty__icon" aria-hidden>
        {icon ?? <IconInbox size={24} />}
      </div>
      <h3 className="su-empty__title">{title}</h3>
      {description && <p className="su-empty__desc">{description}</p>}
      {action && (
        <Button variant="primary" onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </div>
  )
}
