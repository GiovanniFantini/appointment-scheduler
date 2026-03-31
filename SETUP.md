# Setup Locale — Gestionale Aziendale

## Prerequisiti

- .NET 8 SDK
- Node.js 22+
- PostgreSQL 16+
- Git

## 1. Backend

```bash
cd backend

# Ripristina pacchetti
dotnet restore

# Configura connection string (crea appsettings.Local.json)
# oppure esporta la variabile d'ambiente:
# ConnectionStrings__DefaultConnection="Host=localhost;Database=gestionale;Username=postgres;Password=postgres"

# Applica migration al DB (il codice la esegue automaticamente all'avvio)
# oppure manualmente:
cd AppointmentScheduler.Data
dotnet ef database update --startup-project ../AppointmentScheduler.API

# Avvia API
cd ../AppointmentScheduler.API
dotnet run
# API disponibile su http://localhost:5000
# Swagger su http://localhost:5000/swagger
```

### Variabili di configurazione (appsettings.Local.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gestionale;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars-change-me",
    "Issuer": "GestionaleAziendale.API",
    "Audience": "GestionaleAziendale.Client",
    "ExpirationMinutes": 1440
  },
  "SEED_DATABASE": true
}
```

Al primo avvio vengono creati:

- Admin di default: `admin@admin.com` / `password`

## 2. Frontend

Ogni app è indipendente. Avviale in terminali separati:

```bash
# Admin App (port 5175)
cd frontend/admin-app
npm install
npm run dev

# Merchant App (port 5174)
cd frontend/merchant-app
npm install
npm run dev

# Employee App (port 5176)
cd frontend/employee-app
npm install
npm run dev

# Consumer App — Work in Progress (port 5173)
cd frontend/consumer-app
npm install
npm run dev
```

## 3. Build di produzione

```bash
# Backend
cd backend
dotnet build -c Release

# Ogni frontend
cd frontend/admin-app && npm run build
cd frontend/merchant-app && npm run build
cd frontend/employee-app && npm run build
cd frontend/consumer-app && npm run build
```

## Credenziali default (sviluppo)

| Account | Email                | Password   |
|---------|----------------------|------------|
| Admin   | `admin@admin.com`    | `password` |

## Flusso di registrazione

1. **Merchant**: si registra su `/register` → viene creato `IsApproved=false` → l'admin approva da `/merchants`
2. **Employee**: si registra su `/login` (employee) → inserisce email → viene collegato alle aziende che lo hanno pre-caricato
3. **Admin**: esiste solo tramite seed o creazione manuale (non c'è registrazione pubblica)

## Azure (produzione)

Configura queste variabili d'ambiente nell'App Service:

```env
POSTGRESQLCONNSTR_DefaultConnection=...
JwtSettings__SecretKey=...
ConnectionStrings__AzureBlobStorage=...
AzureCommunicationServices__ConnectionString=...
AzureCommunicationServices__SenderAddress=...
CorsOrigins__0=https://your-admin-app.azurewebsites.net
CorsOrigins__1=https://your-merchant-app.azurewebsites.net
CorsOrigins__2=https://your-employee-app.azurewebsites.net
CorsOrigins__3=https://your-consumer-app.azurewebsites.net
RUN_MIGRATIONS=true
SEED_DATABASE=false
```
