import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react'
import './form.css'

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: ReactNode
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { label, className = '', disabled, ...rest },
  ref
) {
  return (
    <label className={`su-checkbox ${disabled ? 'su-checkbox--disabled' : ''} ${className}`}>
      <input ref={ref} type="checkbox" disabled={disabled} {...rest} />
      <span>{label}</span>
    </label>
  )
})
