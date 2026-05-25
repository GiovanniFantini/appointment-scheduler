import { useCallback, useMemo, useState } from 'react'

/**
 * Hook controllabile per gestire lo stato di un wizard (step corrente + furthest).
 * Esposto per casi avanzati; nella maggior parte degli usi basta `<Wizard>` direttamente.
 */
export function useWizard(totalSteps: number, initialStep = 0) {
  const [current, setCurrent] = useState(Math.min(initialStep, totalSteps - 1))
  const [furthest, setFurthest] = useState(initialStep)

  const next = useCallback(() => {
    setCurrent((prev) => {
      const n = Math.min(prev + 1, totalSteps - 1)
      setFurthest((f) => Math.max(f, n))
      return n
    })
  }, [totalSteps])

  const back = useCallback(() => {
    setCurrent((prev) => Math.max(prev - 1, 0))
  }, [])

  const goTo = useCallback(
    (idx: number) => {
      if (idx >= 0 && idx <= furthest) setCurrent(idx)
    },
    [furthest]
  )

  return useMemo(
    () => ({
      current,
      furthest,
      next,
      back,
      goTo,
      isFirst: current === 0,
      isLast: current === totalSteps - 1
    }),
    [current, furthest, next, back, goTo, totalSteps]
  )
}
