import type { FocusEvent, MouseEvent } from 'react'

export function localDateStr(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
function showPicker(input: HTMLInputElement) {
  try { input.showPicker?.() } catch {
    // Alcuni browser richiedono un gesto utente anche quando il campo riceve focus.
  }
}
export const nativeDateInputProps = {
  onFocus: (event: FocusEvent<HTMLInputElement>) => showPicker(event.currentTarget),
  onClick: (event: MouseEvent<HTMLInputElement>) => showPicker(event.currentTarget),
}
