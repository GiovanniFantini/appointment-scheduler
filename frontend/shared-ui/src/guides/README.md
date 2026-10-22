# Guide contestuali

Merchant ed employee passano ad `AppShell.pageGuide` una guida della pagina corrente.
Le pagine account condividono `accountPageGuides`; gli altri contenuti sono nei file
`*Page.guide.ts` accanto alla pagina. Admin non espone le guide.

## Aggiungere o aggiornare un percorso

1. Aggiungere un attributo semantico stabile, per esempio `data-guide="requests.new"`.
2. Definire un percorso di 2-5 passaggi con titolo e una breve frase per passaggio.
3. Impostare `feature` e `minLevel` quando il percorso richiede permessi specifici.
4. Per aprire un modulo o una scheda, usare `nextWhen` con il selettore che compare
   dopo il clic dell'utente. Non invocare `.click()` dalla guida.
5. Usare `optional` solo per elementi che possono legittimamente mancare, come
   una segnalazione o un ordine da ricevere. Un passaggio obbligatorio mancante
   interrompe il percorso con un messaggio; non viene presentato come eseguito.
6. Usare `readOnly` per spiegare un comando che salva, invia, approva o registra
   un'operazione. L'utente può eseguirlo dopo aver chiuso la guida.
7. Usare `requiresTarget` se serve un record già presente prima di iniziare.
   Con `unavailableWhen` e `unavailableMessage` spiegare prerequisiti mancanti,
   per esempio nessuna filiale assegnata. L'attesa dopo un'azione scade dopo
   otto secondi e riporta alla panoramica senza segnare il percorso come visto.

Il primo target deve essere sempre disponibile sulla pagina. I percorsi non
navigano tra pagine e non alterano dati o valori dei moduli. Nei wizard esistenti
usare `GuideHint` con un aiuto diverso per ogni passaggio.

`version` va incrementata quando cambia il significato della guida. Lo storico
locale separa app, utente, azienda e versione. Memorizza solo suggerimenti chiusi
e percorsi visti, non il completamento delle operazioni aziendali. Il blocco del
localStorage non impedisce l'uso delle guide.

## Verifica

Da `frontend`: `npm run test:guides`, poi build delle app interessate.
Nel browser verificare almeno un profilo base e uno con privilegi, il passaggio
in un modulo reale, Escape, il ritorno alla panoramica, cambio pagina e mobile.
I test verificano permessi, isolamento dello storico, contratto degli ancoraggi
e interazioni DOM reali con Driver.js, comprese chiusura rapida e Indietro.
