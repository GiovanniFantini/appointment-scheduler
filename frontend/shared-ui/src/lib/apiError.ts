/**
 * Estrae un messaggio d'errore leggibile da una risposta API fallita.
 *
 * Gestisce i tre formati che il backend può restituire:
 *  1. Errore esplicito del dominio: `{ message: "..." }` (es. AuthController).
 *  2. Errore di validazione DataAnnotations (ProblemDetails di ASP.NET):
 *     `{ title, errors: { Campo: ["msg1", ...] } }`. Questo è il caso che
 *     prima veniva mascherato in "link scaduto": un input utente sbagliato
 *     (es. password troppo corta) viene rifiutato dal model binding PRIMA
 *     che il controller giri, quindi non ha un `message` nostro.
 *  3. Nessuno dei due → `fallback` generico (l'errore vero resta nei log
 *     del server, qui non vogliamo esporre dettagli all'utente).
 */
export function extractApiError(err: unknown, fallback: string): string {
  const data = (err as {
    response?: {
      data?: {
        message?: string
        errors?: Record<string, string[] | string>
      }
    }
  })?.response?.data

  if (!data) return fallback

  // 1. Messaggio esplicito del dominio.
  if (typeof data.message === 'string' && data.message.trim().length > 0) {
    return data.message
  }

  // 2. Errori di validazione: prendiamo il primo messaggio non vuoto.
  if (data.errors && typeof data.errors === 'object') {
    for (const value of Object.values(data.errors)) {
      const first = Array.isArray(value) ? value[0] : value
      if (typeof first === 'string' && first.trim().length > 0) {
        return first
      }
    }
  }

  // 3. Generico.
  return fallback
}
