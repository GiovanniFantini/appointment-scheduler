import type { FocusEvent, MouseEvent } from 'react'

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

export function formatBrowserDate(
  date: Date,
  options: Intl.DateTimeFormatOptions = { day: '2-digit', month: '2-digit', year: 'numeric' },
): string {
  return new Intl.DateTimeFormat(browserLocale(), options).format(date)
}
