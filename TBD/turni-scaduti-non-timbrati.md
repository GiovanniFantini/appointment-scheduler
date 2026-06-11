# Turni scaduti non timbrati → anomalia — COMPLETATO

## Problema (sintomo osservato)

Nella pagina Timbratura, l'11 giugno comparivano "Turno Test" (10/06 17:36–17:40) e
"Turno Test 2" (10/06 19:00–20:00) con etichetta **"In attesa dell'orario di inizio."**
Erano turni del **10**, mai timbrati, che l'11 risultavano ancora "in attesa".

## Causa radice

Due meccanismi scollegati, con due definizioni diverse di "turno chiuso":

1. La vista "oggi" (`GetTodayShiftContextsAsync`) trascinava i turni di ieri e li
   scartava **solo se avevano un clock-out**. Un turno di ieri mai iniziato restava
   visibile e finiva nel ramo `else` dello stato → "In attesa dell'orario di inizio."
   perché l'unica distinzione era `inWindow` true/false (non si guardava se la
   finestra era già **passata**).
2. Il motore anomalie missing-punch (`RunMissingPunchDetectionAsync`) sapeva già
   creare `MissingClockIn`/`MissingClockOut`, ma era invocato **solo manualmente**
   da un bottone manager. Nessun automatismo → il turno mancato non diventava
   anomalia da solo.

## Soluzione implementata

Il turno passato mai timbrato **sparisce** dalla lista turni e diventa un'anomalia
`MissingClockIn`. Rilevamento **lazy su entrambi i lati** (dipendente + manager).
Nessun background service, nessuna migration (concorrenza gestita via codice con
fallback).

## Lavoro svolto

- [x] Core di detection condiviso `DetectMissingPunchForParticipantsAsync`, usato sia
      da `RunMissingPunchDetectionAsync` (merchant) sia dal nuovo
      `RunMissingPunchDetectionForEmployeeAsync` (per-dipendente).
- [x] Idempotenza + fallback anti-concorrenza via codice: `existingSet`, ricontrollo
      sul DB prima del save, `try/catch DbUpdateException` con detach. Nessun vincolo
      DB. Limite noto: finestra teorica minima tra ricontrollo e save (doppione raro,
      non critico).
- [x] Trigger lazy lato dipendente in `GetTodayShiftsAsync`.
- [x] Trigger lazy lato manager nell'endpoint `GetAnomalies`.
- [x] `GetTodayShiftContextsAsync`: scarta i turni di ieri mai iniziati con finestra
      chiusa (`IsClockWindowClosedAsync` = oltre `endWall + LateClockOutTolerance`).
      Turno senza `EndTime` → finestra non determinabile → non scartato.
- [x] Nuovo stato `IsExpired` + messaggio "Finestra di timbratura chiusa." per i
      turni di **oggi** mai timbrati oltre la fine finestra (prima mostravano
      "In attesa dell'orario di inizio.", fuorviante). DTO `ShiftClockStatusDto` +
      frontend (`TodayShiftsPanel`, tipi, CSS: pill/card "expired").
- [x] Frontend `TimbraturaPage`: `getTodayShifts` atteso prima di leggere le
      anomalie (evita la race delle chiamate parallele).
- [x] Test: 9 nuovi/aggiornati in `TimeClockServiceTests` (drop+anomalia, finestra
      ancora aperta, senza EndTime, per-employee, idempotenza, concorrenza,
      MissingClockOut, IsExpired). Suite completa: 444/444 verde.
- [x] Documentazione aggiornata: `docs/FLUSSI-UTENTE.md` (corner case timbratura +
      descrizione bottone "Rileva timbrature mancanti").

## Decisioni di prodotto chiuse durante l'analisi

- **Modello uscite intermedie (caffè)**: si usano le **Pause** esistenti
  (BreakStart/BreakEnd), non ri-entrate multiple. Nessun cambio di paradigma.
- **Asimmetria entrata/uscita**: confermata corretta by-design. Un turno aperto
  (entrata senza uscita) è chiudibile a ogni ora; un'entrata mai fatta su turno
  finito è un'anomalia, non una timbratura tardiva → niente finestra di recupero
  estesa (eviterebbe ambiguità tra turni consecutivi).
- **Limite storico sulla detection lazy**: nessuno (recupera tutto l'arretrato).
- **Multi-employee / multi-turno**: già corretti — anomalie per `EventParticipant`,
  una per dipendente per turno.

## Limite noto accettato

Concorrenza gestita solo via codice (niente unique index per scelta): resta una
finestra teorica minima tra il ricontrollo sul DB e il `SaveChanges` in cui due
richieste davvero simultanee potrebbero creare un doppione di anomalia. Impatto
basso (anomalia duplicata, non dato critico).

## File toccati

- `backend/AppointmentScheduler.Core/Services/TimeClockService.cs`
- `backend/AppointmentScheduler.Core/Services/ITimeClockService.cs`
- `backend/AppointmentScheduler.Shared/DTOs/TimeClockDto.cs`
- `backend/AppointmentScheduler.API/Controllers/EmployeeTimeClockManagementController.cs`
- `backend/AppointmentScheduler.API.Tests/TimeClockServiceTests.cs`
- `frontend/employee-app/src/pages/TimbraturaPage/TimbraturaPage.tsx`
- `frontend/employee-app/src/components/TodayShiftsPanel/TodayShiftsPanel.tsx`
- `frontend/employee-app/src/components/TodayShiftsPanel/TodayShiftsPanel.css`
- `frontend/employee-app/src/components/TimeClockWidget/TimeClockWidget.css`
- `frontend/employee-app/src/types/timbratura.ts`
- `docs/FLUSSI-UTENTE.md`
