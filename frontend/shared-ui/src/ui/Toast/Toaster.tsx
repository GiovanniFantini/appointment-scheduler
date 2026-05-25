import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode
} from 'react'
import { createPortal } from 'react-dom'
import { IconAlert, IconClose, IconInfo, IconSuccess } from '../../icons'
import './Toast.css'

export type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface ToastOpts {
  title?: string
  duration?: number
}

interface ToastItem {
  id: number
  variant: ToastVariant
  message: string
  title?: string
  leaving?: boolean
}

interface ToastApi {
  success: (msg: string, opts?: ToastOpts) => void
  error: (msg: string, opts?: ToastOpts) => void
  warning: (msg: string, opts?: ToastOpts) => void
  info: (msg: string, opts?: ToastOpts) => void
  dismiss: (id: number) => void
}

const ToastCtx = createContext<ToastApi | null>(null)

let toastSeq = 1
const DEFAULT_DURATION: Record<ToastVariant, number> = {
  success: 3500,
  info: 4000,
  warning: 5000,
  error: 6000
}

interface ToasterProps {
  children: ReactNode
}

export function Toaster({ children }: ToasterProps) {
  const [items, setItems] = useState<ToastItem[]>([])
  const timeoutsRef = useRef<Map<number, ReturnType<typeof setTimeout>>>(new Map())

  const dismiss = useCallback((id: number) => {
    setItems((prev) => prev.map((it) => (it.id === id ? { ...it, leaving: true } : it)))
    const removeTimer = setTimeout(() => {
      setItems((prev) => prev.filter((it) => it.id !== id))
    }, 200)
    timeoutsRef.current.set(-id, removeTimer)
  }, [])

  const push = useCallback(
    (variant: ToastVariant, message: string, opts?: ToastOpts) => {
      const id = toastSeq++
      setItems((prev) => [...prev, { id, variant, message, title: opts?.title }])
      const duration = opts?.duration ?? DEFAULT_DURATION[variant]
      if (duration > 0) {
        const t = setTimeout(() => dismiss(id), duration)
        timeoutsRef.current.set(id, t)
      }
    },
    [dismiss]
  )

  useEffect(
    () => () => {
      timeoutsRef.current.forEach((t) => clearTimeout(t))
      timeoutsRef.current.clear()
    },
    []
  )

  const api = useMemo<ToastApi>(
    () => ({
      success: (msg, opts) => push('success', msg, opts),
      error: (msg, opts) => push('error', msg, opts),
      warning: (msg, opts) => push('warning', msg, opts),
      info: (msg, opts) => push('info', msg, opts),
      dismiss
    }),
    [push, dismiss]
  )

  return (
    <ToastCtx.Provider value={api}>
      {children}
      {createPortal(
        <div className="su-toaster" role="region" aria-label="Notifiche">
          {items.map((it) => (
            <ToastView key={it.id} item={it} onClose={() => dismiss(it.id)} />
          ))}
        </div>,
        document.body
      )}
    </ToastCtx.Provider>
  )
}

function ToastView({ item, onClose }: { item: ToastItem; onClose: () => void }) {
  const IconCmp = item.variant === 'success'
    ? IconSuccess
    : item.variant === 'error' || item.variant === 'warning'
      ? IconAlert
      : IconInfo
  return (
    <div className={`su-toast su-toast--${item.variant} ${item.leaving ? 'su-toast--leaving' : ''}`} role="alert">
      <span className={`su-toast__icon su-toast__icon--${item.variant}`} aria-hidden>
        <IconCmp size={18} />
      </span>
      <div className="su-toast__body">
        {item.title && <div className="su-toast__title">{item.title}</div>}
        <div className="su-toast__msg">{item.message}</div>
      </div>
      <button type="button" className="su-toast__close" onClick={onClose} aria-label="Chiudi notifica">
        <IconClose size={14} />
      </button>
    </div>
  )
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastCtx)
  if (!ctx) throw new Error('useToast() richiede <Toaster> nell\'albero dei componenti')
  return ctx
}
