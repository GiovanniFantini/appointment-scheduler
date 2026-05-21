# Segnalazioni — bug e incongruenze

Questo documento raccoglie i **bug probabili** e le **incongruenze** emersi
durante la mappatura dei flussi utente delle tre applicazioni (vedi
[FLUSSI-UTENTE.md](FLUSSI-UTENTE.md)). Non è un audit di sicurezza né una review
esaustiva: sono le cose notate leggendo il codice per documentarne il
comportamento.

Ogni voce indica gravità, dove si trova, cosa succede e una possibile direzione
di intervento. Le gravità sono indicative:

| Gravità | Significato |
|---------|-------------|
| 🔴 Alta | Funzionalità che non fa quello che promette, o rischio concreto in produzione. |
| 🟡 Media | Comportamento incoerente o fragile, impatto limitato o circoscritto. |
| 🟢 Bassa | Refuso, codice morto, pulizia. |

> **Stato (aggiornato 2026-05-21):** risolte le segnalazioni 1, 2, 4, 5, 6, 7,
> 8, 9. La 3 (account Merchant sull'app Employee) è lasciata aperta in attesa
> di conferma sull'intento. Vedi le note "✅ Risolto" sotto ogni voce.

---

## Bug probabili

### 1. 🔴 L'opzione "Invia notifica" sul turno non produce alcuna notifica

**Dove**: modale turno (`EventModal.tsx`) → opzione *Invia notifica*; flusso
notifiche lato backend.

**Cosa succede**: la casella *"Invia notifica"* imposta il flag
`notificationEnabled`, che viene regolarmente salvato sull'evento. Esistono i
tipi `NotificationType.EventCreated`, `EventUpdated`, `EventDeleted`. Tuttavia
**nessun componente del backend produce queste notifiche**:
`NotificationService.CreateAsync` viene invocato unicamente da
`EmployeeRequestService` (esito delle richieste). Creare, modificare o eliminare
un turno con "Invia notifica" attiva non recapita nulla ai partecipanti.

**Impatto**: il Merchant crede di aver avvisato i dipendenti di un turno; non è
così. L'opzione è di fatto inerte.

**Direzione**: o si implementa la generazione delle notifiche di evento (al
salvataggio di un turno con `notificationEnabled` e partecipanti collegati a un
account), oppure si rimuove l'opzione dalla UI finché la feature non c'è.

**✅ Risolto**: implementata la generazione delle notifiche. `EventService` ora
inietta `INotificationService` e, quando `NotificationEnabled` è attivo,
recapita `EventCreated`/`EventUpdated`/`EventDeleted` a ogni partecipante
collegato a un account utente (`Employee.UserId != null`) in `CreateAsync`,
`UpdateAsync` e `DeleteAsync`. La clonazione non genera notifiche di proposito
(eviterebbe un flood su decine di turni).

---

### 2. 🔴 I conflitti dei turni non vengono mostrati su drag & drop e clonazione

**Dove**: `EventService.CreateAsync` / `UpdateAsync` / `CloneAsync` /
`CloneWeekAsync` (calcolano i `Warnings`); `CalendarioPage.tsx` (drag & drop),
`CopyWeekDialog.tsx`, `PianificazionePage.tsx` (clonazione).

**Cosa succede**: il backend calcola correttamente i conflitti (ferie
sovrapposte, doppio turno, mansione mancante, filiale non abilitata) e li
restituisce nel campo `warnings` della risposta. Il **modale turno** legge e
mostra questi warning. Ma le **altre vie di scrittura** non li leggono mai:

- il **drag & drop** di un turno sul calendario chiama l'aggiornamento e ignora
  `response.data.warnings`;
- il **ridimensionamento** di un turno fa lo stesso;
- la **copia settimana** e la **clonazione** ricevono i warning di ogni clone ma
  non li espongono.

**Impatto**: trascinando un turno sopra un giorno di ferie approvate di un
dipendente, oppure clonando una settimana su date con conflitti, l'utente **non
riceve alcun avviso**. La regola "procedi e segnala" vale solo dal modale.

**Direzione**: dopo le operazioni di drag/resize/clone, ispezionare i `warnings`
della risposta e mostrarli (toast, banner o un riepilogo per la copia massiva).

**✅ Risolto**: drag & drop e ridimensionamento su `CalendarioPage` ora leggono
`response.data.warnings` e mostrano un banner conflitti sopra il calendario.
`CopyWeekDialog` aggrega i warning di tutti i cloni e mostra un riepilogo
post-copia; `PianificazionePage` mostra lo stesso riepilogo dopo `clone-week`.

---

### 3. 🟡 Un account di tipo Merchant può autenticarsi sull'app Employee

**Dove**: `AuthService.LoginEmployeeAsync` e `SelectCompanyAsync`.

**Cosa succede**: `LoginEmployeeAsync` cerca l'utente con
`AccountType == Employee || AccountType == Merchant`. `SelectCompanyAsync`
applica lo stesso criterio. Di conseguenza un account creato come Merchant può
accedere anche dall'app Employee (l'endpoint `employee/login` lo accetta).

**Impatto**: può essere **intenzionale** — alla registrazione di un'azienda
viene creato anche un profilo `Employee` per il titolare, quindi il titolare
"è" un dipendente. Ma il comportamento non è documentato e crea una superficie
d'accesso incrociata fra le app: vale la pena decidere se è una feature voluta
(e allora documentarla) o un residuo da restringere.

**Direzione**: confermare l'intento. Se voluto, documentarlo; se no, restringere
`LoginEmployeeAsync` al solo `AccountType.Employee`.

**⏸ Non risolto**: lasciato aperto in attesa di conferma sull'intento — alla
registrazione di un'azienda viene effettivamente creato un profilo `Employee`
per il titolare, quindi il login incrociato potrebbe essere voluto. Va deciso
con il product owner prima di toccare `LoginEmployeeAsync`.

---

### 4. 🟡 Il flag "wizard filiali già visto" è salvato per dispositivo, non per account

**Dove**: `FilialiPage.tsx` — chiave `merchant.filialiWizardSeen` in
`localStorage`.

**Cosa succede**: il wizard "Configura le filiali" si apre automaticamente al
primo accesso di un'azienda mono-sede. Una volta chiuso, viene scritto il flag
`merchant.filialiWizardSeen` in `localStorage`. `localStorage` è **legato al
browser/dispositivo**, non all'account aziendale.

**Impatto**:
- la stessa azienda, da un altro browser o dispositivo, **rivede il wizard**;
- se due utenti diversi condividono lo stesso browser, il secondo **non vede mai
  il wizard**, anche se per la sua azienda sarebbe pertinente.

**Direzione**: legare lo stato "wizard completato" a un dato dell'azienda lato
server (es. un flag sul merchant), oppure derivarlo da una condizione reale
(l'azienda ha più di una filiale / ha completato l'onboarding).

**✅ Risolto** (con direzione rivista): non serve un flag server-side. Lo stato
"wizard completato" è già derivato da una condizione reale — il wizard si apre
solo se `branches.length <= 1`, e una volta configurata una seconda filiale non
si riapre da solo. Il flag serviva solo a non riproporlo dopo una chiusura
*senza configurare*: è stato spostato da `localStorage` a `sessionStorage`, così
è per-sessione e non per-dispositivo (niente eredità cross-account sullo stesso
browser, niente soppressione permanente su un altro device).

---

### 5. 🟡 `RunMissingPunchDetection` usa la mutazione di un set come condizione

**Dove**: `TimeClockService.RunMissingPunchDetectionAsync`.

**Cosa succede**: per evitare anomalie duplicate, il codice usa
`existingSet.Add((...)) ` **dentro la condizione `if`**: l'`Add` restituisce
`false` se la coppia è già presente. Funziona — deduplica nello stesso giro — ma
mescola un effetto collaterale (mutazione del set) con il test di guardia.

**Impatto**: nessun malfunzionamento osservabile oggi. È un punto **fragile**:
una rilettura o un refactoring distratti possono romperne la logica senza che
sia evidente.

**Direzione**: separare il controllo dall'inserimento — prima `Contains`, poi
`Add` come passo distinto — per rendere l'intento esplicito.

**✅ Risolto**: la guardia ora usa `!existingSet.Contains(key)` nell'`if`, con
l'`existingSet.Add(key)` come istruzione separata nel corpo. Comportamento
identico, intento esplicito.

---

## Incongruenze

### 6. 🟡 Lunghezza minima della password incoerente tra le schermate

**Dove**: registrazione Merchant (`RegisterPage.tsx`, `minLength=6`);
registrazione Employee (`RegisterPage.tsx`, `minLength=8`); reset password
(`ResetPasswordPage.tsx`, `minLength=8` + controllo a video); backend
`PasswordResetService` (`newPassword.Length < 8`).

**Cosa succede**: la registrazione di un'azienda accetta una password di **6
caratteri**. Tutto il resto del sistema — registrazione dipendente e reset
password (frontend e backend) — richiede **8 caratteri**. Inoltre i DTO di
registrazione (`RegisterMerchantRequest`, `EmployeeRegisterRequest`) **non hanno
alcuna validazione di lunghezza lato backend**: il limite minimo esiste solo
come attributo `minLength` nei form.

**Impatto**: un titolare può registrarsi con una password di 6-7 caratteri che
non rispetta il criterio applicato altrove. La validazione di registrazione
dipende interamente dal browser: una chiamata diretta all'API la aggira del
tutto.

**Direzione**: uniformare il minimo (8 caratteri) su tutte le schermate e,
soprattutto, **validare la password lato backend** nei flussi di registrazione,
non solo nel reset.

**✅ Risolto**: il form di registrazione Merchant usa ora `minLength=8` come gli
altri. Lato backend `AuthService` espone la costante `MinPasswordLength = 8` e
valida la password in `RegisterMerchantAsync` e `RegisterEmployeeAsync`
(lancia `ArgumentException`, mappata a 400 dall'`AuthController`). Anche
`PasswordResetService` usa la stessa costante al posto del `< 8` hardcoded. Una
chiamata diretta all'API non può più aggirare il limite.

---

### 7. 🟢 Il tipo di richiesta "Cambio turno" non è creabile da nessuna interfaccia

**Dove**: enum `EmployeeRequestType.CambioTurno`; `CreateRequestModal.tsx`.

**Cosa succede**: `CambioTurno` esiste nell'enum dei tipi di richiesta ed è
gestito in colori e label del calendario e della pagina Richieste. Ma il modale
di creazione richiesta (`CreateRequestModal`) offre **solo Ferie, Permesso,
Malattia**. Nessun dipendente può creare una richiesta di tipo "Cambio turno".

**Impatto**: codice e gestione visuale per un tipo che non può esistere nei
dati creati dall'app. O è una feature dimenticata a metà, o è codice morto.

**Direzione**: decidere se la feature va completata (aggiungere il tipo al
modale, con i campi che gli servono) o rimossa (enum e gestione visuale).

**✅ Risolto** (rimosso): un vero "cambio turno" richiederebbe campi dedicati
(turno sorgente, destinazione, controparte) — è un progetto a sé, non un fix.
Rimosso il codice morto: il membro `CambioTurno = 2` dall'enum
`EmployeeRequestType` (i valori 3/4 restano espliciti per non spostarsi) e la
gestione visuale (colori, label, classe CSS `.cambioturno`) dalle pagine
Richieste e Calendario. Nessuna UI poteva creare quel tipo, quindi nessun dato
reale ne dipende.

---

### 8. 🟢 Commento errato sull'enum `AccountType` nell'AppLayout Admin

**Dove**: `admin-app/src/components/AppLayout.tsx` — commento sull'interfaccia
`User`.

**Cosa succede**: il commento dichiara `1 = admin, 2 = consumer, 3 = merchant,
4 = employee`. L'enum reale del sistema è `Admin = 1, Merchant = 2,
Employee = 3` (non esiste un `AccountType` "consumer").

**Impatto**: nessun malfunzionamento — il campo `accountType` qui non viene
usato per logica. È però un commento fuorviante che può indurre in errore chi
legge il codice.

**Direzione**: correggere il commento per allinearlo all'enum reale.

**✅ Risolto**: il commento ora recita `AccountType enum: 1 = Admin,
2 = Merchant, 3 = Employee`.

---

## Punto di attenzione (non un bug)

### 9. 🟡 Gli orari dei turni e della timbratura sono "wall-clock" senza fuso

**Dove**: `TimeClockService` (`NowWallClock => DateTime.Now`, `ToWallClock` con
`DateTimeKind.Unspecified`); coerente con il resto del codebase, che tratta
`DateOnly`/`TimeOnly` come orari locali del merchant.

**Cosa succede**: orari dei turni, deviazioni, tolleranze e anomalie sono
calcolati come **orario "da parete" senza fuso orario**. È una scelta
**deliberata e documentata** nel codice, e coerente ovunque.

**Perché è un punto di attenzione**: il calcolo corretto dipende dal fatto che
il **server di produzione sia configurato sul fuso orario giusto**. Un deploy in
un fuso diverso (o un cambio di regione del servizio) sfasa silenziosamente
tolleranze di entrata/uscita e rilevazione delle anomalie, senza errori
evidenti.

**Direzione**: non è da "correggere" — ma andrebbe **documentato come vincolo di
deploy** (il fuso del server è un requisito), e idealmente reso esplicito (fuso
del merchant memorizzato e applicato, anziché ereditato dall'ambiente).

**✅ Risolto** (documentato): aggiunta la sezione "Vincoli di Deploy" in
[PRODUCTION_ARCHITECTURE.md](../PRODUCTION_ARCHITECTURE.md), che indica il fuso
orario del server come requisito (`TZ=Europe/Rome` su Azure App Service) e ne
spiega la conseguenza operativa. Rendere il fuso del merchant esplicito nei dati
resta un miglioramento futuro, fuori scope di questo intervento.

---

## Riepilogo

| # | Gravità | Sintesi | Stato |
|---|---------|---------|-------|
| 1 | 🔴 Alta | "Invia notifica" sul turno non genera notifiche. | ✅ Risolto |
| 2 | 🔴 Alta | Conflitti non mostrati su drag & drop e clonazione. | ✅ Risolto |
| 3 | 🟡 Media | Account Merchant può loggarsi sull'app Employee. | ⏸ Da confermare |
| 4 | 🟡 Media | Flag "wizard filiali visto" salvato per dispositivo, non per account. | ✅ Risolto |
| 5 | 🟡 Media | `RunMissingPunchDetection`: mutazione del set usata come guardia. | ✅ Risolto |
| 6 | 🟡 Media | Lunghezza minima password incoerente; nessuna validazione backend in registrazione. | ✅ Risolto |
| 7 | 🟢 Bassa | Tipo richiesta "Cambio turno" non creabile da alcuna UI. | ✅ Risolto (rimosso) |
| 8 | 🟢 Bassa | Commento errato sull'enum `AccountType` (Admin AppLayout). | ✅ Risolto |
| 9 | 🟡 Media | Orari wall-clock senza fuso: vincolo di deploy da documentare. | ✅ Risolto (documentato) |

> Le segnalazioni 1, 2 e 6 erano le più concrete: incidevano su funzionalità che
> l'utente crede attive (notifiche, avvisi di conflitto) o sulla robustezza di
> un controllo di sicurezza (password). La 3 resta aperta: è una decisione di
> prodotto, non un fix tecnico.
