import type { ReactNode } from 'react'
import { Breadcrumb, type BreadcrumbItem } from './Breadcrumb'
import './PageHeader.css'

interface PageHeaderProps {
  title: string
  subtitle?: string
  breadcrumbs?: BreadcrumbItem[]
  actions?: ReactNode
}

export function PageHeader({ title, subtitle, breadcrumbs, actions }: PageHeaderProps) {
  return (
    <div className="su-pageheader">
      <div className="su-pageheader__left">
        {breadcrumbs && breadcrumbs.length > 0 && <Breadcrumb items={breadcrumbs} />}
        <h1 className="su-pageheader__title">{title}</h1>
        {subtitle && <p className="su-pageheader__subtitle">{subtitle}</p>}
      </div>
      {actions && <div className="su-pageheader__actions">{actions}</div>}
    </div>
  )
}
