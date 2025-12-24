# Phase 2: Calendario Disponibilità con Slot Orari

## Requisiti

### Tipi di Prenotazione Merchant
1. **TimeSlot** (Slot Orari) - Es. Pizzeria/Ristorante
   - Orari fissi e predefiniti (es. 12:00, 12:30, 13:00, 19:00, 19:30, etc.)
   - Cliente sceglie uno slot disponibile
   - Durata predefinita dal servizio

2. **TimeRange** (Inizio/Fine Attività) - Es. Libreria/Biblioteca
   - Cliente può entrare in qualsiasi momento nell'orario di apertura
   - Può specificare orario di inizio (opzionale)
   - Può specificare orario di fine (opzionale)
   - Basato su orari di apertura/chiusura

3. **DayOnly** (Solo Giorno) - Es. Discoteca/Eventi
   - Prenotazione valida per l'intera giornata/serata
   - Nessun orario specifico richiesto
   - Solo selezione data

### Regole di Business
- Il cliente è guidato dal tipo di prenotazione configurato dal merchant
- L'approvazione del merchant è obbligatoria (già implementato con BookingStatus)
- Le disponibilità possono essere ricorrenti (pattern settimanale) o one-time

## Design Database

### Nuovo Enum: BookingMode
```csharp
public enum BookingMode
{
    TimeSlot = 1,      // Slot orari fissi
    TimeRange = 2,     // Inizio/fine flessibili
    DayOnly = 3        // Solo giorno
}
```

### Modifica Tabella Services
```sql
ALTER TABLE Services ADD COLUMN BookingMode INT NOT NULL DEFAULT 1;
ALTER TABLE Services ADD COLUMN SlotDurationMinutes INT NULL;
ALTER TABLE Services ADD COLUMN MaxCapacityPerSlot INT NULL;
```

### Nuova Tabella: Availabilities
```sql
CREATE TABLE Availabilities (
    Id SERIAL PRIMARY KEY,
    ServiceId INT NOT NULL,
    DayOfWeek INT NULL,              -- 0-6 (Sunday-Saturday) per ricorrenti, NULL per one-time
    SpecificDate DATE NULL,          -- Data specifica per one-time, NULL per ricorrenti
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsRecurring BOOLEAN NOT NULL,
    MaxCapacity INT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL,

    CONSTRAINT FK_Availability_Service FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Availabilities_ServiceId ON Availabilities(ServiceId);
CREATE INDEX IX_Availabilities_DayOfWeek ON Availabilities(DayOfWeek);
CREATE INDEX IX_Availabilities_SpecificDate ON Availabilities(SpecificDate);
```

### Nuova Tabella: AvailabilitySlots (per BookingMode.TimeSlot)
```sql
CREATE TABLE AvailabilitySlots (
    Id SERIAL PRIMARY KEY,
    AvailabilityId INT NOT NULL,
    SlotTime TIME NOT NULL,
    MaxCapacity INT NULL,

    CONSTRAINT FK_AvailabilitySlot_Availability FOREIGN KEY (AvailabilityId) REFERENCES Availabilities(Id) ON DELETE CASCADE
);

CREATE INDEX IX_AvailabilitySlots_AvailabilityId ON AvailabilitySlots(AvailabilityId);
```

## API Endpoints

### AvailabilityController

**Merchant Endpoints:**
- `GET /api/availability/my-availabilities` - Lista availability del merchant
- `POST /api/availability` - Crea nuova availability
- `PUT /api/availability/{id}` - Modifica availability
- `DELETE /api/availability/{id}` - Elimina availability
- `POST /api/availability/{id}/slots` - Aggiungi slot a availability (per TimeSlot mode)

**Public Endpoints:**
- `GET /api/availability/service/{serviceId}` - Ottieni availability per un servizio
- `GET /api/availability/available-slots?serviceId={id}&date={date}` - Ottieni slot disponibili per una data

### Modifiche BookingsController
- Validazione che la prenotazione rispetti le availability configurate
- Verifica capacità massima per slot

## Frontend Changes

### Merchant App - Nuove Pagine/Componenti

**1. Service Setup (modifica existing)**
- Selezione BookingMode (radio buttons)
- Configurazione SlotDurationMinutes (se TimeSlot)
- Configurazione MaxCapacityPerSlot

**2. Availability Management (nuova pagina)**
- Calendar view delle availability
- Form per creare availability ricorrenti (weekly pattern)
- Form per creare availability one-time (eventi speciali)
- Per TimeSlot mode: configurazione slot orari specifici
- List view delle availability esistenti con edit/delete

### Consumer App - Modifiche

**1. Service Details (modifica existing)**
- Mostra tipo di prenotazione (TimeSlot/TimeRange/DayOnly)
- Descrizione del processo di prenotazione

**2. Booking Form (modifica existing - dinamico)**

**Se BookingMode = TimeSlot:**
- Selezione data (calendar)
- Dropdown con slot disponibili per quella data
- Numero persone (limitato da MaxCapacity)

**Se BookingMode = TimeRange:**
- Selezione data (calendar)
- Mostra orari di apertura/chiusura
- Campo orario inizio (opzionale/suggerito)
- Campo orario fine (opzionale)

**Se BookingMode = DayOnly:**
- Solo selezione data (calendar)
- Numero persone

## Implementation Order

1. **Backend - Database Layer**
   - Aggiungere BookingMode enum
   - Creare migration per Availabilities e AvailabilitySlots
   - Aggiornare Service model

2. **Backend - Business Layer**
   - Creare AvailabilityService
   - Estendere BookingService con validazione availability
   - Implementare algoritmo di ricerca slot disponibili

3. **Backend - API Layer**
   - Creare AvailabilityController
   - Aggiornare ServicesController per BookingMode

4. **Frontend - Merchant App**
   - Pagina Availability Management
   - Modificare Service form per BookingMode

5. **Frontend - Consumer App**
   - Componente booking dinamico
   - Integrazione calendar con availability

6. **Testing & Refinement**

## Technical Considerations

### Calcolo Slot Disponibili (TimeSlot Mode)
```
Per una data specifica:
1. Trova availability ricorrente per il giorno della settimana
2. Trova availability one-time per la data specifica
3. Recupera tutti gli slot configurati
4. Per ogni slot, calcola capacità residua:
   - Conta bookings confermati per quella data/ora
   - Sottrai da MaxCapacity
5. Ritorna solo slot con capacità > 0
```

### Validazione Booking
```
Prima di creare booking:
1. Verifica che esista availability per la data
2. Se TimeSlot: verifica che lo slot esista e abbia capacità
3. Se TimeRange: verifica che l'orario sia dentro apertura/chiusura
4. Se DayOnly: verifica che il servizio sia disponibile quel giorno
5. Aggiungi booking con status Pending
6. Merchant approva/rifiuta
```

### Recurring vs One-Time Priority
- Se esistono sia ricorrenti che one-time per la stessa data, one-time ha priorità
- Permette override per eccezioni (chiusure, eventi speciali)
