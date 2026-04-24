export function formatBrowserDate(date: Date): string {
  const locale = typeof navigator === 'undefined'
    ? undefined
    : (navigator.languages?.[0] ?? navigator.language)

  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}
