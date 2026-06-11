import { useRef, useState, useCallback, type ReactNode } from 'react'
import './DayCarousel.css'

export interface DaySlide {
  /** Chiave stabile (es. la data ISO del giorno). */
  key: string
  /** Etichetta giorno mostrata in testa alla slide (es. "giovedì 11 giugno"). */
  label: string
  /** Contenuto della slide (lista timbrature/anomalie del giorno). */
  content: ReactNode
}

interface Props {
  slides: DaySlide[]
}

/**
 * Carousel a pagine: una slide a larghezza piena per giorno, con scroll-snap
 * orizzontale (swipe nativo su mobile) e frecce ‹ › su desktop. Tiene il
 * collassabile ad altezza contenuta — si scorre di lato invece di allungare
 * la pagina. Usato per storico timbrature e anomalie, così l'interazione è
 * identica in entrambi.
 */
export default function DayCarousel({ slides }: Props) {
  const trackRef = useRef<HTMLDivElement>(null)
  const [index, setIndex] = useState(0)

  // Tiene l'indicatore (dots/freccia) allineato allo scroll, anche da swipe.
  const onScroll = useCallback(() => {
    const track = trackRef.current
    if (!track) return
    const i = Math.round(track.scrollLeft / track.clientWidth)
    setIndex(prev => (prev === i ? prev : i))
  }, [])

  const goTo = useCallback((i: number) => {
    const track = trackRef.current
    if (!track) return
    const clamped = Math.max(0, Math.min(i, slides.length - 1))
    track.scrollTo({ left: clamped * track.clientWidth, behavior: 'smooth' })
  }, [slides.length])

  if (slides.length === 0) return null

  const single = slides.length === 1

  return (
    <div className="dc">
      {!single && (
        <div className="dc-nav">
          <button
            type="button"
            className="dc-arrow"
            onClick={() => goTo(index - 1)}
            disabled={index === 0}
            aria-label="Giorno precedente"
          >
            ‹
          </button>
          <span className="dc-day-label">{slides[index]?.label}</span>
          <button
            type="button"
            className="dc-arrow"
            onClick={() => goTo(index + 1)}
            disabled={index === slides.length - 1}
            aria-label="Giorno successivo"
          >
            ›
          </button>
        </div>
      )}

      <div className="dc-track" ref={trackRef} onScroll={onScroll}>
        {slides.map(s => (
          <div className="dc-slide" key={s.key}>
            {single && <div className="dc-day-label dc-day-label--inline">{s.label}</div>}
            {s.content}
          </div>
        ))}
      </div>

      {!single && (
        <div className="dc-dots">
          {slides.map((s, i) => (
            <button
              type="button"
              key={s.key}
              className={`dc-dot ${i === index ? 'dc-dot--active' : ''}`}
              onClick={() => goTo(i)}
              aria-label={`Vai a ${s.label}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}
