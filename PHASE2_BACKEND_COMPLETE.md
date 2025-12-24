# Phase 2 Backend - COMPLETATO ✅

## Summary

Il backend per la fase 2 è stato completamente implementato con sistema di calendario disponibilità e gestione slot orari.

## Modifiche Implementate

### 1. Database Schema

#### Nuovo Enum: `BookingMode`
- `TimeSlot` (1) - Slot orari fissi (es. pizzeria)
- `TimeRange` (2) - Orario flessibile inizio/fine (es. biblioteca)
- `DayOnly` (3) - Solo giorno senza orario (es. discoteca)

#### Modifiche Tabella `Services`
```sql
- BookingMode INT NOT NULL DEFAULT 1
- SlotDurationMinutes INT NULL
- MaxCapacityPerSlot INT NULL
```

#### Nuova Tabella: `Availabilities`
- Gestisce disponibilità ricorrenti (settimanali) e one-time (date specifiche)
- Supporta finestre orarie (StartTime/EndTime)
- Capacità massima configurabile
- Relazione 1:N con Service

#### Nuova Tabella: `AvailabilitySlots`
- Slot orari specifici per BookingMode.TimeSlot
- Capacità per singolo slot
- Relazione 1:N con Availability

### 2. Entities & Models

**File creati/modificati:**
- `/backend/AppointmentScheduler.Shared/Enums/BookingMode.cs` (NEW)
- `/backend/AppointmentScheduler.Shared/Models/Availability.cs` (NEW)
- `/backend/AppointmentScheduler.Shared/Models/AvailabilitySlot.cs` (NEW)
- `/backend/AppointmentScheduler.Shared/Models/Service.cs` (UPDATED)

### 3. DTOs

**File creati/modificati:**
- `/backend/AppointmentScheduler.Shared/DTOs/AvailabilityDto.cs` (NEW)
  - `AvailabilityDto`
  - `AvailabilitySlotDto`
  - `CreateAvailabilityRequest`
  - `UpdateAvailabilityRequest`
  - `CreateAvailabilitySlotRequest`
  - `AvailableSlotDto`
- `/backend/AppointmentScheduler.Shared/DTOs/ServiceDto.cs` (UPDATED)
  - Aggiunti campi BookingMode, SlotDurationMinutes, MaxCapacityPerSlot

### 4. Services (Business Logic)

**AvailabilityService** (NEW)
- `GetMerchantAvailabilitiesAsync()` - Lista availability merchant
- `GetServiceAvailabilitiesAsync()` - Availability per servizio specifico
- `CreateAvailabilityAsync()` - Crea availability con validazione
- `UpdateAvailabilityAsync()` - Modifica availability
- `DeleteAvailabilityAsync()` - Elimina availability
- `AddSlotsToAvailabilityAsync()` - Aggiunge slot (solo TimeSlot mode)
- `GetAvailableSlotsAsync()` - Calcola slot disponibili con capacità residua

**BookingService** (UPDATED)
- Aggiunta validazione availability in `CreateBookingAsync()`
- `ValidateBookingAvailabilityAsync()` - Valida prenotazione contro availability
- `ValidateTimeSlotBookingAsync()` - Validazione specifica per TimeSlot mode
- `ValidateTimeRangeBooking()` - Validazione specifica per TimeRange mode

**ServiceManagementService** (UPDATED)
- Mapping BookingMode in Create/Update/MapToDto

### 5. API Endpoints

**AvailabilityController** (NEW) - `/api/availability`
- `GET /my-availabilities` [MerchantOnly] - Le mie availability
- `GET /service/{serviceId}` [Public] - Availability di un servizio
- `GET /available-slots?serviceId={id}&date={date}` [Public] - Slot disponibili per data
- `GET /{id}` [MerchantOnly] - Dettaglio availability
- `POST /` [MerchantOnly] - Crea availability
- `PUT /{id}` [MerchantOnly] - Modifica availability
- `DELETE /{id}` [MerchantOnly] - Elimina availability
- `POST /{id}/slots` [MerchantOnly] - Aggiungi slot

### 6. Database Configuration

**ApplicationDbContext** (UPDATED)
- Aggiunti `DbSet<Availability>` e `DbSet<AvailabilitySlot>`
- Configurate relazioni e indici per performance

**Migration**: `20251224130059_phase-two.cs`

### 7. Dependency Injection

**Program.cs** (UPDATED)
- Registrato `IAvailabilityService` / `AvailabilityService`

## Logica di Business Implementata

### Priorità Availability
- **Specific Date > Recurring**: Se esiste availability per data specifica, ha priorità su quella ricorrente
- Permette override per eccezioni (chiusure, eventi speciali)

### Validazione Booking per BookingMode

#### TimeSlot Mode
1. Verifica esistenza availability per la data
2. Verifica esistenza slot specifico richiesto
3. Calcola capacità residua:
   - Conta bookings (Pending + Confirmed) per lo stesso slot
   - Sottrae da MaxCapacity (slot > availability > service)
4. Rifiuta se capacità insufficiente

#### TimeRange Mode
1. Verifica esistenza availability per la data
2. Verifica che StartTime >= Availability.StartTime
3. Verifica che EndTime <= Availability.EndTime
4. Verifica che StartTime < EndTime

#### DayOnly Mode
1. Verifica solo esistenza availability per la data
2. Nessuna validazione orari

### Gestione Capacità (Cascading)
```
MaxCapacity priorità:
1. AvailabilitySlot.MaxCapacity (più specifico)
2. Availability.MaxCapacity
3. Service.MaxCapacityPerSlot
4. Unlimited (int.MaxValue)
```

## File Modificati/Creati

### NEW Files (13)
1. `/backend/AppointmentScheduler.Shared/Enums/BookingMode.cs`
2. `/backend/AppointmentScheduler.Shared/Models/Availability.cs`
3. `/backend/AppointmentScheduler.Shared/Models/AvailabilitySlot.cs`
4. `/backend/AppointmentScheduler.Shared/DTOs/AvailabilityDto.cs`
5. `/backend/AppointmentScheduler.Core/Services/IAvailabilityService.cs`
6. `/backend/AppointmentScheduler.Core/Services/AvailabilityService.cs`
7. `/backend/AppointmentScheduler.API/Controllers/AvailabilityController.cs`
8. `/backend/AppointmentScheduler.Data/Migrations/20251224130059_phase-two.cs`
9. `/PHASE2_DESIGN.md`
10. `/PHASE2_BACKEND_COMPLETE.md` (questo file)

### UPDATED Files (5)
1. `/backend/AppointmentScheduler.Shared/Models/Service.cs`
2. `/backend/AppointmentScheduler.Shared/DTOs/ServiceDto.cs`
3. `/backend/AppointmentScheduler.Data/ApplicationDbContext.cs`
4. `/backend/AppointmentScheduler.Core/Services/ServiceManagementService.cs`
5. `/backend/AppointmentScheduler.Core/Services/BookingService.cs`
6. `/backend/AppointmentScheduler.API/Program.cs`

## Prossimi Passi (Frontend)

### Merchant App
1. **Services Page** - Aggiungere selezione BookingMode nel form servizio
2. **NEW: Availabilities Page** - Gestione availability calendario
   - Lista availability esistenti
   - Form creazione availability ricorrente/one-time
   - Gestione slot per TimeSlot mode
   - Calendar view

### Consumer App
1. **Service Details** - Mostrare BookingMode e descrizione
2. **Booking Form** - Dinamico basato su BookingMode:
   - TimeSlot: Mostra calendario + dropdown slot disponibili
   - TimeRange: Mostra orari apertura + input inizio/fine
   - DayOnly: Solo calendario

## Testing Scenarios

### TimeSlot (Pizzeria)
- Merchant crea availability ricorrente Sabato 19:00-23:00
- Merchant aggiunge slot: 19:00, 19:30, 20:00, 20:30, etc.
- Ogni slot ha MaxCapacity 20 persone
- Cliente prenota slot 20:00 per 4 persone
- Sistema conta bookings esistenti per 20:00 e verifica capacità

### TimeRange (Biblioteca)
- Merchant crea availability ricorrente Lunedì-Venerdì 9:00-18:00
- Cliente prenota dalle 10:00 alle 12:00
- Sistema valida che 10:00-12:00 sia dentro 9:00-18:00

### DayOnly (Discoteca)
- Merchant crea availability one-time per 31/12/2025
- Cliente prenota per 31/12/2025
- Sistema valida solo data, nessun orario richiesto

## Note Tecniche

- Tutte le date/ore sono gestite in UTC nel database
- TimeSpan usato per orari (indipendente da data)
- Indici database su ServiceId, DayOfWeek, SpecificDate per performance
- Validazione robusta con messaggi di errore localizzati (IT)
- Cascade delete: Service -> Availabilities -> AvailabilitySlots
