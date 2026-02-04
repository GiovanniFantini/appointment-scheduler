# Setup Locale - Appointment Scheduler

Guida completa per configurare l'ambiente di sviluppo locale.

## Prerequisiti

- .NET 8 SDK
- Node.js 22+
- PostgreSQL (o Docker)
- Git

## Setup Rapido (5 minuti)

### 1. Database PostgreSQL

```bash
# Avvia PostgreSQL con Docker (raccomandato)
docker run --name appointment-db \
  -e POSTGRES_PASSWORD=dev123 \
  -e POSTGRES_DB=AppointmentScheduler \
  -p 5432:5432 \
  -d postgres:16

# Verifica che sia in esecuzione
docker ps
```

### 2. Backend (.NET 8)

```bash
cd backend/AppointmentScheduler.API

# Copia configurazione locale
cp appsettings.Local.json.example appsettings.Local.json

# Modifica appsettings.Local.json con le tue credenziali
# (Connection string, JWT secret key min 32 caratteri)

# Applica migrazioni database
dotnet ef database update

# Avvia backend
dotnet run
```

Backend disponibile su:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `http://localhost:5000/swagger`

### 3. Frontend

Scegli quale app frontend avviare (o tutte e 4 in terminali separati):

```bash
# Consumer App (utenti) - porta 5173
cd frontend/consumer-app
npm install
npm run dev

# Merchant App (business) - porta 5174
cd frontend/merchant-app
npm install
npm run dev

# Admin App - porta 5175
cd frontend/admin-app
npm install
npm run dev

# Employee App (dipendenti) - porta 5176
cd frontend/employee-app
npm install
npm run dev
```

## Configurazione Dettagliata

### appsettings.Local.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AppointmentScheduler;Username=postgres;Password=dev123"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "Issuer": "AppointmentScheduler.API",
    "Audience": "AppointmentScheduler.Client",
    "ExpirationMinutes": 1440
  }
}
```

### Alternative a appsettings.Local.json

**User Secrets (raccomandato):**
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"
```

**Variabili d'ambiente:**
```bash
# Linux/macOS
export ConnectionStrings__DefaultConnection="Host=localhost;..."
export JwtSettings__SecretKey="your-secret-key"
```

## Comandi Utili

### Backend

```bash
# Build
dotnet build

# Run con hot reload
dotnet watch run

# Nuova migrazione
dotnet ef migrations add NomeMigrazione --project ../AppointmentScheduler.Data

# Reset database
dotnet ef database drop -f
dotnet ef database update
```

### Frontend

```bash
# Dev server
npm run dev

# Build produzione
npm run build

# Preview build
npm run preview
```

### Docker

```bash
# Start PostgreSQL
docker start appointment-db

# Stop PostgreSQL
docker stop appointment-db

# Connect to DB
docker exec -it appointment-db psql -U postgres -d AppointmentScheduler
```

## Workflow Completo Sviluppo

### Setup Iniziale (una volta)

```bash
# 1. Clone repository
git clone https://github.com/GiovanniFantini/appointment-scheduler.git
cd appointment-scheduler

# 2. Backend setup
cd backend
dotnet restore
dotnet ef database update --startup-project AppointmentScheduler.API

# 3. Frontend setup
cd ../frontend/merchant-app
npm install

cd ../employee-app
npm install
```

### Ogni Giorno (3 terminali)

**Terminale 1 - Backend:**
```bash
cd backend
dotnet run --project AppointmentScheduler.API
```

**Terminale 2 - Merchant App:**
```bash
cd frontend/merchant-app
npm run dev
```

**Terminale 3 - Employee App:**
```bash
cd frontend/employee-app
npm run dev
```

## Troubleshooting

### Backend non si avvia

**Errore: "Unable to connect to database"**
- Verifica PostgreSQL in esecuzione: `docker ps`
- Controlla connection string in appsettings.Local.json
- Test connessione: `docker exec -it appointment-db psql -U postgres`

**Errore: "JWT SecretKey not configured"**
- Verifica che appsettings.Local.json esista
- La SecretKey deve essere almeno 32 caratteri

**Errore: "Port already in use"**
- Cambia porta in launchSettings.json
- Trova processo: `lsof -i :5000` e termina: `kill -9 PID`

### Frontend non si avvia

**Errore: "Cannot find module"**
```bash
rm -rf node_modules package-lock.json
npm install
```

**Errore: "CORS policy"**
- Verifica che backend sia in esecuzione
- Controlla configurazione CORS in Program.cs

**Frontend non si connette al backend**
- Verifica di usare `npm run dev` (NON `npm run build`)
- Il proxy Vite deve essere attivo (solo in modalità dev)
- Controlla che backend sia su `http://localhost:5000`

### Database

**Reset completo database**
```bash
cd backend/AppointmentScheduler.API
dotnet ef database drop -f
dotnet ef database update
```

**Verifica tabelle create**
```bash
docker exec -it appointment-db psql -U postgres -d AppointmentScheduler -c "\dt"
```

## Test Rapidi

### Verifica Backend
```bash
# Test health check
curl http://localhost:5000/health

# Test API version
curl http://localhost:5000/api/version

# Swagger UI (browser)
open http://localhost:5000/swagger
```

### Verifica Proxy Frontend
```javascript
// Browser console (con frontend su localhost:5174)
fetch('/api/version').then(r => r.json()).then(console.log)
// Deve ritornare: {version: "1.0.0", buildTime: "..."}
```

## Accesso Applicazioni

- Consumer App: `http://localhost:5173`
- Merchant App: `http://localhost:5174`
- Admin App: `http://localhost:5175`
- Employee App: `http://localhost:5176`
- Backend API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

## Prossimi Passi

Dopo il setup:
1. Leggi [PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md) per coding standards
2. Leggi [backend/CONFIGURATION.md](backend/CONFIGURATION.md) per gestione secrets
3. Leggi [README.md](README.md) per architettura completa

## Note Importanti

- **Backend e frontend devono essere entrambi in esecuzione**
- **Usa sempre `npm run dev` per sviluppo locale** (NON `npm run build`)
- **Proxy Vite** gestisce automaticamente CORS in sviluppo
- **Hot reload** funziona sia backend (.NET) che frontend (Vite)
- **Migration** vengono applicate automaticamente se RUN_MIGRATIONS=true
