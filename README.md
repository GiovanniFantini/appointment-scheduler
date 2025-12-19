# Appointment Scheduler

Piattaforma di prenotazione multi-verticale (B2C e B2B) che centralizza l'esperienza di prenotazione per utenti finali e gestione business per merchant.

## 🎯 Obiettivo del Progetto

Creare una Web App mobile-ready bidirezionale che:
- **B2C**: Permette agli utenti di prenotare servizi (ristoranti, sport, wellness, ecc.)
- **B2B**: Fornisce ai merchant strumenti per gestire prenotazioni e business
- **Multi-verticale**: Gestisce diversi settori in un unico ecosistema

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
    └── merchant-app/                      # Dashboard merchant (porta 5174)
```

## 👥 Ruoli Utente

1. **User (Consumer)**: Può solo prenotare servizi
2. **Merchant (Business)**: Gestisce le proprie attività e prenotazioni
3. **Admin**: Gestisce permessi e approva i merchant

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

## 🚀 Come Iniziare

### Prerequisiti

- .NET 8 SDK
- Node.js 18+
- PostgreSQL (o SQL Server)

### Backend Setup

1. Vai nella cartella backend:
```bash
cd backend
```

2. Configura la connection string in `AppointmentScheduler.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AppointmentScheduler;Username=postgres;Password=yourpassword"
  }
}
```

3. Crea il database (da AppointmentScheduler.API):
```bash
dotnet ef migrations add InitialCreate --project ../AppointmentScheduler.Data
dotnet ef database update
```

4. Avvia l'API:
```bash
cd AppointmentScheduler.API
dotnet run
```

L'API sarà disponibile su `https://localhost:5001` (o `http://localhost:5000`)

### Frontend Setup

#### Consumer App (Utenti)

```bash
cd frontend/consumer-app
npm install
npm run dev
```

Disponibile su `http://localhost:5173`

#### Merchant App (Business)

```bash
cd frontend/merchant-app
npm install
npm run dev
```

Disponibile su `http://localhost:5174`

## 🔐 Autenticazione

Il sistema usa JWT Bearer Token:

1. **Login/Register** → Ricevi un token JWT
2. Invia il token nell'header: `Authorization: Bearer {token}`
3. Il token contiene UserId, Email e Role

### Policy di Autorizzazione

- **AdminOnly**: Solo Admin
- **MerchantOnly**: Merchant e Admin
- **UserOnly**: User, Merchant e Admin

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

## 🔧 Configurazione JWT

In `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-change-this-in-production-min-32-chars",
    "Issuer": "AppointmentScheduler.API",
    "Audience": "AppointmentScheduler.Client",
    "ExpirationMinutes": 1440
  }
}
```

**⚠️ IMPORTANTE**: Cambia la `SecretKey` in produzione!

## 🌐 CORS

Il backend è configurato per accettare richieste da:
- `http://localhost:5173` (Consumer App)
- `http://localhost:5174` (Merchant App)

Modifica in `Program.cs` per ambiente di produzione.

## 📝 API Endpoints

### Auth
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Registrazione

### Bookings (Autenticato)
- `GET /api/bookings` - Le mie prenotazioni
- `GET /api/bookings/{id}` - Dettaglio prenotazione
- `POST /api/bookings` - Crea prenotazione
- `PATCH /api/bookings/{id}/cancel` - Cancella prenotazione

## 🎨 Stili e UI

- **Tailwind CSS** per styling rapido
- **Mobile-first** responsive design
- **Gradient backgrounds** per differenziare Consumer (blu/viola) e Merchant (verde/blu)

## 🔜 Prossimi Sviluppi

- [ ] Admin panel per approvare merchant
- [ ] CRUD completo per servizi (merchant)
- [ ] Calendario prenotazioni con disponibilità
- [ ] Sistema di notifiche (email/push)
- [ ] Recensioni e rating
- [ ] Pagamenti integrati
- [ ] Analytics avanzate per merchant
- [ ] App mobile native (React Native)

## 📄 Licenza

Progetto privato.

## 👨‍💻 Autore

Giovanni Fantini
