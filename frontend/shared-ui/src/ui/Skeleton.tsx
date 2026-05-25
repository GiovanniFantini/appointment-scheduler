import type { CSSProperties } from 'react'
import './misc.css'

interface SkeletonProps {
  variant?: 'text' | 'box' | 'circle'
  width?: number | string
  height?: number | string
  /** Numero di righe (solo per variant=text). */
  count?: number
  style?: CSSProperties
}

export function Skeleton({ variant = 'text', width, height, count = 1, style }: SkeletonProps) {
  const cls = `su-skeleton ${variant === 'circle' ? 'su-skeleton--circle' : ''} ${variant === 'text' ? 'su-skeleton--text' : ''}`
  const inline: CSSProperties = {
    width: width ?? (variant === 'circle' ? height : '100%'),
    height: height ?? (variant === 'text' ? undefined : '80px'),
    ...style
  }

  if (variant === 'text' && count > 1) {
    return (
      <>
        {Array.from({ length: count }).map((_, i) => (
          <span key={i} className={cls} style={{ ...inline, width: i === count - 1 ? '70%' : inline.width }} />
        ))}
      </>
    )
  }
  return <span className={cls} style={inline} />
}
