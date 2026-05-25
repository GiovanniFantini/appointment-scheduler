import type { ReactNode } from 'react'
import './form.css'

interface FormFieldProps {
  label?: string
  htmlFor?: string
  required?: boolean
  helper?: ReactNode
  error?: ReactNode
  children: ReactNode
}

export function FormField({ label, htmlFor, required, helper, error, children }: FormFieldProps) {
  return (
    <div className="su-field">
      {label && (
        <label className="su-field__label" htmlFor={htmlFor}>
          {label}
          {required && <span className="su-field__label-required" aria-hidden>*</span>}
        </label>
      )}
      {children}
      {error ? (
        <span className="su-field__error" role="alert">{error}</span>
      ) : helper ? (
        <span className="su-field__helper">{helper}</span>
      ) : null}
    </div>
  )
}
