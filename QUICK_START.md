# Quick Start Guide

Guida rapida per iniziare a sviluppare in locale.

## Prerequisiti

- .NET 8 SDK
- Node.js 18+
- PostgreSQL (o Docker)
- Git

## Setup in 5 Minuti

### 1. Clone e Setup Database

```bash
# Clone del repository
git clone https://github.com/GiovanniFantini/appointment-scheduler.git
cd appointment-scheduler

# Avvia PostgreSQL con Docker (o usa installazione locale)
docker run --name appointment-db \
  -e POSTGRES_PASSWORD=dev123 \
  -e POSTGRES_DB=AppointmentScheduler \
  -p 5432:5432 \
  -d postgres:16
```

### 2. Configurazione Backend

```bash
cd backend/AppointmentScheduler.API

# Copia configurazione locale
cp appsettings.Local.json.example appsettings.Local.json

# Modifica appsettings.Local.json con le tue credenziali
# (usa l'editor che preferisci)
```

**appsettings.Local.json** (esempio per Docker):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AppointmentScheduler;Username=postgres;Password=dev123"
  },
  "JwtSettings": {
    "SecretKey": "development-secret-key-at-least-32-characters-long-change-in-prod",
    "Issuer": "AppointmentScheduler.API",
    "Audience": "AppointmentScheduler.Client",
    "ExpirationMinutes": 1440
  }
}
```

### 3. Crea Database e Tabelle

```bash
# Installa EF Core tools (se non presente)
dotnet tool install --global dotnet-ef

# Crea migrazione iniziale
dotnet ef migrations add InitialCreate --project ../AppointmentScheduler.Data

# Applica al database
dotnet ef database update

# Avvia backend
dotnet run
```

Backend disponibile su: `https://localhost:5001` (Swagger: `https://localhost:5001/swagger`)

### 4. Avvia Frontend

```bash
# Consumer App (utenti)
cd frontend/consumer-app
npm install
npm run dev
# Disponibile su http://localhost:5173

# Merchant App (business) - in un'altra finestra
cd frontend/merchant-app
npm install
npm run dev
# Disponibile su http://localhost:5174
```

## Test Rapido

### 1. Registra un Utente

Vai su `http://localhost:5173/register`:
- Nome: Mario
- Cognome: Rossi
- Email: mario@test.com
- Password: Password123

### 2. Registra un Merchant

Vai su `http://localhost:5174/register`:
- Nome: Luca
- Cognome: Bianchi
- Email: luca@merchant.com
- Password: Password123
- Nome Business: Ristorante Da Luca

**Nota:** Il merchant deve essere approvato da un admin prima di operare.

### 3. Testa API con Swagger

Vai su `https://localhost:5001/swagger`:
1. POST `/api/auth/login` con le credenziali
2. Copia il token dalla risposta
3. Click su "Authorize" e incolla: `Bearer {token}`
4. Ora puoi testare tutti gli endpoint autenticati

## Struttura Progetto

```
appointment-scheduler/
├── backend/
│   ├── AppointmentScheduler.API/         # Web API - Avvia con dotnet run
│   ├── AppointmentScheduler.Core/        # Business logic
│   ├── AppointmentScheduler.Data/        # Database
│   └── AppointmentScheduler.Shared/      # Models condivisi
├── frontend/
│   ├── consumer-app/                      # App utenti - npm run dev
│   └── merchant-app/                      # App merchant - npm run dev
└── docs/                                  # Documentazione
```

## Comandi Utili

### Backend

```bash
# Restore dipendenze
dotnet restore

# Build
dotnet build

# Run con hot reload
dotnet watch run

# Nuova migrazione
dotnet ef migrations add NomeMigrazione --project ../AppointmentScheduler.Data

# Rollback migrazione
dotnet ef database update PrecedenteMigrazione

# Drop database
dotnet ef database drop
```

### Frontend

```bash
# Installa dipendenze
npm install

# Dev server
npm run dev

# Build produzione
npm run build

# Preview build
npm run preview
```

### Docker (opzionale)

```bash
# Start PostgreSQL
docker start appointment-db

# Stop PostgreSQL
docker stop appointment-db

# Logs
docker logs appointment-db

# Connect to DB
docker exec -it appointment-db psql -U postgres -d AppointmentScheduler
```

## Troubleshooting

### Backend non si avvia

**Errore: "Unable to connect to database"**
- Verifica che PostgreSQL sia in esecuzione: `docker ps`
- Controlla connection string in appsettings.Local.json
- Test connessione: `docker exec -it appointment-db psql -U postgres`

**Errore: "JWT SecretKey not configured"**
- Verifica che appsettings.Local.json esista
- La SecretKey deve essere almeno 32 caratteri

**Errore: "Port already in use"**
- Cambia porta in launchSettings.json o
- Trova processo: `lsof -i :5001` e uccidi: `kill -9 PID`

### Frontend non si avvia

**Errore: "Cannot find module"**
```bash
rm -rf node_modules package-lock.json
npm install
```

**Errore: "CORS policy"**
- Verifica che backend sia in esecuzione
- Controlla configurazione CORS in Program.cs

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

## Prossimi Passi

1. Leggi [PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md) per coding standards
2. Leggi [CONFIGURATION.md](backend/CONFIGURATION.md) per gestione configurazioni
3. Leggi [README.md](README.md) per architettura completa

## Supporto

Per problemi o domande:
1. Controlla [Troubleshooting](#troubleshooting)
2. Verifica [Issues](https://github.com/GiovanniFantini/appointment-scheduler/issues)
3. Crea nuovo issue con tag `question` o `bug`
