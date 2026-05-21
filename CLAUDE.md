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

## Struttura

- `AppointmentScheduler.API` — controller
- `AppointmentScheduler.Core` — service (logica applicativa)
- `AppointmentScheduler.Data` — `ApplicationDbContext`, migration EF
- `AppointmentScheduler.Shared` — `Models`, `DTOs`, `Enums`, `Helpers`
