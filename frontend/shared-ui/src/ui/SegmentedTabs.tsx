import type { ReactNode } from 'react'
import './SegmentedTabs.css'

export interface SegmentedTabOption<T extends string = string> {
  value: T
  label: ReactNode
  /** Contatore opzionale mostrato come badge accanto al label. */
  count?: number
}

export interface SegmentedTabsProps<T extends string = string> {
  options: SegmentedTabOption<T>[]
  value: T
  onChange: (value: T) => void
  className?: string
}

/** Controllo a tab segmentate (mirror di .tabseg del mockup). */
export function SegmentedTabs<T extends string = string>({
  options,
  value,
  onChange,
  className = ''
}: SegmentedTabsProps<T>) {
  return (
    <div className={`su-segtabs ${className}`} role="tablist">
      {options.map((opt) => {
        const active = opt.value === value
        return (
          <button
            key={opt.value}
            type="button"
            role="tab"
            aria-selected={active}
            className={`su-segtabs__btn ${active ? 'su-segtabs__btn--active' : ''}`}
            onClick={() => onChange(opt.value)}
          >
            {opt.label}
            {opt.count != null && opt.count > 0 && (
              <span className="su-segtabs__count">{opt.count}</span>
            )}
          </button>
        )
      })}
    </div>
  )
}
