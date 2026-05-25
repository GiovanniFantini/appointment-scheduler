import type { ReactNode } from 'react'
import { Modal } from './Modal'
import { Button } from './Button'
import { IconAlert } from '../icons'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: ReactNode
  variant?: 'danger' | 'warning'
  confirmLabel?: string
  cancelLabel?: string
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  open,
  title,
  message,
  variant = 'danger',
  confirmLabel = 'Conferma',
  cancelLabel = 'Annulla',
  loading,
  onConfirm,
  onCancel
}: ConfirmDialogProps) {
  return (
    <Modal
      open={open}
      onClose={onCancel}
      title={title}
      size="sm"
      closeOnBackdrop={!loading}
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </Button>
          <Button variant={variant === 'danger' ? 'danger' : 'primary'} onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <div className="su-confirm__icon-wrap">
        <div className={`su-confirm__icon su-confirm__icon--${variant}`}>
          <IconAlert size={20} />
        </div>
        <div className="su-confirm__msg">{message}</div>
      </div>
    </Modal>
  )
}
