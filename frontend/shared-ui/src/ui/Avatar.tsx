import type { CSSProperties } from 'react'
import './Avatar.css'

export type AvatarSize = 'sm' | 'md' | 'lg' | 'xl'

export interface AvatarProps {
  /** Nome completo o stringa da cui derivare iniziali + colore. */
  name?: string
  /** Iniziali esplicite (sovrascrivono quelle derivate da name). */
  initials?: string
  size?: AvatarSize
  /** Forza un colore di sfondo invece di quello derivato dall'hash. */
  color?: string
  className?: string
  style?: CSSProperties
}

const PALETTE_SIZE = 7

/** Hash deterministico → indice 1..7 della palette avatar dei token. */
function paletteIndex(seed: string): number {
  let h = 0
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0
  return (h % PALETTE_SIZE) + 1
}

function deriveInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export function Avatar({ name = '', initials, size = 'md', color, className = '', style }: AvatarProps) {
  const text = initials ?? deriveInitials(name)
  const bg = color ?? `var(--su-avatar-${paletteIndex(name || text)})`
  return (
    <span
      className={`su-avatar su-avatar--${size} ${className}`}
      style={{ background: bg, ...style }}
      aria-hidden="true"
    >
      {text}
    </span>
  )
}
