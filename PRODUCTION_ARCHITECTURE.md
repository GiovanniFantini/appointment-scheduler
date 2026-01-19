# Architettura di Produzione - Appointment Scheduler

## Panoramica

L'applicazione è deployata su **Azure App Service** (Linux) con deployment automatico tramite **GitHub Actions**.

### Componenti

L'architettura consiste di **5 Azure App Services separati**:

1. **Backend API** (.NET 8)
2. **Consumer App** (React + Vite + Express)
3. **Merchant App** (React + Vite + Express)
4. **Admin App** (React + Vite + Express)
5. **Employee App** (React + Vite + Express)

---

## Backend API (.NET 8)

### Stack
- ASP.NET Core 8.0
- PostgreSQL (Azure Database for PostgreSQL)
- JWT Authentication
- Entity Framework Core

### Endpoint
- `/api/*` - API endpoints
- `/swagger` - Documentazione API
- `/health`, `/health/ready`, `/health/live` - Health checks

### Configurazione Azure

**Variabili d'Ambiente Richieste:**
```bash
POSTGRESQLCONNSTR_DefaultConnection  # Connection string PostgreSQL
JwtSettings__SecretKey              # Chiave segreta JWT (min 32 caratteri)
JwtSettings__Issuer                 # Issuer JWT
JwtSettings__Audience               # Audience JWT
CorsOrigins__0                      # URL Consumer App
CorsOrigins__1                      # URL Merchant App
CorsOrigins__2                      # URL Admin App
CorsOrigins__3                      # URL Employee App
RUN_MIGRATIONS                      # true/false (default: true)
SEED_DATABASE                       # true/false (default: false)
```

### GitHub Actions Workflow

**File:** `.github/workflows/deploy-backend-api.yml`

**Trigger:** Push su `main` con modifiche in `backend/**`

**Steps:**
1. Setup .NET 8.0
2. Build progetto `AppointmentScheduler.API`
3. Publish in modalità Release
4. Deploy su Azure tramite `azure/webapps-deploy@v2`

---

## Frontend Apps (Consumer, Merchant, Admin)

### Stack Comune
- React 18 + TypeScript
- Vite 6 (build tool)
- Tailwind CSS 3.4
- Express.js (server Node.js 20)
- Axios (HTTP client)

### Architettura Server Express

Ogni frontend ha un server Express (`server.js`) che:
1. Serve file statici dalla directory `dist/`
2. Proxy delle API verso il backend
3. Gestisce health checks
4. Supporta client-side routing (SPA)

**Configurazione:**
```javascript
const PORT = process.env.PORT || 8080;
const API_URL = process.env.API_URL || 'https://appointment-scheduler-api.azurewebsites.net';
```

### Consumer App
- Homepage utenti finali
- Prenotazione servizi
- Workflow: `.github/workflows/deploy-consumer-app.yml`

### Merchant App
- Dashboard merchant
- Gestione servizi e prenotazioni
- Workflow: `.github/workflows/deploy-merchant-app.yml`

### Admin App
- Pannello amministrativo
- Approvazione merchant
- Workflow: `.github/workflows/deploy-admin-app.yml`

### Employee App
- App dipendenti
- Sistema timbrature e turni
- Workflow: `.github/workflows/deploy-employee-app.yml`

### Build e Deploy Process

**Build Phase:**
```bash
npm ci              # Install dependencies
tsc                 # TypeScript compilation
vite build          # Build production → dist/
```

**Deploy Phase:**
```bash
ZIP Deploy (dist/ + node_modules/ + server.js)
Azure startup: "npm start" → node server.js
```

### App Settings (Comuni)
```json
{
  "API_URL": "https://appointment-scheduler-api.azurewebsites.net",
  "NODE_ENV": "production",
  "SCM_DO_BUILD_DURING_DEPLOYMENT": "false",
  "WEBSITE_NODE_DEFAULT_VERSION": "~20"
}
```

---

## Flusso di Comunicazione

```
Browser → Frontend App (Express)
          ├─ /api/* → Proxy → Backend API → PostgreSQL
          └─ /* → dist/index.html (React SPA)
```

### Esempio Chiamata API

1. Browser: `GET https://consumer-app.azurewebsites.net/api/merchants`
2. Express: Intercetta con proxy middleware
3. Proxy: Inoltra a `https://appointment-scheduler-api.azurewebsites.net/api/merchants`
4. Backend: Processa, query PostgreSQL, risponde
5. Browser: Riceve dati

### CORS Configuration

Il backend API configura CORS per i 3 frontend:

```csharp
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

---

## GitHub Actions - CI/CD

### Deployment Automatico

| Modifiche in                | Workflow                     |
|-----------------------------|------------------------------|
| `backend/**`                | `deploy-backend-api.yml`     |
| `frontend/consumer-app/**`  | `deploy-consumer-app.yml`    |
| `frontend/merchant-app/**`  | `deploy-merchant-app.yml`    |
| `frontend/admin-app/**`     | `deploy-admin-app.yml`       |
| `frontend/employee-app/**`  | `deploy-employee-app.yml`    |

### GitHub Secrets Richiesti

**Backend:**
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

**Frontend (per ogni app):**
- `AZURE_WEBAPP_NAME_[CONSUMER|MERCHANT|ADMIN]`
- `AZURE_WEBAPP_PUBLISH_PROFILE_[CONSUMER|MERCHANT|ADMIN]`
- `AZURE_CREDENTIALS`
- `AZURE_RESOURCE_GROUP`
- `VITE_API_URL`
- `API_URL`

---

## Database

### Tecnologia
Azure Database for PostgreSQL (Flexible Server)

### Connection String Format
```
Server=<server>.postgres.database.azure.com;Database=<dbname>;Port=5432;User Id=<username>;Password=<password>;Ssl Mode=Require;
```

### Migrazioni
Il backend applica automaticamente le migrazioni EF Core all'avvio se `RUN_MIGRATIONS=true`.

**ATTENZIONE:** Per disabilitare in produzione: `RUN_MIGRATIONS=false`

---

## Monitoring e Health Checks

### Endpoint Health Check

**Backend:**
- `GET /health` - Check generale (include database)
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

**Frontend:**
- `GET /health` - Check generale
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### Logging

Vedere log in tempo reale:
```bash
az webapp log tail --name <app-service-name> --resource-group <resource-group>
```

Oppure via Azure Portal: App Service → Monitoring → Log stream

---

## Troubleshooting

### Controllare i Log
```bash
az webapp log tail --name <app-service-name> --resource-group <resource-group>
```

### Endpoint Debug (Admin App)
```
GET /api-debug/config
```
Restituisce configurazione runtime (API_URL, NODE_ENV, PORT).

### Problemi Comuni

**Backend non riceve richieste API**
- Verifica CORS configurato con URL frontend
- Controlla `API_URL` nei frontend App Services
- Testa backend: `GET /health`

**Frontend mostra "502 Bad Gateway"**
- Express server non partito
- Verifica startup command: `npm start`
- Controlla che `node_modules` e `server.js` siano deployati

**Build Failure: "npx tsc installs wrong package"**
- Usa `tsc` direttamente nel build script (non `npx tsc`)

---

## Architettura Riassuntiva

```
┌────────────────────────────────────────────────────────┐
│                 Internet (HTTPS)                       │
└────┬──────────┬──────────┬──────────┬──────────────────┘
     │          │          │          │
┌────▼────┐┌───▼────┐┌────▼────┐┌────▼────┐
│Consumer ││Merchant││  Admin  ││Employee │
│  App    ││  App   ││   App   ││   App   │
│(Node 20)││(Node 20)││(Node 20)││(Node 20)│
└────┬────┘└───┬────┘└────┬────┘└────┬────┘
     │          │          │          │
     └──────────┴──────────┴──────────┘
                   │ Proxy /api/*
             ┌─────▼─────┐
             │  Backend  │
             │    API    │
             │  (.NET 8) │
             └─────┬─────┘
                   │
             ┌─────▼─────┐
             │PostgreSQL │
             │ Database  │
             └───────────┘
```

Ogni frontend è un App Service separato che usa Express.js per proxy API e servire la React SPA.

---

## Prossimi Passi

1. Configurare Application Insights per monitoring avanzato
2. Setup Azure CDN per assets statici
3. Implementare caching (Redis)
4. Setup staging slots per deploy blue/green
5. Configurare auto-scaling
6. Backup automatici database
7. Setup alerting per downtime/errori
