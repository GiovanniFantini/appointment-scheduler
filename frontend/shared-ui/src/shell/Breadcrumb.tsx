import { Link } from 'react-router-dom'
import { IconChevronRight } from '../icons'
import './PageHeader.css'

export interface BreadcrumbItem {
  label: string
  to?: string
}

interface BreadcrumbProps {
  items: BreadcrumbItem[]
}

export function Breadcrumb({ items }: BreadcrumbProps) {
  return (
    <nav className="su-breadcrumb" aria-label="Breadcrumb">
      {items.map((item, idx) => {
        const isLast = idx === items.length - 1
        return (
          <span key={`${idx}-${item.label}`} style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            {item.to && !isLast ? (
              <Link to={item.to}>{item.label}</Link>
            ) : (
              <span className={isLast ? 'su-breadcrumb__current' : undefined}>{item.label}</span>
            )}
            {!isLast && (
              <span className="su-breadcrumb__sep" aria-hidden>
                <IconChevronRight size={12} />
              </span>
            )}
          </span>
        )
      })}
    </nav>
  )
}
