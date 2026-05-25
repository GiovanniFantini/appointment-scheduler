import type { HTMLAttributes, ReactNode } from 'react'
import './misc.css'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  /** Versione meno marcata (no shadow, surface anziché elevated). */
  flat?: boolean
}

export function Card({ children, flat, className = '', ...rest }: CardProps) {
  return (
    <div className={`su-card ${flat ? 'su-card--flat' : ''} ${className}`} {...rest}>
      {children}
    </div>
  )
}
