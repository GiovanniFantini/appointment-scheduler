import type { ReactNode } from 'react'
import './StatusChip.css'

export type StatusChipVariant =
  | 'neutral'
  | 'accent'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'

export interface StatusChipProps {
  variant?: StatusChipVariant
  /** Icona opzionale a sinistra del testo (es. <TiBeach size={12} />). */
  icon?: ReactNode
  children: ReactNode
  className?: string
}

/** Pill-chip di stato (mirror di .chip/.chipgreen/.chipamber del mockup). */
export function StatusChip({ variant = 'neutral', icon, children, className = '' }: StatusChipProps) {
  return (
    <span className={`su-status-chip su-status-chip--${variant} ${className}`}>
      {icon && <span className="su-status-chip__icon">{icon}</span>}
      {children}
    </span>
  )
}
