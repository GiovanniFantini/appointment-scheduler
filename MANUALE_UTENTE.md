# Manuale Utente - Appointment Scheduler

## Panoramica

Piattaforma per la gestione di prenotazioni che connette clienti, attività commerciali e dipendenti.

**Ruoli:**
- **Cliente**: Prenota servizi
- **Merchant**: Gestisce attività, prenotazioni, dipendenti e turni
- **Dipendente**: Gestisce turni e timbrature
- **Admin**: Approva merchant e gestisce piattaforma

**Categorie Servizi:** Ristoranti, Sport, Wellness, Healthcare, Professional, Altri

---

## Clienti

### Registrazione e Login
1. `/register` → Inserisci email, password, nome/cognome → Crea Account
2. `/login` → Inserisci credenziali → Accedi

### Prenotare Servizi

**Cerca:**
- Naviga per categoria o usa ricerca

**Prenota:**
1. Seleziona servizio
2. Scegli data e orario (3 modalità disponibili):
   - **TimeSlot**: Orari fissi (es. 12:00, 12:30) - ristoranti, parrucchieri
   - **TimeRange**: Orario flessibile - noleggi, coworking
   - **DayOnly**: Solo data - eventi, abbonamenti
3. Inserisci numero persone, note, seleziona dipendente (opzionale)
4. Conferma prenotazione

### Gestire Prenotazioni

**Stati:**
- Pending: In attesa conferma merchant
- Confirmed: Confermata
- Cancelled: Cancellata
- Completed: Completata
- NoShow: Mancata presentazione

**Cancellare:** `/bookings` → Seleziona prenotazione → Cancella

---

## Merchant

### Registrazione e Approvazione
1. Registrati come Merchant con dati azienda (Business Name, P.IVA, indirizzo)
2. Attendi approvazione admin (24-48h)
3. Dopo approvazione, accedi alle funzionalità complete

### Configurazione Iniziale

**Profilo Attività:**
- Logo, foto, descrizione, categorie, social media

**Orari Apertura:**
- `/business-hours` → Imposta orari per ogni giorno della settimana

**Periodi Chiusura:**
- `/closure-periods` → Aggiungi ferie o chiusure straordinarie

### Gestione Servizi

**Creare Servizio:**
1. `/services` → Nuovo Servizio
2. Configura:
   - Nome, descrizione, categoria
   - Prezzo, durata
   - Modalità prenotazione (TimeSlot/TimeRange/DayOnly)
   - Capacità massima per slot
3. Salva

**Disponibilità:**
- `/availabilities` → Crea slot disponibili per servizi
- Assegna dipendenti specifici (opzionale)

### Gestione Prenotazioni

**Visualizza:** `/bookings` → Filtra per data, servizio, stato, dipendente

**Azioni:**
- **Conferma**: Approva prenotazione Pending
- **Rifiuta**: Rifiuta prenotazione
- **Completa**: Marca come completata
- **No-Show**: Segna mancata presentazione

### Gestione Dipendenti

**Aggiungere:**
1. `/employees` → Nuovo Dipendente
2. Inserisci: Nome, email, telefono, ruolo, badge code
3. Salva

**Assegnare a Servizi:**
- Seleziona dipendenti abilitati durante creazione/modifica servizio

### Gestione Turni

**Template Turni (consigliato):**
1. `/shift-templates` → Nuovo Template
2. Configura: Nome, orari, tipo, pause, giorni settimana
3. Usa template per generare turni automaticamente per periodo

**Turno Singolo:**
- `/shifts` → Nuovo Turno → Seleziona dipendente, data, orari

**Calendario:** Visualizza turni mensili con stato e dipendenti assegnati

### Validazione Timbrature

**Auto-Validazione (95% automatica):**
- Timbrature entro ±15 min dall'orario pianificato → Auto-approvate
- Ritardi >15 min, straordinari non pianificati → Richiedono revisione

**Approvazione Batch:**
1. `/timbrature-validation` → Visualizza riepilogo settimanale
2. "Approva Tutte Auto-Validate" → 1 click per approvare timbrature regolari

**Gestire Anomalie:**
- Visualizza timbrature con anomalie
- Vedi motivazione dipendente
- Approva o rifiuta

**Scambi Turno:**
- Approva richieste scambio tra dipendenti

---

## Dipendenti

### Accesso
- Registrati tramite invito email dal datore di lavoro
- Login con credenziali personali

### Dashboard
- Turno corrente
- Ore lavorate oggi/settimana/mese
- Alert benessere (>50h/settimana)

### Timbrature

**Check-In:**
1. Apri app → Visualizzi turno odierno
2. "ENTRA" → Registra orario, GPS (se abilitata), avvia timer

**Pause:**
- "INIZIA PAUSA" → Timer lavoro si ferma
- "FINE PAUSA" → Timer riprende

**Check-Out:**
- "ESCI" → Registra orario, calcola ore totali
- Visualizza riepilogo turno

**Gestione Anomalie:**

*Ritardo (>15 min):*
- Sistema chiede motivazione (Traffico, Permesso, Recupero ore, Altro)
- Inserisci motivazione → Auto-validato

*Straordinario:*
- Sistema chiede classificazione (Paid, Banca Ore, Recupero, Volontario)
- Seleziona categoria → Richiede approvazione manager

**Correzioni:**
- Dimenticato timbratura? → `/my-shifts` → Richiedi Correzione
- Entro 24h → Auto-approvata
- Oltre 24h → Richiede approvazione manager

### Scambi Turno

**Richiedere:**
1. `/shift-swaps` → Nuova Richiesta
2. Seleziona tuo turno, collega, turno collega, motivazione
3. Invia → Collega accetta → Manager approva → Turni scambiati

**Ricevute:**
- Visualizza richieste ricevute
- Accetta o rifiuta

### Best Practices

✅ **Fare:**
- Timbra sempre entrata/uscita
- Registra pause >15 min
- Inserisci motivazione se in ritardo
- Richiedi correzioni entro 24h

❌ **Evitare:**
- Far timbrare altri al posto tuo
- Dimenticare sistematicamente
- Ignorare alert benessere

---

## FAQ

**Q: Password dimenticata?**
A: Login → "Password Dimenticata?" → Segui istruzioni email

**Q: App funziona su smartphone?**
A: Sì, responsive su tutti i dispositivi

**Q: Modificare prenotazione?**
A: Cancella e riprenota, o contatta merchant

**Q: Tempo approvazione merchant?**
A: 24-48h lavorative

**Q: Dipendente dimentica timbratura?**
A: Richiedi correzione entro 24h (auto-approvata)

**Q: Validare manualmente tutte le timbrature?**
A: No, 95% auto-validate, approvazione batch settimanale

---

## Supporto

**Tecnico:** support@appointmentscheduler.com (Lun-Ven 9-18)
**Merchant:** merchant-support@appointmentscheduler.com
**Dipendenti:** Contatta manager o employee-support@appointmentscheduler.com

---

**Versione**: 1.0 - Gennaio 2026
