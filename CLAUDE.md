# AppointmentScheduler — Istruzioni per Claude

Gestionale aziendale B2B2E (eventi/turni, timbratura, magazzino multi-filiale).
Backend .NET 8 + EF Core + PostgreSQL (Npgsql). Frontend separato.

## Date e orari — regole obbligatorie

PostgreSQL via Npgsql mappa `DateTime` su `timestamp with time zone` e **accetta solo
`DateTimeKind.Utc`**. Scrivere un `DateTime` con `Kind=Unspecified` o `Local` provoca:

> `System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL
> type 'timestamp with time zone', only UTC is supported.`

Regole da seguire:

1. **Timestamp generati dal server** (`CreatedAt`, `UpdatedAt`, `OrderedAt`, `*At`):
   usare sempre `DateTime.UtcNow`, **mai** `DateTime.Now`.

2. **Timestamp che arrivano da una request del client** (`DateTime` / `DateTime?` nei
   `*Request` DTO): un valore deserializzato da JSON senza offset ha
   `Kind=Unspecified` e fa crashare il `SaveChanges`. Prima di assegnarlo a un'entità
   passarlo da `DateTimeUtc.Coerce(...)`
   (`AppointmentScheduler.Shared/Helpers/DateTimeUtc.cs`).

3. **Date/orari di dominio senza fuso** (data di un turno, ora di inizio, giorno di
   ferie): usare `DateOnly` / `TimeOnly`, non `DateTime`. EF Core li mappa su
   `date` / `time` e non hanno il problema del Kind. Questa è la convenzione già
   adottata in `Event`, `EmployeeRequest`, `TimeEntry.WorkDate`, ecc. — rispettarla
   per ogni nuovo campo data.

4. Gli orari di timbratura sono **wall-clock locali**, non UTC: vedi `TimeClockService`
   (`ToWallClock`, `NowWallClock`). Non convertirli a UTC.

Quando aggiungi un nuovo campo data: scegli `DateOnly`/`TimeOnly` se non serve il
fuso; se è un istante e arriva dal client, ricordati `DateTimeUtc.Coerce`.

## Frontend date input

- Ogni `input type="date"` deve aprire il picker nativo anche su focus/click usando l'helper condiviso `nativeDateInputProps` del rispettivo `src/lib/dateUtils.ts`.
- Nelle form evento, `EndDate` è opzionale salvo requisito esplicito di business: se manca, l'evento vale per il solo `StartDate`.
- Per eventi `Ferie`, `Malattia` e `Permessi` non mostrare Filiale/Reparto e richiedere sempre la selezione di un solo dipendente.

## Struttura

Backend (`backend/`):

- `AppointmentScheduler.API` — controller, middleware, authorization, converter JSON
- `AppointmentScheduler.Core` — service (logica applicativa), interfacce, opzioni
- `AppointmentScheduler.Data` — `ApplicationDbContext`, migration EF, `DbInitializer`
- `AppointmentScheduler.Shared` — `Models`, `DTOs`, `Enums`, `Helpers`
- `AppointmentScheduler.API.Tests` — xUnit (test di service e flussi)

Frontend (`frontend/`): quattro app Vite + React + TypeScript + Tailwind che
condividono `shared-ui` (alias `@scheduler/ui`):

- `admin-app` — back-office amministratore di piattaforma
- `merchant-app` — gestionale del merchant
- `employee-app` — app dipendente (turni, timbratura, magazzino, documenti)
- `shared-ui` — design system condiviso (`ui/`, `form/`, `shell/`, `wizard/`, `auth/`)

## Stile del codice e codice morto

Il codebase è tenuto deliberatamente pulito; quando lavori, mantieni questi invarianti:

- **Commenti**: spiegano *il perché* e i casi limite (turni notturni, concorrenza,
  wall-clock vs UTC), non *cosa* fa il codice. Non aggiungere commenti che ripetono
  l'istruzione successiva e non rimuovere quelli che documentano una scelta non ovvia.
  La lingua dei commenti è l'italiano.
- **Backend**: la build deve restare a **0 warning** (`dotnet build` in `backend/`).
  Niente codice commentato, `#region` o `Console.WriteLine` di debug.
- **Frontend**: i `tsconfig.json` hanno `noUnusedLocals` e `noUnusedParameters`
  attivi, quindi import/variabili/parametri inutilizzati fanno fallire `tsc`. Niente
  `console.log` di debug nel codice che va in produzione.
- **Dead code frontend a livello di modulo** (file/export mai importati): non lo
  rileva `tsc`. Usa `npx knip` dentro la singola app per trovarli, e verifica sempre
  con una ricerca prima di eliminare.
