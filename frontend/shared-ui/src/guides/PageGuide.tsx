import { useEffect, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { driver, type Driver, type DriveStep } from 'driver.js'
import { availableGuideTours, guideStorageKey, type GuideAccess, type GuideTour, type PageGuideDefinition } from './types'
import 'driver.js/dist/driver.css'
import './guides.css'

interface Props {
  app: 'merchant' | 'employee'
  page: PageGuideDefinition
  user: GuideAccess & { userId: number; merchantId?: number }
}
interface History { dismissed?: boolean; viewed?: string[] }
const dialogSelector = '[role="dialog"], .modal-box, .modal-container, .inventory-modal, .wizard-box, .jam-modal, .copy-dialog'
function readHistory(key: string): History {
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? '{}')
    return { dismissed: value?.dismissed === true, viewed: Array.isArray(value?.viewed) ? value.viewed.filter((id: unknown) => typeof id === 'string') : [] }
  } catch { return {} }
}
function visibleElement(selector: string): HTMLElement | undefined {
  return Array.from(document.querySelectorAll<HTMLElement>(selector)).find(el => el.getClientRects().length > 0 && getComputedStyle(el).visibility !== 'hidden')
}

/** Page-scoped catalogue and spotlight. Business actions always remain explicit user actions. */
export function PageGuide({ app, page, user }: Props) {
  const key = guideStorageKey(app, user.userId, user.merchantId, page)
  const [history, setHistory] = useState<History>(() => readHistory(key))
  const [open, setOpen] = useState(false)
  const [message, setMessage] = useState('')
  const trigger = useRef<HTMLButtonElement>(null)
  const panel = useRef<HTMLDivElement>(null)
  const active = useRef<Driver>()
  const stopWatching = useRef<() => void>(() => {})
  const closeActive = useRef<() => void>(() => {})
  const tours = availableGuideTours(page, user)
  function save(next: History) {
    setHistory(next)
    try { localStorage.setItem(key, JSON.stringify(next)) } catch { /* Works without browser storage. */ }
  }
  useEffect(() => () => closeActive.current(), [])
  useEffect(() => {
    if (!open) return
    const previous = document.activeElement as HTMLElement | null
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const shell = document.querySelector<HTMLElement>('.su-shell')
    const wasInert = shell?.inert ?? false
    if (shell) shell.inert = true
    const otherDialogs = Array.from(document.querySelectorAll<HTMLElement>(dialogSelector))
      .filter(dialog => dialog !== panel.current && !dialog.contains(panel.current))
      .map(dialog => ({ dialog, inert: dialog.inert }))
    otherDialogs.forEach(({ dialog }) => { dialog.inert = true })
    panel.current?.querySelector<HTMLButtonElement>('button')?.focus()
    function keyboard(event: KeyboardEvent) {
      if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); setOpen(false) }
      if (event.key !== 'Tab') return
      const buttons = Array.from(panel.current?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)') ?? [])
      const first = buttons[0], last = buttons[buttons.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus() }
    }
    document.addEventListener('keydown', keyboard, true)
    return () => {
      document.removeEventListener('keydown', keyboard, true)
      document.body.style.overflow = previousOverflow
      if (shell) shell.inert = wasInert
      otherDialogs.forEach(({ dialog, inert }) => { dialog.inert = inert })
      if (previous?.isConnected && !active.current?.isActive()) previous.focus()
    }
  }, [open])
  if (!tours.length) return null

  function start(tour: GuideTour) {
    const underlyingDialog = Array.from(document.querySelectorAll<HTMLElement>(dialogSelector))
      .find(dialog => dialog !== panel.current && !dialog.contains(panel.current) && dialog.getClientRects().length > 0)
    if (underlyingDialog && !underlyingDialog.contains(visibleElement(tour.steps[0].target) ?? null)) {
      setMessage('Chiudi la guida e il modulo aperto prima di iniziare un altro percorso. I dati che hai inserito restano nel modulo.')
      return
    }
    const unavailable = () => !!tour.unavailableWhen && !!visibleElement(tour.unavailableWhen)
    if (unavailable() || tour.requiresTarget && !visibleElement(tour.requiresTarget)) {
      setMessage(tour.unavailableMessage ?? 'Non ci sono elementi disponibili per questo percorso. Attendi il caricamento o riprova dopo averli aggiunti.')
      return
    }
    if (!visibleElement(tour.steps[0].target)) {
      setMessage('Attendi il caricamento della pagina oppure chiudi il modulo aperto, poi riprova.')
      return
    }
    closeActive.current()
    setOpen(false)
    setMessage('')
    let closed = false
    let backwardIndex: number | undefined
    const steps: DriveStep[] = tour.steps.map(step => ({
      element: () => visibleElement(step.target)!,
      skipMissingElement: step.optional ?? false,
      disableActiveInteraction: step.readOnly ?? false,
      popover: { title: step.title, description: step.text, side: 'bottom', align: 'start' },
    }))
    function teardown() {
      if (closed) return
      closed = true
      stopWatching.current()
      if (active.current === instance) {
        active.current = undefined
        closeActive.current = () => {}
      }
      const fallback = trigger.current
      // Restore focus after Driver has restored its original trigger, including early animation exits.
      requestAnimationFrame(() => {
        if (!fallback?.isConnected) return
        if (panel.current) { panel.current.querySelector<HTMLElement>('button')?.focus(); return }
        const form = Array.from(document.querySelectorAll<HTMLElement>(dialogSelector))
          .find(dialog => dialog.getClientRects().length > 0)
        if (form) form.querySelector<HTMLElement>('button:not(:disabled), input:not(:disabled)')?.focus()
        else fallback.focus()
      })
    }
    function stop() {
      // Driver 1.10 can omit onDestroyed during its first animation. Never rely on that hook for cleanup.
      teardown()
      instance.destroy()
    }
    function fail(text: string) {
      stop()
      setMessage(text)
      setOpen(true)
    }
    function move(index: number) {
      while (index < tour.steps.length && tour.steps[index].optional && !visibleElement(tour.steps[index].target)) index++
      if (index >= tour.steps.length) { finish(); return }
      if (!visibleElement(tour.steps[index].target)) {
        fail('Questo passaggio non è disponibile nello stato attuale. Apri la sezione indicata e riprova.')
        return
      }
      instance.moveTo(index)
    }
    function finish() {
      save({ ...history, dismissed: true, viewed: [...new Set([...(history.viewed ?? []), tour.id])] })
      stop()
      setMessage('Percorso visto. Le operazioni si completano con i normali comandi della pagina.')
      setOpen(true)
    }
    const instance = driver({
      steps, animate: !window.matchMedia('(prefers-reduced-motion: reduce)').matches,
      overlayOpacity: 0.55, overlayClickBehavior: 'none', stagePadding: 6, popoverOffset: 20,
      popoverClass: 'su-guide-popover', showProgress: true, progressText: '{{current}} di {{total}}',
      nextBtnText: 'Avanti', prevBtnText: 'Indietro', doneBtnText: 'Ho capito', closeBtnLabel: 'Chiudi guida',
      allowKeyboardControl: false,
      onCloseClick: stop,
      onNextClick: (_element, _step, opts) => {
        const step = tour.steps[opts.index ?? 0]
        if (step.nextWhen && !visibleElement(step.nextWhen)) return
        move((opts.index ?? 0) + 1)
      },
      onPrevClick: (_element, _step, opts) => {
        let index = (opts.index ?? 0) - 1
        while (index >= 0 && !visibleElement(tour.steps[index].target)) index--
        if (index >= 0) { backwardIndex = index; instance.moveTo(index) }
      },
      onDoneClick: finish,
      onHighlightStarted: element => {
        if (element && window.matchMedia('(max-width: 767px)').matches) element.scrollIntoView({ block: 'center', behavior: 'instant' })
      },
      onPopoverRender: (popover, opts) => {
        stopWatching.current()
        const index = opts.index ?? 0
        const current = tour.steps[index]
        const backtracking = backwardIndex === index
        backwardIndex = undefined
        const back = document.createElement('button')
        back.type = 'button'; back.className = 'su-guide-overview'; back.textContent = 'Torna alla panoramica'
        back.dataset.activity = 'guide.overview'
        back.onclick = () => { stop(); setOpen(true) }
        popover.wrapper.append(back)
        popover.wrapper.setAttribute('aria-label', `${tour.title}: ${current.title}`)
        popover.wrapper.setAttribute('aria-live', 'polite')
        popover.closeButton.setAttribute('aria-label', 'Chiudi guida')
        popover.closeButton.dataset.activity = 'guide.close'
        popover.nextButton.dataset.activity = 'guide.next'
        popover.previousButton.dataset.activity = 'guide.previous'
        let queued = false
        let deadline: number | undefined
        const ready = () => !current.nextWhen || !!visibleElement(current.nextWhen)
        const check = () => {
          if (queued || closed) return
          queued = true
          requestAnimationFrame(() => {
            queued = false
            if (closed || !instance.isActive() || instance.getActiveIndex() !== index) return
            if (unavailable()) {
              fail(tour.unavailableMessage ?? 'Questo percorso non è disponibile nello stato attuale della pagina.')
            } else if (!visibleElement(current.target)) {
              fail('La sezione è stata chiusa. Puoi riaprire il percorso dalla panoramica.')
            } else if (current.nextWhen) {
              const canContinue = ready()
              popover.nextButton.disabled = !canContinue
              popover.nextButton.textContent = canContinue ? 'Avanti' : 'Usa il controllo indicato'
              if (canContinue && !backtracking) move(index + 1)
            }
          })
        }
        if (current.nextWhen) {
          popover.nextButton.disabled = !ready()
          popover.nextButton.textContent = ready() ? 'Avanti' : 'Usa il controllo indicato'
        }
        const onClick = (event: MouseEvent) => {
          if (current.nextWhen && deadline === undefined && visibleElement(current.target)?.contains(event.target as Node)) {
            // Start the timeout only after the user acts, not while they are reading the instructions.
            deadline = window.setTimeout(() => {
              if (!closed && instance.getActiveIndex() === index && !ready())
                fail('Il controllo atteso non è disponibile. Verifica i dati o gli avvisi della pagina e riprova.')
            }, 8000)
          }
          check()
        }
        const isGuideNode = (node: Node) => node instanceof Element && !!node.closest('.driver-popover, .driver-overlay')
        const observer = new MutationObserver(records => {
          if (records.some(record => !isGuideNode(record.target) && (record.type !== 'childList' ||
            [...record.addedNodes, ...record.removedNodes].some(node => !isGuideNode(node))))) check()
        })
        // Portals (such as BottomSheet) can render outside main-content. Ignore mutations of the guide itself.
        observer.observe(document.body, { subtree: true, childList: true, attributes: true })
        document.addEventListener('click', onClick)
        const escape = (event: KeyboardEvent) => {
          if (event.key === 'Escape') { event.preventDefault(); event.stopImmediatePropagation(); stop() }
        }
        document.addEventListener('keydown', escape, true)
        stopWatching.current = () => {
          observer.disconnect()
          document.removeEventListener('click', onClick)
          document.removeEventListener('keydown', escape, true)
          window.clearTimeout(deadline)
        }
        check()
        popover.nextButton.disabled ? popover.closeButton.focus() : popover.nextButton.focus()
      },
      onDestroyed: teardown,
    })
    active.current = instance
    closeActive.current = stop
    instance.drive()
  }

  return <div className="su-guide-bar" data-guide-ui>
    {!history.dismissed && <div className="su-guide-invitation">
      <span>Prima volta qui? Scopri come usare questa pagina.</span>
      <button data-activity="guide.dismiss" type="button" aria-label="Nascondi suggerimento guida" onClick={() => save({ ...history, dismissed: true })}>×</button>
    </div>}
    <button data-activity="guide.open" ref={trigger} type="button" className="su-guide-trigger" aria-haspopup="dialog" aria-expanded={open}
      onClick={() => { save({ ...history, dismissed: true }); setMessage(''); setOpen(true) }}><span aria-hidden="true">?</span> Guida</button>
    {open && createPortal(<div data-activity="guide.backdrop" className="su-guide-backdrop" onClick={event => { if (event.target === event.currentTarget) setOpen(false) }}>
      <div ref={panel} className="su-guide-catalogue" role="dialog" aria-modal="true" aria-labelledby="su-guide-title" aria-describedby="su-guide-description">
        <div className="su-guide-heading"><div><p>GUIDA ALLA PAGINA</p><h2 id="su-guide-title">{page.title}</h2></div>
          <button data-activity="guide.catalogue.close" type="button" className="su-guide-close" aria-label="Chiudi guida" onClick={() => setOpen(false)}>×</button></div>
        <p id="su-guide-description">Scegli cosa vuoi fare. Puoi fermarti in qualsiasi momento.</p>
        {message && <p role="status" className="su-guide-message">{message}</p>}
        <div className="su-guide-paths">{tours.map(tour => <button data-activity="guide.start" type="button" key={tour.id} onClick={() => start(tour)}>
          <span className="su-guide-path-title">{tour.title}<span aria-hidden="true"> →</span></span><span>{tour.description}</span>
          <small>{tour.steps.length} passaggi{history.viewed?.includes(tour.id) ? ' · Già visto' : ''}</small>
        </button>)}</div>
      </div>
    </div>, document.body)}
  </div>
}

/** Compact help inside an existing wizard, without adding another navigation flow. */
export function GuideHint({ children }: { children: ReactNode }) {
  return <details className="su-guide-hint"><summary data-activity="guide.hint">Aiuto per questo passaggio</summary><p>{children}</p></details>
}
