# Pacchetti aziendali

## Gestione amministratore

- **Pacchetti**: crea e modifica un numero libero di pacchetti. Ogni pacchetto ha un nome univoco, una descrizione, le funzioni incluse e i limiti quantitativi. Per una configurazione personalizzata si crea un pacchetto dedicato.
- **Merchant → Pacchetto**: mostra pacchetto corrente, utilizzo e scadenza. Consente assegnazione definitiva, prova, proroga e conversione definitiva.
- Le modifiche a un pacchetto si applicano a tutte le aziende assegnate, comprese quelle in prova. L'interfaccia richiede conferma indicando quante aziende sono coinvolte.
- Nessun acquisto o pagamento online. La registrazione non assegna un pacchetto: deve farlo l'admin. L'approvazione dell'azienda e l'assegnazione sono due requisiti distinti.

## Funzioni e permessi

Il pacchetto appartiene al merchant ed è condiviso da tutte le sue filiali. Il dipendente usa il pacchetto dell'azienda selezionata.

L'accesso effettivo è l'intersezione fra funzioni incluse e permessi del ruolo. Il titolare, nell'app merchant, dispone del livello Manager delle funzioni incluse. Un ruolo non può aggirare un'esclusione del pacchetto, anche se ne conserva la configurazione per una futura riattivazione.

Il backend rilegge pacchetto e ruolo per ogni richiesta autenticata nel contesto aziendale e sostituisce i claim di funzione obsoleti. Le API dei moduli sono protette da `RequiresPlanFeature`. Non è necessario rinnovare il JWT per applicare le revoche. I token precedenti alla selezione dell'azienda non accedono alle API operative.

Le app verificano `/api/subscription` prima di montare le pagine, ogni 30 secondi, al ritorno in primo piano e dopo un 403. Alla variazione dei diritti rimontano le pagine per eliminare dati e form dei moduli revocati. Errori di verifica impediscono l'accesso finché il server non torna raggiungibile.

Calendario e gestione Richieste sono separati. Ferie, malattia e permessi inseriti come eventi restano nel Calendario. Se Richieste è escluso, il calendario non carica le richieste e non mostra la relativa legenda. Senza Mansioni, il wizard del turno passa direttamente da periodo a partecipanti. Sedi/reparti e anagrafiche necessari ad assegnare i turni restano consultabili come riferimenti; la gestione dei moduli esclusi rimane bloccata.

Senza Mansioni, le API di creazione di eventi e risorse rifiutano nuove assegnazioni di mansioni. Le API di aggiornamento conservano quelle storiche lato server, ignorando modifiche manuali ai relativi campi. Nei turni rimangono solo le assegnazioni dei partecipanti mantenuti nel turno.

Le notifiche di moduli non accessibili vengono filtrate anche dal riepilogo. Le notifiche storiche dei giustificativi sono distinte dalle altre richieste usando il titolo deterministico emesso da `TimeClockService`.

## Limiti

Un valore nullo significa illimitato. I limiti sono indipendenti dalle funzioni abilitate.

| Limite | Conteggio |
| --- | --- |
| Dipendenti attivi | Membership attive con anagrafica attiva, inclusi interni, esterni e titolare se membro |
| Filiali attive | Filiali attive, inclusa la sede principale; minimo configurabile 1 |
| Spazio documenti | Somma dei byte di tutte le versioni completate dei documenti non eliminati; admin in MiB |

Il controllo intercetta il salvataggio di membership, riattivazioni, filiali e versioni documentali. Su PostgreSQL usa un lock transazionale per merchant per serializzare i consumi concorrenti. Un aggiornamento che non aumenta l'utilizzo è consentito anche oltre quota; creazioni e riattivazioni che aumentano l'utilizzo oltre quota vengono respinte con `409 / SUBSCRIPTION_LIMIT`.

Una riduzione di pacchetto non elimina dati. Le risorse oltre quota restano utilizzabili nelle funzioni ancora incluse. Rientrare nella quota o aumentarne il limite sblocca le nuove aggiunte.

Il limite documentale è **logico**, non una quota fisica dell'account Azure. L'upload continua a usare SAS; la dimensione reale viene verificata alla finalizzazione. Un file che supera la quota non viene pubblicato, viene marcato come fallito e ne viene richiesta la cancellazione dal blob. I caricamenti abbandonati e la retention fisica dei documenti eliminati richiedono le consuete politiche di pulizia dello storage.

## Prove gratuite

- Durata proposta: 14 giorni, modificabile dall'admin da 1 a 3650 giorni.
- Un'assegnazione in prova fa partire la durata dal momento dell'assegnazione.
- La proroga aggiunge giorni alla scadenza futura, oppure parte da adesso se la prova è già scaduta.
- La conversione definitiva conserva il pacchetto corrente ed elimina la scadenza.
- Alla scadenza l'operatività viene bloccata sia nelle API sia nelle app, con la pagina “Prova terminata”. Dati conservati. Restano possibili logout e cambio azienda.
- Le scadenze sono istanti UTC generati dal server; vengono visualizzate nell'orario locale del browser. Nessun job schedulato è necessario per far scadere una prova.

## Rilascio

La migration `20260914104528_AddSubscriptionPlans` crea il catalogo e i riferimenti sul merchant, poi crea **Completo** (dieci funzioni, limiti nulli) e lo assegna ai merchant già presenti. I merchant registrati successivamente non ricevono automaticamente questo pacchetto.

Distribuire insieme API e le tre app, applicando prima la migration. Le nuove API dipendono dalle colonne aggiunte. Ripetere la migrazione su una copia dei dati di staging prima del go-live. I test automatici usano InMemory per i casi di quota, HTTP TestServer per autorizzazione e scadenze e PostgreSQL locale per migration e lock concorrenti.

La pagina merchant **Report** era già un'anteprima con dati dimostrativi. L'inclusione del modulo nel pacchetto controlla l'accesso alla pagina; non aggiunge reportistica reale.

## Verifica

- `dotnet build` e `dotnet test` da `backend/` con runtime ASP.NET Core 8.
- `npm run build` da `frontend/` per tutte le app.
- `SubscriptionTests` copre intersezione pacchetto/ruolo, cambi con token invariato, azienda senza membership, scadenza esatta, assegnazioni/proroghe/conversioni, quote e riattivazioni, conteggio delle versioni e recupero dello spazio logico.
- Verifica browser su fixture: creazione/modifica/assegnazione admin, schermata mobile, esclusione delle richieste API ai moduli non inclusi, wizard calendario, scadenza e ripristino nella sessione corrente, blocco URL diretti merchant ed employee.

### Revisione del 14 settembre 2026

Correzioni aggiuntive verificate con test di regressione:

- Quote calcolate sullo stato persistito invece delle `OriginalValues` potenzialmente obsolete dopo il salvataggio di un altro contesto.
- Controllo anche per `SaveChanges` sincrono, documento e versione creati nello stesso salvataggio e ripristino di documenti eliminati logicamente.
- Stato frontend del pacchetto associato all'ID del merchant: una risposta precedente non può abilitare il contesto di un'altra azienda. Il form admin viene ricreato al cambio merchant.
- Chiusura delle modifiche indirette alle Mansioni attraverso Calendario e Risorse.

Esito locale: 477 test backend superati; build backend senza warning; build delle tre app riuscite; verifica browser con API simulate riuscita. Resta il warning Vite preesistente sulla dimensione del bundle employee.

### Collaudo PostgreSQL locale

Eseguito su PostgreSQL 16.15, con binari portabili scaricati da [EDB](https://www.enterprisedb.com/download-postgresql-binaries), istanza temporanea su `127.0.0.1:55432` e dati sintetici. Nessun collegamento al database dell'applicazione.

`SubscriptionPostgresTests` verifica:

- Applicazione di tutte le migration fino alla precedente, inserimento di un merchant preesistente e applicazione della nuova migration.
- Pacchetto Completo con dieci funzioni, nessun limite e nessuna scadenza assegnato al merchant preesistente; nuovi merchant senza assegnazione automatica.
- Riesecuzione della migration e dello script SQL idempotente senza duplicare il pacchetto.
- Due connessioni concorrenti per ciascuna quota: verifica esplicita in `pg_locks` che entrambe attendano il lock, poi un solo salvataggio accettato e l'altro respinto.
- Rollback dell'anagrafica dipendente e del documento/versione quando il salvataggio supera la quota.
- Eliminazione logica e ripristino documenti, downgrade oltre quota con conservazione dei dati e modifiche ordinarie consentite.
- Rollback e riapplicazione della migration conservando i dati operativi. Il rollback elimina la configurazione dei pacchetti; la riapplicazione assegna nuovamente Completo ai merchant presenti.

Esito: **478 test superati**, incluso il test PostgreSQL, build backend con **0 warning**. Il database casuale di collaudo viene eliminato automaticamente; l'istanza temporanea è stata arrestata dopo i test.

Per ripetere il test con un PostgreSQL locale e un utente autorizzato a creare database, da `backend/`:

```powershell
$env:SCHEDULER_TEST_POSTGRES = 'Host=127.0.0.1;Port=55432;Username=scheduler_audit;Database=postgres'
dotnet test --filter FullyQualifiedName~SubscriptionPostgresTests
```

La variabile è opzionale: senza di essa il test PostgreSQL risulta saltato. Il test accetta soltanto un host locale, crea un database dal nome casuale `scheduler_test_*` ed elimina soltanto quel database. Non usa la connection string dell'applicazione. Resta da verificare il rilascio su una copia dei dati reali di staging e sulla versione PostgreSQL usata in produzione.

### Regressione API del 18 settembre 2026

`ApiEndpointRegressionTests` scopre le azioni MVC registrate e verifica **148 azioni API con 1.174 richieste HTTP**, sia con EF InMemory sia con PostgreSQL 16.15 locale. Il test PostgreSQL usa la stessa variabile opzionale `SCHEDULER_TEST_POSTGRES`, un database casuale `scheduler_api_test_*` e tutte le migration reali; il database viene eliminato alla fine.

- Ogni metodo viene controllato con utente anonimo, ruolo consentito, admin, ruolo errato dove applicabile, assenza di azienda, pacchetto vuoto e prova scaduta.
- Un contratto esplicito associa i controller ai moduli e rileva la rimozione accidentale di `RequiresPlanFeature`.
- Le verifiche di accesso interrompono l'esecuzione dopo middleware e policy tramite un filtro presente soltanto nei test. Non costituiscono test funzionali delle singole mutazioni.
- Separatamente vengono eseguite le azioni GET con i servizi effettivi e le scritture con JSON malformato o identificativi inesistenti. Vengono controllati gli errori inattesi; le fixture sintetiche non coprono tutte le combinazioni dei dati aziendali.
- Il flusso admin esegue realmente creazione, modifica, assegnazione e proroga del pacchetto, verificando persistenza, funzioni non valide, limiti negativi e nomi duplicati.

È stata individuata e corretta una regressione su `/api/Version`: l'endpoint pubblico veniva bloccato dal pacchetto scaduto o dall'assenza di un'azienda nel token. Il middleware ora lascia le rotte pubbliche e quelle inesistenti al normale routing/autorizzazione.

Esito complessivo: **480 test superati, nessuno saltato, build con 0 warning**. Le 477 verifiche precedenti di controller, servizi e flussi restano parte della suite, insieme al test migration/concorrenza e alle due matrici API. Il collaudo non comprende chiamate reali ai servizi Azure di invio email e Blob Storage, né ogni flusso di scrittura con dati validi per ogni endpoint.
