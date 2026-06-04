import { Avatar, type AvatarSize } from './Avatar'

export interface AvatarStackPerson {
  name?: string
  initials?: string
  color?: string
}

export interface AvatarStackProps {
  people: AvatarStackPerson[]
  size?: AvatarSize
  /** Numero massimo di avatar mostrati; il resto diventa "+N". */
  max?: number
  className?: string
}

export function AvatarStack({ people, size = 'sm', max = 4, className = '' }: AvatarStackProps) {
  const shown = people.slice(0, max)
  const extra = people.length - shown.length
  return (
    <span className={`su-avatar-stack ${className}`}>
      {shown.map((p, i) => (
        <Avatar key={i} name={p.name} initials={p.initials} color={p.color} size={size} />
      ))}
      {extra > 0 && <span className={`su-avatar-stack__more su-avatar-stack__more--${size}`}>+{extra}</span>}
    </span>
  )
}
