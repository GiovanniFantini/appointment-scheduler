import { forwardRef, useRef, type InputHTMLAttributes } from 'react'
import './form.css'

interface DatePickerProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  error?: boolean
}

/**
 * Wrapper su <input type="date"> che apre il picker nativo anche su click/focus
 * (rispetta la regola CLAUDE.md: nativeDateInputProps).
 */
export const DatePicker = forwardRef<HTMLInputElement, DatePickerProps>(function DatePicker(
  { className = '', error, onClick, onFocus, ...rest },
  ref
) {
  const innerRef = useRef<HTMLInputElement | null>(null)

  const tryShowPicker = () => {
    const el = innerRef.current
    if (el && typeof (el as HTMLInputElement & { showPicker?: () => void }).showPicker === 'function') {
      try {
        ;(el as HTMLInputElement & { showPicker: () => void }).showPicker()
      } catch {
        /* alcuni browser bloccano showPicker se non triggered direttamente da user gesture */
      }
    }
  }

  return (
    <input
      ref={(el) => {
        innerRef.current = el
        if (typeof ref === 'function') ref(el)
        else if (ref) (ref as React.MutableRefObject<HTMLInputElement | null>).current = el
      }}
      type="date"
      className={`su-input ${error ? 'su-input--error' : ''} ${className}`}
      onClick={(e) => {
        tryShowPicker()
        onClick?.(e)
      }}
      onFocus={(e) => {
        tryShowPicker()
        onFocus?.(e)
      }}
      {...rest}
    />
  )
})
