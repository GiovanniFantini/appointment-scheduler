import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react'
import './Button.css'

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon: ReactNode
  /** Etichetta a11y (obbligatoria per icon-only button). */
  ariaLabel: string
  variant?: 'default' | 'danger'
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(function IconButton(
  { icon, ariaLabel, variant = 'default', className = '', type = 'button', ...rest },
  ref
) {
  const classes = ['su-iconbtn', variant === 'danger' ? 'su-iconbtn--danger' : '', className]
    .filter(Boolean)
    .join(' ')
  return (
    <button ref={ref} type={type} className={classes} aria-label={ariaLabel} {...rest}>
      {icon}
    </button>
  )
})
