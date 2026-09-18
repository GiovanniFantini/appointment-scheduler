import { forwardRef, type InputHTMLAttributes } from 'react'
import './form.css'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { className = '', error, ...rest },
  ref
) {
  return (
    <input data-activity="shared-ui.form.Input.1"
      ref={ref}
      className={`su-input ${error ? 'su-input--error' : ''} ${className}`}
      {...rest}
    />
  )
})
