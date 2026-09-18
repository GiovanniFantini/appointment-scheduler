# Registro attività custom

## Installazione e consultazione

Applicare la migrazione `AddActivityEvents` prima di distribuire le app. Il normale
avvio con `RUN_MIGRATIONS=true` usa già il migratore del progetto. Il menu
**Registro attività** è presente in admin, merchant ed employee.

L'admin può consultare tutti gli eventi. Il merchant vede solo il tenant del JWT
verificato. L'employee vede solo le proprie richieste, interazioni ed eventi esterni
nel tenant attivo; non riceve i dettagli delle modifiche alle entità. La policy
abbonamenti esistente continua ad applicarsi. I parametri di ricerca non possono
ampliare il perimetro autorizzato.

## Copertura

| Origine | Copertura | Significato dell'esito |
| --- | --- | --- |
| Middleware API | Tutte le azioni controller e URL API non riconosciuti, letture, scritture, autenticazione, autorizzazione, validazione, throttling, eccezioni | `completed` significa risposta HTTP completata; non garantisce l'esito aziendale implicito in risposte volutamente generiche |
| DbContext | Inserimenti, modifiche, eliminazioni EF, salvataggi sincroni/asincroni, processi senza HTTP | `committed` esiste solo se viene confermata la transazione che contiene anche i dati |
| Browser | Router, click, change, submit, invalid, primo focus nei form, dialog condivisi, errori, cambio contesto, logout/pagehide | `observed` è una dichiarazione non affidabile del client |
| Email e storage | Avvio ed esito chiamate al provider, rilascio URL upload/download, verifica proprietà/esistenza e rimozione blob | `completed` è il ritorno del provider; `unknown` indica un'eccezione con effetto esterno potenzialmente avvenuto |

I flussi di account, aziende, abbonamenti, risorse, ruoli, filiali, mansioni,
pianificazione, richieste, timbrature, anomalie, magazzino, fornitori, ordini,
documenti, notifiche e report passano attraverso questi punti comuni.
L'azione server è il nome stabile `Controller.Action`; i cambiamenti indicano
anche tipo e ID dell'entità. Un'operazione può comprendere più richieste e più
modifiche: collegarle mediante `OperationId`, senza deduplicare eventi differenti.
Ogni richiesta conserva anche il proprio `RequestId`.

Gli elementi interattivi hanno `data-activity` statici nel sorgente. Non rinumerare
gli identificativi esistenti. `npm run test:activity` segnala nuovi controlli non
classificati; `node scripts/activity-controls.mjs --write` assegna gli identificativi
mancanti. Gli elementi generati fuori da React usano un percorso strutturale di
fallback. I componenti custom devono inoltrare `data-activity` al nodo DOM.

Il test della matrice API verifica la presenza di eventi per ogni azione controller;
l'endpoint di raccolta è escluso per evitare un evento ricorsivo per ogni batch.

## Persistenza e protezione dei dati

`ActivityEvents` contiene schema versionato, identità server, tenant, sorgente,
app dichiarata dal client, correlazioni, timestamp, esito e dettagli JSON.
I timestamp server sono UTC. Gli orari di dominio rimangono wall-clock o DateOnly/
TimeOnly; il registro non cambia le regole delle timbrature.

Le modifiche EF e il loro audit usano una transazione unica. Se esiste una
transazione del chiamante viene usato un savepoint. Un fallimento dell'audit annulla
il salvataggio; un rollback esterno annulla anche i relativi eventi. I rifiuti
preventivi delle quote lasciano il change tracker correggibile dal chiamante.

I dati stringa/binari/array sono oscurati per default; i cambiamenti conservano i
nomi dei campi. Sono ammessi scalari numerici, booleani, enum, identificativi e
date. Password, token, segreti e coordinate rimangono oscurati. Nessun body HTTP,
query string, URL firmato, contenuto email o documento entra nel registro.
Per aggiungere valori testuali leggibili serve una modifica esplicita alla policy.

Il browser mantiene al massimo 500 eventi in sessionStorage, senza token, con
scadenza di 24 ore; invia batch di 50 ogni 5 secondi e alla sospensione pagina.
I tentativi riusano EventId; il server applica un vincolo univoco. Il cambio di
credenziali elimina la coda del vecchio contesto e conta gli scarti. L'API limita
dimensione, cardinalità, formato degli eventi e frequenza di raccolta. Le chiamate
di raccolta non rientrano nel client Axios e non causano loop di errori/autenticazione.

## Gestione operativa

Gli eventi applicativi sono di sola aggiunta tramite DbContext. Il worker giornaliero
elimina esclusivamente interazioni browser più vecchie di
`Activity:TelemetryRetentionDays` (default 30, 0 disabilita), fino a 10.000 per ciclo,
registrando la manutenzione nella stessa transazione. I record server non hanno
scadenza automatica. Dimensionare il limite di manutenzione se l'ingestione cresce.

La consultazione usa cursori di 100 record e indici per tenant, utente, entità,
operazione e ricezione. L'esportazione JSON riguarda la pagina filtrata corrente
(massimo 100 record), con la stessa autorizzazione; genera `Activity.Export`.

I fallimenti di scrittura delle richieste e degli eventi esterni producono errori
strutturati tramite ILogger. Impostare un allarme operativo su tali errori e
monitorare dimensioni/indici della tabella. Non inviare di nuovo un'email solo
perché il suo audit finale è fallito: i decorator mantengono l'esito originale.

## Confini verificabili

- Nessun sistema browser può garantire il recupero dopo chiusura forzata, cancellazione
  dello storage o rete permanentemente assente. Gli scarti rilevabili producono
  `telemetry.dropped`; un form iniziato senza submit non prova un abbandono volontario.
- Rilascio URL download, trasferimento del file e presa visione sono eventi distinti.
  Il trasferimento diretto Azure Blob non viene osservato dal backend.
- L'audit EF descrive le entità presenti nel change tracker. Una cascata esclusivamente
  database è rappresentata dall'eliminazione della radice, senza diff separati dei
  figli non caricati. SQL diretto, accessi manuali al database, migrazioni e operazioni
  esterne al gestionale richiedono strumentazione dedicata.
- I risultati dei provider esterni non sono transazionali con PostgreSQL. Questo
  intervento registra tentativi/esiti, senza introdurre retry di side effect o
  modificare i flussi sincroni esistenti. Una coda outbox per l'esecuzione differita
  richiede una migrazione funzionale separata dei relativi flussi.
- L'immutabilità applicativa non protegge da un amministratore del database. Copie
  esterne con conservazione protetta e archiviazione/partizionamento sono decisioni
  infrastrutturali da dimensionare sui volumi reali.

## Verifica

- `dotnet build backend -c Release --warnaserror`
- `dotnet test backend -c Release`
- Per le prove relazionali impostare `SCHEDULER_TEST_POSTGRES` su PostgreSQL locale:
  vengono creati e rimossi esclusivamente database con nomi casuali di test.
- Da `frontend`: `npm run test:activity` e `npm run build`.
- Workflow `activity-validation.yml`: backend, PostgreSQL, tre build frontend e
  controllo degli identificativi su PR e push ai branch `codex/**`.
