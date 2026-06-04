import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import './BottomSheet.css'

export interface BottomSheetProps {
  open: boolean
  onClose: () => void
  title?: string
  children: ReactNode
  footer?: ReactNode
  /** Se true (default), il click sul backdrop chiude lo sheet. */
  closeOnBackdrop?: boolean
  /** Nasconde la X in header (la maniglia resta come affordance di chiusura). */
  hideClose?: boolean
}

/**
 * Bottom-sheet: stessa meccanica di Modal (portal, ESC, scroll-lock) ma
 * ancorato in basso con maniglia e top radius. Su desktop (>768px) si centra
 * come una card modale; su mobile aderisce al fondo. Props compatibili con
 * Modal così i call-site si convertono rinominando il componente.
 */
export function BottomSheet({
  open,
  onClose,
  title,
  children,
  footer,
  closeOnBackdrop = true,
  hideClose = false
}: BottomSheetProps) {
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
      className="su-sheet__backdrop"
      onClick={(e) => {
        if (closeOnBackdrop && e.target === e.currentTarget) onClose()
      }}
      role="presentation"
    >
      <div className="su-sheet" role="dialog" aria-modal="true" aria-label={title}>
        <button
          type="button"
          className="su-sheet__handle"
          aria-label="Chiudi"
          onClick={onClose}
        />
        {title !== undefined && (
          <div className="su-sheet__header">
            <h2 className="su-sheet__title">{title}</h2>
            {!hideClose && (
              <button type="button" className="su-sheet__close" aria-label="Chiudi" onClick={onClose}>
                <svg width={20} height={20} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                  <path d="M18 6l-12 12" />
                  <path d="M6 6l12 12" />
                </svg>
              </button>
            )}
          </div>
        )}
        <div className="su-sheet__body">{children}</div>
        {footer && <div className="su-sheet__footer">{footer}</div>}
      </div>
    </div>,
    document.body
  )
}
