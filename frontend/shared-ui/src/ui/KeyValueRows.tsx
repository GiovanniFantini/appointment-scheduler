import type { ReactNode } from 'react'
import './KeyValueRows.css'

export interface KeyValueRow {
  label: ReactNode
  value: ReactNode
  /** Variante semantica del valore (es. 'success' per residuo positivo). */
  tone?: 'default' | 'success' | 'warning' | 'danger' | 'muted'
}

export interface KeyValueRowsProps {
  rows: KeyValueRow[]
  className?: string
}

/** Lista di righe label/valore (mirror di .row del mockup, per i dettagli). */
export function KeyValueRows({ rows, className = '' }: KeyValueRowsProps) {
  return (
    <div className={`su-kvrows ${className}`}>
      {rows.map((r, i) => (
        <div className="su-kvrow" key={i}>
          <span className="su-kvrow__label">{r.label}</span>
          <span className={`su-kvrow__value su-kvrow__value--${r.tone ?? 'default'}`}>{r.value}</span>
        </div>
      ))}
    </div>
  )
}
