# Collaudo del registro attività — 18 settembre 2026

Commit applicativo verificato: `2db2d25`.

## Ambiente e metodo

Collaudo attraverso Chromium e le interfacce reali delle tre app, con login tramite
form. Backend .NET 8, database PostgreSQL separato e Azurite locale per il trasferimento
effettivo dei documenti. Nessuna risposta API simulata. Account, azienda, dipendenti
e documenti sono sintetici; il collaudo non riguarda un ambiente di produzione.

Il dipendente parte con Calendario, Richieste e Documenti in sola lettura. Il merchant
modifica il ruolo assegnato abilitando le funzionalità con livello Manager. Il
dipendente effettua nuovamente il login e usa le nuove autorizzazioni. Si tratta di
una modifica dei permessi del ruolo assegnato, non di un cambio del suo identificativo.

## Operazioni e riscontri nell'admin

| Operazione eseguita nell'interfaccia | Azione server | Riscontro persistito |
| --- | --- | --- |
| Dipendente: invio richiesta ferie | `EmployeeRequests.Create` | 1 richiesta |
| Dipendente: notifiche segnate come lette | `Notifications.MarkAllRead` | 1 notifica |
| Merchant: aggiornamento permessi del ruolo | `MerchantRoles.Update` | 20 record: eliminazione e ricreazione delle 10 autorizzazioni |
| Merchant: creazione ruolo di prova | `MerchantRoles.Create` | 1 ruolo e 10 autorizzazioni |
| Dipendente: creazione filiale | `EmployeeBranches.Create` | 1 filiale |
| Dipendente: modifica nome filiale | `EmployeeBranches.Update` | 1 modifica |
| Dipendente: creazione reparto | `EmployeeBranches.CreateDepartment` | 1 reparto |
| Dipendente: creazione di due risorse con ruolo, filiale e reparto | `EmployeeResources.Create` | 2 dipendenti e 2 appartenenze |
| Dipendente: creazione turno con due partecipanti | `Events.Create` | 1 turno e 2 partecipanti |
| Dipendente: caricamento documento TXT destinato a un'altra risorsa | `EmployeeDocuments.CreateDocumentForEmployee`, `EmployeeDocuments.FinalizeUpload` | Documento e versione creati e finalizzati |

Il documento è stato trasferito realmente allo storage locale. La finalizzazione
ha verificato esistenza e proprietà del blob. Nell'admin sono presenti anche gli
eventi esterni `document.upload-url`, `document.exists` e `document.properties`.
Come descritto in `activity-audit.md`, il trasferimento diretto al blob non è un evento
osservato dal backend.

## Verifiche della console admin

- Login admin reale, apertura Registro attività e filtri per app, utente, categoria,
  azione e tipo oggetto. Verificati 12 gruppi di modifiche dalla tabella e dalle
  risposte effettive della console.
- Per le modifiche verificate: autore corretto (dipendente o merchant), azienda
  corretta, applicazione corretta ed esito `committed` / “Salvato”.
- Apertura dei dettagli e verifica dei diff. I testi dei dati di dominio sono
  oscurati secondo la politica documentata; il registro non è una copia dei documenti.
- Esportazione JSON della pagina filtrata effettuata dall'interfaccia.
- Correlazione del turno: la stessa operazione mostra richiesta HTTP 201, turno
  salvato e due partecipanti salvati, per un totale di quattro eventi server.
- Verificate nell'admin anche navigazioni (`page.view`), click (`ui.click`), invii
  form (`form.submit`), richiesta di creazione turno ed esito del provider documentale.
- Nelle nove sequenze completate: nessun errore JavaScript e nessuna risposta API
  HTTP 4xx/5xx. Nessun errore applicativo rilevato nei log del backend di collaudo.

Le prime esecuzioni dell'automazione sono state adattate al wizard iniziale delle
filiali e ai selettori dei controlli; le sequenze riportate sono quelle completate.
Il collaudo non dimostra la copertura di ogni possibile combinazione di permessi,
interruzione di rete, browser o operazione del gestionale: questi scenari restano
distinti dai flussi concreti verificati qui.

Le evidenze locali (screenshot, risposte del registro e riepiloghi di rete) sono in
`output/activity-e2e/`. Non vengono versionati gli stati autenticati del browser,
le credenziali di collaudo o i file dello storage locale.
