import { useEffect, useState } from 'react'
import { PageHeader } from '../shell/PageHeader'
import './profile.css'

type Theme = 'dark' | 'light'

const STORAGE_KEY = 'pref:theme'

export function getStoredTheme(): Theme {
  if (typeof window === 'undefined') return 'dark'
  const v = window.localStorage.getItem(STORAGE_KEY)
  return v === 'light' ? 'light' : 'dark'
}

export function applyTheme(theme: Theme) {
  if (typeof document === 'undefined') return
  document.documentElement.setAttribute('data-theme', theme)
}

/**
 * Inizializza il tema da localStorage all'avvio dell'app.
 * Va chiamato una sola volta in main.tsx prima del render.
 */
export function initTheme() {
  applyTheme(getStoredTheme())
}

interface ThemeOption {
  value: Theme
  title: string
  desc: string
}

const OPTIONS: ThemeOption[] = [
  { value: 'dark', title: 'Scuro', desc: 'Sfondo scuro, riposante in ambienti poco illuminati (default).' },
  { value: 'light', title: 'Chiaro', desc: 'Sfondo chiaro, classico — utile alla luce diretta del sole.' }
]

export function PreferencesPage() {
  const [theme, setTheme] = useState<Theme>(getStoredTheme())

  useEffect(() => {
    applyTheme(theme)
    localStorage.setItem(STORAGE_KEY, theme)
  }, [theme])

  return (
    <div className="su-page">
      <PageHeader
        title="Preferenze"
        subtitle="Personalizza l'interfaccia. Le modifiche vengono salvate automaticamente."
      />

      <div className="su-page__section">
        <h2 className="su-page__section-title">Tema</h2>
        <p className="su-page__section-desc">
          Cambia l'aspetto dell'applicazione. La preferenza viene memorizzata in questo browser.
        </p>

        <div className="su-theme-toggle" role="radiogroup" aria-label="Tema interfaccia">
          {OPTIONS.map((opt) => {
            const active = theme === opt.value
            return (
              <button
                type="button"
                key={opt.value}
                role="radio"
                aria-checked={active}
                className={`su-theme-toggle__row ${active ? 'su-theme-toggle__row--active' : ''}`}
                onClick={() => setTheme(opt.value)}
              >
                <div>
                  <div className="su-theme-toggle__title">{opt.title}</div>
                  <div className="su-theme-toggle__desc">{opt.desc}</div>
                </div>
                <span className="su-theme-toggle__radio" aria-hidden />
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}
