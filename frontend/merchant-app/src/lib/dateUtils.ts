export function parseDateOnly(str: string): Date {
  const [y, m, d] = str.split('-').map(Number)
  return new Date(y, m - 1, d)
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
