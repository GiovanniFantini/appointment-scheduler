# Appointment Scheduler

Piattaforma di prenotazione multi-verticale (B2C e B2B) che centralizza l'esperienza di prenotazione per utenti finali e gestione business per merchant.

**🚀 Stato Produzione**: Applicazione deployata su Azure con 5 App Services + PostgreSQL Database

## Documentazione

- **[SETUP.md](SETUP.md)** - Setup completo ambiente di sviluppo (INIZIA QUI)
- **[PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md)** - Coding standards e best practices
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Soluzioni ai problemi comuni
- **[VERSIONING.md](VERSIONING.md)** - Sistema di versioning Git-based
- **[PRODUCTION_ARCHITECTURE.md](PRODUCTION_ARCHITECTURE.md)** - Architettura Azure e deployment
- **[backend/CONFIGURATION.md](backend/CONFIGURATION.md)** - Gestione configurazioni e secrets

## Inizio Rapido

```bash
# 1. Database con Docker
docker run --name appointment-db -e POSTGRES_PASSWORD=dev123 -p 5432:5432 -d postgres

# 2. Backend
cd backend/AppointmentScheduler.API
cp appsettings.Local.json.example appsettings.Local.json
dotnet ef database update
dotnet run

# 3. Frontend Consumer (altra finestra)
cd frontend/consumer-app
npm install && npm run dev

# 4. Frontend Merchant (altra finestra)
cd frontend/merchant-app
npm install && npm run dev

# 5. Frontend Admin (altra finestra)
cd frontend/admin-app
npm install && npm run dev

# 6. Frontend Employee (altra finestra)
cd frontend/employee-app
npm install && npm run dev
```

Vai su:
- http://localhost:5173 (Consumer)
- http://localhost:5174 (Merchant)
- http://localhost:5175 (Admin)
- http://localhost:5176 (Employee)

Per istruzioni dettagliate: [SETUP.md](SETUP.md)

## 🎯 Obiettivo del Progetto

Creare una Web App mobile-ready multi-direzionale che:
- **B2C**: Permette agli utenti di prenotare servizi (ristoranti, sport, wellness, ecc.)
- **B2B**: Fornisce ai merchant strumenti per gestire prenotazioni e business
- **B2B2E**: Gestione completa dipendenti con timbrature, turni e coordinamento
- **Multi-verticale**: Gestisce diversi settori in un unico ecosistema
- **Cloud-Native**: Deployato su Azure con CI/CD automatico

## 🏗️ Architettura

### Stack Tecnologico

**Backend:**
- ASP.NET Core 8 Web API
- Entity Framework Core
- PostgreSQL (o SQL Server)
- JWT Authentication

**Frontend:**
- React 18 + TypeScript
- Vite
- Tailwind CSS
- React Router
- Axios

### Struttura del Progetto

```
appointment-scheduler/
├── backend/
│   ├── AppointmentScheduler.API/         # Web API
│   ├── AppointmentScheduler.Core/        # Business Logic
│   ├── AppointmentScheduler.Data/        # EF Core + Database
│   └── AppointmentScheduler.Shared/      # Models/DTOs/Enums
└── frontend/
    ├── consumer-app/                      # App utenti (porta 5173)
    ├── merchant-app/                      # Dashboard merchant (porta 5174)
    ├── admin-app/                         # Admin panel (porta 5175)
    └── employee-app/                      # App dipendenti (porta 5176)
```

## 👥 Ruoli Utente

1. **User (Consumer)**: Può solo prenotare servizi
2. **Merchant (Business)**: Gestisce le proprie attività e prenotazioni
3. **Employee (Dipendente)**: Gestisce turni, timbrature e colleghi
4. **Admin**: Gestisce permessi e approva i merchant

## 📊 Database Schema

### Tabelle Principali

**Users**
- Id, Email, PasswordHash, FirstName, LastName, PhoneNumber
- Role (User/Merchant/Admin)
- IsActive, CreatedAt, UpdatedAt

**Merchants**
- Id, UserId, BusinessName, Description
- Address, City, PostalCode, Country, VatNumber
- IsApproved (approvato dall'admin), CreatedAt, ApprovedAt

**Services**
- Id, MerchantId, Name, Description
- ServiceType (Restaurant/Sport/Wellness/Healthcare/Professional/Other)
- Price, DurationMinutes, IsActive
- Configuration (JSON per specificità verticali)

**Bookings**
- Id, UserId, ServiceId
- BookingDate, StartTime, EndTime
- Status (Pending/Confirmed/Cancelled/Completed/NoShow)
- NumberOfPeople, Notes
- CreatedAt, ConfirmedAt, CancelledAt

**Employees**
- Id, UserId, FirstName, LastName, Email, PhoneNumber
- HireDate, IsActive, CreatedAt

**Shifts**
- Id, EmployeeId, MerchantId, Date
- StartTime, EndTime, BreakMinutes
- Status (Scheduled/InProgress/Completed/Cancelled)
- CheckInTime, CheckOutTime (timbrature effettive)

**ShiftTemplates**
- Id, MerchantId, Name, Description
- DayOfWeek, StartTime, EndTime, BreakMinutes

**ShiftSwapRequests**
- Id, RequestingEmployeeId, TargetEmployeeId, ShiftId
- Status (Pending/Approved/Rejected)
- RequestDate, ResponseDate

**BusinessHours**
- Id, MerchantId, DayOfWeek
- OpenTime, CloseTime, IsOpen

**ClosurePeriods**
- Id, MerchantId, StartDate, EndDate
- Reason, CreatedAt

**EmployeeWorkingHoursLimits**
- Id, EmployeeId, MerchantId
- MaxHoursPerDay, MaxHoursPerWeek, MaxHoursPerMonth

## 🚀 Sviluppo

### Prerequisiti

- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Node.js 18+ ([Download](https://nodejs.org/))
- PostgreSQL 16+ ([Download](https://www.postgresql.org/download/)) o Docker
- Git

### Principi di Sviluppo

Questo progetto segue questi principi:
- **User-Friendly**: Interfacce intuitive, messaggi chiari
- **Dependency Injection**: Tutti i servizi usano DI
- **Testabile**: Facile da testare in locale
- **Documentato**: Commenti XML/JSDoc su tutti i metodi public
- **Sicuro**: Secrets mai committati, password hashate con BCrypt

Vedi [PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md) per dettagli completi.

### Setup Completo

Vedi [SETUP.md](SETUP.md) per istruzioni passo-passo.

**In breve:**
1. Clone repository
2. Avvia PostgreSQL (Docker o locale)
3. Crea `appsettings.Local.json` con tue credenziali
4. Esegui `dotnet ef database update`
5. Avvia backend con `dotnet run`
6. Avvia frontend con `npm install && npm run dev`

### Comandi Utili

**Backend:**
```bash
dotnet run                    # Avvia API
dotnet watch run              # Avvia con hot reload
dotnet ef database update     # Applica migrations
dotnet ef migrations add Name # Crea nuova migration
```

**Frontend:**
```bash
npm run dev      # Dev server
npm run build    # Build produzione
npm run preview  # Preview build
```

### API Documentation

Una volta avviato il backend, vai su:
- Swagger UI: https://localhost:5001/swagger
- Documentazione interattiva con possibilita' di testare endpoint

## 🔐 Autenticazione

Il sistema usa JWT Bearer Token:

1. **Login/Register** → Ricevi un token JWT
2. Invia il token nell'header: `Authorization: Bearer {token}`
3. Il token contiene UserId, Email e Role

### Policy di Autorizzazione

- **AdminOnly**: Solo Admin
- **MerchantOnly**: Merchant e Admin
- **EmployeeOnly**: Employee e Admin
- **UserOnly**: User, Merchant, Employee e Admin

## 📱 Interfacce

### Lato Consumer (B2C)

**Funzionalità:**
- Registrazione/Login
- Esplorazione servizi per categoria
- Prenotazione servizi
- Gestione delle proprie prenotazioni
- Visualizzazione storico

**Pagine:**
- `/login` - Login
- `/register` - Registrazione
- `/` - Home (categorie servizi)
- `/bookings` - Le mie prenotazioni

### Lato Merchant (B2B)

**Funzionalità:**
- Registrazione come merchant (richiede approvazione admin)
- Dashboard con statistiche
- Gestione servizi
- Gestione prenotazioni
- Calendario disponibilità
- Report e analytics

**Pagine:**
- `/login` - Login merchant
- `/register` - Registrazione merchant
- `/` - Dashboard

### Lato Admin

**Funzionalità:**
- Login riservato agli amministratori
- Dashboard di gestione piattaforma
- Approvazione/rifiuto merchant
- Gestione utenti (in sviluppo)
- Statistiche piattaforma (in sviluppo)

**Pagine:**
- `/login` - Login admin
- `/` - Dashboard admin
- `/merchants` - Gestione merchant

### Lato Employee (Dipendenti)

**Funzionalità:**
- Login riservato ai dipendenti
- Dashboard personale con statistiche
- Gestione timbrature (check-in/check-out)
- Visualizzazione turni personali
- Gestione colleghi e merchant
- Sistema smart di timbratura con validazione orari

**Pagine:**
- `/login` - Login dipendente
- `/register` - Registrazione dipendente
- `/` - Dashboard dipendente
- `/timbratura` - Sistema timbrature
- `/my-shifts` - I miei turni
- `/colleagues` - Colleghi

## 🔧 Configurazione

### File di Configurazione

Il progetto usa una gerarchia di configurazione:
1. `appsettings.json` - Base (versionato)
2. `appsettings.Development.json` - Development (versionato)
3. `appsettings.Local.json` - Locale personale (NON versionato)

**Mai committare:**
- Password o API keys
- `appsettings.Local.json`
- Certificati o chiavi private

**Setup locale:**
```bash
cp appsettings.Local.json.example appsettings.Local.json
# Modifica con le tue credenziali
```

**Alternative sicure:**
- User Secrets (consigliato): `dotnet user-secrets set "Key" "Value"`
- Environment variables: `export Key__SubKey="Value"`

Vedi [CONFIGURATION.md](backend/CONFIGURATION.md) per dettagli completi.

## 🌐 CORS

Il backend è configurato per accettare richieste da:
- `http://localhost:5173` (Consumer App)
- `http://localhost:5174` (Merchant App)
- `http://localhost:5175` (Admin App)
- `http://localhost:5176` (Employee App)

Modifica in `Program.cs` per ambiente di produzione.

## 📝 API Endpoints

### Auth
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Registrazione
- `POST /api/employee-auth/login` - Login dipendente
- `POST /api/employee-auth/register` - Registrazione dipendente

### Bookings (Autenticato)
- `GET /api/bookings` - Le mie prenotazioni
- `GET /api/bookings/{id}` - Dettaglio prenotazione
- `POST /api/bookings` - Crea prenotazione
- `PATCH /api/bookings/{id}/cancel` - Cancella prenotazione

### Employee (Autenticato Employee)
- `GET /api/employees` - Lista dipendenti
- `GET /api/employees/{id}` - Dettaglio dipendente
- `GET /api/timbrature/my-timbrature` - Le mie timbrature
- `POST /api/timbrature/check-in` - Timbra entrata
- `POST /api/timbrature/check-out` - Timbra uscita
- `GET /api/employee-colleagues/my-merchants` - I miei merchant
- `GET /api/shift-templates` - Template turni
- `GET /api/shift-swap-requests` - Richieste scambio turni

### Merchant (Autenticato Merchant)
- `GET /api/merchants` - Lista merchant
- `GET /api/services` - Servizi del merchant
- `POST /api/services` - Crea servizio
- `GET /api/availability` - Disponibilità servizi
- `GET /api/business-hours` - Orari apertura
- `GET /api/closure-period` - Periodi di chiusura

### Admin (Autenticato Admin)
- `GET /api/merchants/pending` - Merchant in attesa approvazione
- `PATCH /api/merchants/{id}/approve` - Approva merchant
- `PATCH /api/merchants/{id}/reject` - Rifiuta merchant

### System
- `GET /api/version` - Versione applicazione

## 🎨 Stili e UI

- **Tailwind CSS** per styling rapido
- **Mobile-first** responsive design
- **Gradient backgrounds** per differenziare:
  - Consumer (blu/viola)
  - Merchant (verde/blu)
  - Admin (viola/rosso)
  - Employee (arancione/giallo)

## 🔜 Roadmap

### Fase 1: Core Features ✅ COMPLETATA
- [x] Setup progetto e architettura
- [x] Autenticazione JWT con 3 ruoli
- [x] Database multi-verticale
- [x] Frontend Consumer e Merchant base
- [x] Admin panel per approvare merchant
- [x] CRUD completo servizi merchant
- [x] Sistema prenotazioni completo

### Fase 2: Advanced Features ✅ COMPLETATA
- [x] Calendario disponibilita' con slot orari (3 modalità: TimeSlot, TimeRange, DayOnly)
- [x] Sistema gestione dipendenti (Employee App)
- [x] Sistema timbrature smart con validazione orari
- [x] Gestione turni e template turni
- [x] Scambio turni tra colleghi
- [x] Periodi di chiusura merchant
- [x] Limiti orari lavorativi dipendenti
- [x] Orari apertura business
- [x] Sistema versioning Git-based
- [x] Deploy produzione su Azure (4 App Services)
- [ ] Sistema notifiche (email/push)
- [ ] Recensioni e rating
- [ ] Upload immagini servizi
- [ ] Ricerca e filtri avanzati

### Fase 3: Business Features
- [ ] Pagamenti integrati (Stripe)
- [ ] Analytics e report merchant
- [ ] Sistema fedalta' utenti
- [ ] Promozioni e sconti
- [ ] Export dati (CSV, PDF)

### Fase 4: Scale & Polish
- [ ] App mobile (React Native)
- [ ] Ottimizzazioni performance
- [ ] Testing completo (unit + e2e)
- [ ] CI/CD pipeline
- [ ] Documentazione API completa

## Contribuire

1. Leggi [PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md)
2. Crea branch da `main`
3. Segui coding standards (DI, comments, no emoji in codice)
4. Testa in locale
5. Crea Pull Request

## 🌐 Produzione e Deployment

L'applicazione è deployata su **Microsoft Azure** con architettura multi-servizio:

### Componenti Azure
- **5 App Services** (Linux, Node 20 / .NET 8):
  - Backend API (.NET 8)
  - Consumer App (React + Express)
  - Merchant App (React + Express)
  - Admin App (React + Express)
  - Employee App (React + Express)
- **PostgreSQL Database** (Azure Database for PostgreSQL - Flexible Server)
- **GitHub Actions CI/CD** per deployment automatico

### URLs Produzione
Vedi [PRODUCTION_ARCHITECTURE.md](PRODUCTION_ARCHITECTURE.md) per dettagli completi su:
- Configurazione Azure App Services
- Variabili d'ambiente richieste
- Workflow GitHub Actions
- Monitoring e health checks
- Troubleshooting produzione

### CI/CD Pipeline
Il progetto usa GitHub Actions per deployment automatico:
- Push su `main` → Deploy automatico dei componenti modificati
- Build separati per backend e ogni frontend
- Health checks pre e post-deployment

## Troubleshooting

### Backend non si avvia
- Verifica PostgreSQL in esecuzione
- Controlla connection string in appsettings.Local.json
- Verifica JWT SecretKey configurata (min 32 caratteri)

### Frontend errori CORS
- Verifica backend in esecuzione su porta corretta
- Controlla configurazione CORS in Program.cs
- In produzione, verifica variabili CorsOrigins in Azure

### Database errori
```bash
# Reset completo
dotnet ef database drop -f
dotnet ef database update
```

### Problemi Timezone
Il sistema usa UTC internamente e converte per display. Vedi [TROUBLESHOOTING.md](TROUBLESHOOTING.md) per fix comuni.

Vedi [SETUP.md](SETUP.md) e [TROUBLESHOOTING.md](TROUBLESHOOTING.md) per troubleshooting dettagliato.

## Risorse

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [React Docs](https://react.dev)
- [Tailwind CSS](https://tailwindcss.com)

## Licenza

Progetto privato.

## Autore

Giovanni Fantini
