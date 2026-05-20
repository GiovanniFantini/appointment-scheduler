# TODO

## Timbratura — punti aperti

Cose predisposte o non implementate dalla feature Timbratura (2026-05-20).
Vedi anche il piano in `~/.claude/plans/manca-una-funzionalit-importante-nested-hare.md`.

### 1. Foto/selfie alla timbratura
- **Stato:** campo `RequirePhoto` presente su `BranchTimeClockSettings` e nei DTO,
  ma è solo un placeholder — nessuna cattura foto, nessuno storage.
- **Da decidere:** implementare (upload immagine via Azure Blob, già usato per i
  documenti HR) oppure rimuovere il toggle per non lasciare un'opzione inerte.

### 2. Arrotondamento ore per le buste paga
- **Stato:** campo `RoundingMinutes` presente (default `0`), letto dal backend
  ma **non applicato** al calcolo delle ore (`CalculateWorkedMinutes`).
- **Da decidere:** se serve per il payroll, implementare l'arrotondamento
  (es. al quarto d'ora) in `TimeClockService`.

### 3. Notifiche su anomalie e revisioni
- **Stato:** non cablate. Alla generazione di un'anomalia o all'approvazione/
  rifiuto del merchant non parte alcuna notifica.
- **Da decidere:** integrare `INotificationService` (già esistente) negli eventi
  di `TimeClockService` (detection anomalia, justify, approve/reject).

### 4. Job schedulato per le mancate timbrature
- **Stato:** la rilevazione è un endpoint manuale (`POST run-detection`),
  attivato dal pulsante "Rileva timbrature mancanti" nella merchant-app.
  Il progetto non ha infrastruttura di background job.
- **Da decidere:** valutare un `HostedService` che esegua la detection
  automaticamente a fine giornata, oppure mantenere il trigger manuale.

### 5. Vista admin-app
- **Stato:** non implementata. L'admin-app non ha una pagina Timbratura.
  Il piano (Fase 5) prevedeva una vista globale read-only.
- **Da decidere:** se serve una visibilità cross-merchant per l'admin.

### 6. App mobile nativa
- **Stato:** `TimeEntrySource.Mobile` predisposto nell'enum; le app sono solo
  web responsive. Nessuna app nativa in programma.
- **Nota:** predisposizione per il futuro — nessuna azione richiesta ora.
