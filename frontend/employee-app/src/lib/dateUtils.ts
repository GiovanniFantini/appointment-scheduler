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
