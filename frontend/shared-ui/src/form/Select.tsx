import { forwardRef, type SelectHTMLAttributes } from 'react'
import './form.css'

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  error?: boolean
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { className = '', error, children, ...rest },
  ref
) {
  return (
    <select
      ref={ref}
      className={`su-select ${error ? 'su-select--error' : ''} ${className}`}
      {...rest}
    >
      {children}
    </select>
  )
})
