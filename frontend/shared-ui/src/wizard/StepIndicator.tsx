import { IconCheck } from '../icons'
import './wizard.css'

interface StepIndicatorProps {
  steps: Array<{ id: string; label: string }>
  current: number
  /** Step massimo già "visto" — utile per disegnare check sui passi completati. */
  furthest?: number
}

export function StepIndicator({ steps, current, furthest }: StepIndicatorProps) {
  const maxDone = Math.max(current, furthest ?? -1)
  return (
    <ol className="su-stepper" aria-label="Wizard steps">
      {steps.map((s, idx) => {
        const isActive = idx === current
        const isDone = idx < maxDone
        return (
          <li
            key={s.id}
            className={`su-stepper__item ${isActive ? 'su-stepper__item--active' : ''} ${isDone ? 'su-stepper__item--done' : ''}`}
            aria-current={isActive ? 'step' : undefined}
          >
            <span className="su-stepper__dot">{isDone ? <IconCheck size={14} /> : idx + 1}</span>
            <span className="su-stepper__label">{s.label}</span>
            {idx < steps.length - 1 && <span className="su-stepper__connector" aria-hidden />}
          </li>
        )
      })}
    </ol>
  )
}
