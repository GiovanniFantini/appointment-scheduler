import { forwardRef, type TextareaHTMLAttributes } from 'react'
import './form.css'

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { className = '', error, ...rest },
  ref
) {
  return (
    <textarea data-activity="shared-ui.form.Textarea.1"
      ref={ref}
      className={`su-textarea ${error ? 'su-textarea--error' : ''} ${className}`}
      {...rest}
    />
  )
})
