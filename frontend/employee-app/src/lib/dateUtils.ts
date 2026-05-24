import type { FocusEvent, MouseEvent } from 'react'

/** Formatta una Date come "YYYY-MM-DD" nel fuso locale del browser (non UTC). */
export function localDateStr(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

/** Parsa "YYYY-MM-DD" come mezzanotte locale (non UTC midnight). */
export function parseDateOnly(str: string): Date {
  const [y, m, d] = str.split('-').map(Number)
  return new Date(y, m - 1, d)
}

function tryShowNativeDatePicker(target: EventTarget | null): void {
  if (!(target instanceof HTMLInputElement)) return

  const picker = (target as HTMLInputElement & { showPicker?: () => void }).showPicker
  if (typeof picker !== 'function') return

  try {
    picker.call(target)
  } catch {
    // Some browsers expose showPicker but still reject the call.
  }
}

export const nativeDateInputProps = {
  onFocus: (event: FocusEvent<HTMLInputElement>) => tryShowNativeDatePicker(event.currentTarget),
  onClick: (event: MouseEvent<HTMLInputElement>) => tryShowNativeDatePicker(event.currentTarget),
}

function browserLocale(): string | undefined {
  if (typeof navigator === 'undefined') return undefined
  return navigator.languages?.[0] ?? navigator.language
}

export function formatBrowserDate(date: Date): string {
  return new Intl.DateTimeFormat(browserLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

export function formatBrowserDateTime(date: Date): string {
  return new Intl.DateTimeFormat(browserLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}
