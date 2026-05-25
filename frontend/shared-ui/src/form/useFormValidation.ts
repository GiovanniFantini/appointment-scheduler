import { useCallback, useState } from 'react'

export type ValidationRule<TValue> = (value: TValue, allValues: Record<string, unknown>) => string | undefined

export type FieldErrors<T extends Record<string, unknown>> = Partial<Record<keyof T, string>>

/**
 * Hook leggero di validazione client-side: nessuna dipendenza esterna.
 * Definisci le regole come funzione `(value, allValues) => errorString | undefined`.
 */
export function useFormValidation<T extends Record<string, unknown>>(
  rules: Partial<Record<keyof T, ValidationRule<any>[]>>
) {
  const [errors, setErrors] = useState<FieldErrors<T>>({})

  const validateField = useCallback(
    (field: keyof T, value: unknown, allValues: T): string | undefined => {
      const fieldRules = rules[field]
      if (!fieldRules) return undefined
      for (const rule of fieldRules) {
        const err = rule(value, allValues)
        if (err) return err
      }
      return undefined
    },
    [rules]
  )

  const validateAll = useCallback(
    (values: T): boolean => {
      const next: FieldErrors<T> = {}
      let ok = true
      ;(Object.keys(rules) as Array<keyof T>).forEach((field) => {
        const err = validateField(field, values[field], values)
        if (err) {
          next[field] = err
          ok = false
        }
      })
      setErrors(next)
      return ok
    },
    [rules, validateField]
  )

  const setError = useCallback((field: keyof T, error: string | undefined) => {
    setErrors((prev) => {
      const next = { ...prev }
      if (error) next[field] = error
      else delete next[field]
      return next
    })
  }, [])

  const clearErrors = useCallback(() => setErrors({}), [])

  return { errors, validateField, validateAll, setError, clearErrors }
}
