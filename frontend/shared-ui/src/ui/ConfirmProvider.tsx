import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import { ConfirmDialog } from './ConfirmDialog'

interface ConfirmOptions {
  title: string
  message: ReactNode
  variant?: 'danger' | 'warning'
  confirmLabel?: string
  cancelLabel?: string
}

type ConfirmFn = (opts: ConfirmOptions) => Promise<boolean>

const ConfirmCtx = createContext<ConfirmFn | null>(null)

interface ConfirmProviderProps {
  children: ReactNode
}

export function ConfirmProvider({ children }: ConfirmProviderProps) {
  const [state, setState] = useState<(ConfirmOptions & { open: boolean }) | null>(null)
  const resolverRef = useRef<((v: boolean) => void) | null>(null)

  const confirm = useCallback<ConfirmFn>((opts) => {
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
      setState({ ...opts, open: true })
    })
  }, [])

  const handleResolve = useCallback((value: boolean) => {
    resolverRef.current?.(value)
    resolverRef.current = null
    setState((prev) => (prev ? { ...prev, open: false } : prev))
  }, [])

  const value = useMemo(() => confirm, [confirm])

  return (
    <ConfirmCtx.Provider value={value}>
      {children}
      {state && (
        <ConfirmDialog
          open={state.open}
          title={state.title}
          message={state.message}
          variant={state.variant ?? 'danger'}
          confirmLabel={state.confirmLabel}
          cancelLabel={state.cancelLabel}
          onConfirm={() => handleResolve(true)}
          onCancel={() => handleResolve(false)}
        />
      )}
    </ConfirmCtx.Provider>
  )
}

export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmCtx)
  if (!ctx) throw new Error('useConfirm() richiede <ConfirmProvider> nell\'albero dei componenti')
  return ctx
}
