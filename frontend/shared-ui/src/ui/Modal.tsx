import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { IconButton } from './IconButton'
import { IconClose } from '../icons'
import './Modal.css'

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl'

export interface ModalProps {
  open: boolean
  onClose: () => void
  title?: string
  size?: ModalSize
  children: ReactNode
  footer?: ReactNode
  /** Se true (default), il click sul backdrop chiude il modal. */
  closeOnBackdrop?: boolean
}

export function Modal({
  open,
  onClose,
  title,
  size = 'md',
  children,
  footer,
  closeOnBackdrop = true
}: ModalProps) {
  useEffect(() => {
    if (!open) return
    function onEsc(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onEsc)
    const prevOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onEsc)
      document.body.style.overflow = prevOverflow
    }
  }, [open, onClose])

  if (!open) return null

  return createPortal(
    <div
      className="su-modal__backdrop"
      onClick={(e) => {
        if (closeOnBackdrop && e.target === e.currentTarget) onClose()
      }}
      role="presentation"
    >
      <div className={`su-modal su-modal--size-${size}`} role="dialog" aria-modal="true" aria-label={title}>
        {title !== undefined && (
          <div className="su-modal__header">
            <h2 className="su-modal__title">{title}</h2>
            <IconButton icon={<IconClose />} ariaLabel="Chiudi" onClick={onClose} />
          </div>
        )}
        <div className="su-modal__body">{children}</div>
        {footer && <div className="su-modal__footer">{footer}</div>}
      </div>
    </div>,
    document.body
  )
}
