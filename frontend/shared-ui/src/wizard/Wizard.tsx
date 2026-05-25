import { useCallback, useState, type ReactNode } from 'react'
import { Button } from '../ui/Button'
import { StepIndicator } from './StepIndicator'
import { IconChevronLeft, IconChevronRight, IconCheck } from '../icons'
import './wizard.css'

export interface WizardStepConfig {
  id: string
  label: string
  title?: string
  description?: string
  /** Render del contenuto dello step. */
  content: ReactNode
  /**
   * Validazione asincrona/sync prima di passare al prossimo step.
   * Ritorna `true` (o `undefined`) per consentire l'avanzamento, `false` per bloccare.
   */
  validate?: () => boolean | void | Promise<boolean | void>
  /** Disabilita "Avanti" finché true. */
  disableNext?: boolean
}

export interface WizardProps {
  steps: WizardStepConfig[]
  onComplete: () => void | Promise<void>
  onCancel?: () => void
  completeLabel?: string
  /** Loading per il bottone finale. */
  completing?: boolean
}

export function Wizard({
  steps,
  onComplete,
  onCancel,
  completeLabel = 'Conferma',
  completing
}: WizardProps) {
  const [current, setCurrent] = useState(0)
  const [furthest, setFurthest] = useState(0)
  const [validating, setValidating] = useState(false)

  const isLast = current === steps.length - 1
  const step = steps[current]

  const handleNext = useCallback(async () => {
    if (!step) return
    if (step.validate) {
      setValidating(true)
      try {
        const ok = await step.validate()
        if (ok === false) return
      } finally {
        setValidating(false)
      }
    }
    if (isLast) {
      await onComplete()
    } else {
      const next = current + 1
      setCurrent(next)
      setFurthest((f) => Math.max(f, next))
    }
  }, [step, isLast, onComplete, current])

  const handleBack = useCallback(() => {
    setCurrent((c) => Math.max(c - 1, 0))
  }, [])

  return (
    <div className="su-wizard">
      <StepIndicator
        steps={steps.map((s) => ({ id: s.id, label: s.label }))}
        current={current}
        furthest={furthest}
      />

      <div className="su-wizard__step-body">
        {step?.title && <h3 className="su-wizard__step-title">{step.title}</h3>}
        {step?.description && <p className="su-wizard__step-desc">{step.description}</p>}
        <div>{step?.content}</div>
      </div>

      <div className="su-wizard__actions">
        <div>
          {onCancel && (
            <Button variant="ghost" onClick={onCancel} disabled={completing || validating}>
              Annulla
            </Button>
          )}
        </div>
        <div className="su-wizard__actions-right">
          <Button
            variant="secondary"
            onClick={handleBack}
            disabled={current === 0 || completing || validating}
            leftIcon={<IconChevronLeft size={14} />}
          >
            Indietro
          </Button>
          <Button
            variant="primary"
            onClick={handleNext}
            loading={validating || completing}
            disabled={step?.disableNext}
            rightIcon={isLast ? <IconCheck size={14} /> : <IconChevronRight size={14} />}
          >
            {isLast ? completeLabel : 'Avanti'}
          </Button>
        </div>
      </div>
    </div>
  )
}
