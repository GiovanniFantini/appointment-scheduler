# Architettura di Produzione - Appointment Scheduler

## Panoramica

L'applicazione Appointment Scheduler è deployata su **Azure App Service** su piattaforma **Linux** con deployment automatico tramite **GitHub Actions**.

### Componenti in Produzione

L'architettura consiste di **4 Azure App Services separati**:

1. **Backend API** (.NET 8)
2. **Consumer App** (React + Vite + Node.js)
3. **Merchant App** (React + Vite + Node.js)
4. **Admin App** (React + Vite + Node.js)

---

## 1. Backend API (.NET 8)

### Tecnologie
- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL (via Azure Database for PostgreSQL)
- **Autenticazione**: JWT Bearer Token
- **ORM**: Entity Framework Core con Npgsql

### Endpoint Principali
- `/api/*` - API endpoints per business logic
- `/swagger` - Documentazione API (Swagger UI)
- `/health` - Health check principale
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Configurazione Azure
- **App Service Name**: `${{ secrets.AZURE_WEBAPP_NAME }}`
- **Deploy Method**: ZIP Deploy
- **Runtime**: .NET 8.0 su Linux
- **Connection String**: `POSTGRESQLCONNSTR_DefaultConnection`

### Workflow GitHub Actions
**File**: `.github/workflows/deploy-backend-api.yml`

**Trigger**:
- Push su branch `main` con modifiche in `backend/**`
- Workflow dispatch manuale

**Steps**:
1. Setup .NET 8.0
2. Build del progetto `AppointmentScheduler.API`
3. Publish in modalità Release
4. Deploy su Azure tramite `azure/webapps-deploy@v2`

### Variabili d'Ambiente Richieste
```bash
POSTGRESQLCONNSTR_DefaultConnection  # Connection string PostgreSQL
JwtSettings__SecretKey              # Chiave segreta per JWT
JwtSettings__Issuer                 # Issuer JWT
JwtSettings__Audience               # Audience JWT
CorsOrigins__0                      # URL Consumer App
CorsOrigins__1                      # URL Merchant App
CorsOrigins__2                      # URL Admin App
RUN_MIGRATIONS                      # true/false (default: true)
SEED_DATABASE                       # true/false (default: false in prod)
```

---

## 2. Frontend Apps (Consumer, Merchant, Admin)

### Architettura Comune

Ogni frontend app è deployata su un **App Service separato** e segue lo stesso pattern architetturale:

#### Stack Tecnologico
- **Framework UI**: React 18 + React Router
- **Build Tool**: Vite 6
- **Linguaggio**: TypeScript 5.6
- **Styling**: Tailwind CSS 3.4
- **Server**: Express.js (Node.js 20)
- **HTTP Client**: Axios

#### Struttura del Server Express (`server.js`)

Ogni frontend ha un server Express che:

1. **Serve i file statici** dalla directory `dist/` (output di Vite build)
2. **Proxy delle API** verso il backend
3. **Health checks** per Azure App Service
4. **Client-side routing** (SPA support)

```javascript
// Configurazione base comune a tutti i frontend
const PORT = process.env.PORT || 8080;
const API_URL = process.env.API_URL || 'https://appointment-scheduler-api.azurewebsites.net';
```

### 2.1 Consumer App

**Descrizione**: Interfaccia per gli utenti finali che vogliono prenotare servizi.

**App Service Name**: `${{ secrets.AZURE_WEBAPP_NAME_CONSUMER }}`

**Workflow**: `.github/workflows/deploy-consumer-app.yml`

**Endpoint**:
- `/` - Homepage consumer
- `/api/*` - Proxy al backend API
- `/health`, `/health/ready`, `/health/live` - Health checks

### 2.2 Merchant App

**Descrizione**: Interfaccia per i merchant (fornitori di servizi) per gestire i propri servizi e prenotazioni.

**App Service Name**: `${{ secrets.AZURE_WEBAPP_NAME_MERCHANT }}`

**Workflow**: `.github/workflows/deploy-merchant-app.yml`

**Endpoint**:
- `/` - Dashboard merchant
- `/api/*` - Proxy al backend API
- `/health`, `/health/ready`, `/health/live` - Health checks

### 2.3 Admin App

**Descrizione**: Interfaccia amministrativa per gestire la piattaforma.

**App Service Name**: `${{ secrets.AZURE_WEBAPP_NAME_ADMIN }}`

**Workflow**: `.github/workflows/deploy-admin-app.yml`

**Endpoint**:
- `/` - Dashboard admin
- `/api/*` - Proxy al backend API
- `/api-debug/config` - Endpoint di debug per verificare configurazione
- `/health`, `/health/ready`, `/health/live` - Health checks

### Processo di Build e Deploy Frontend

**Build Phase** (GitHub Actions Runner):
```bash
1. npm ci                  # Install dependencies
2. tsc                     # TypeScript compilation
3. vite build              # Build production bundle → dist/
```

**Deploy Phase** (Azure App Service):
```bash
1. ZIP Deploy dell'intera directory frontend (inclusi dist/, node_modules/, server.js)
2. Azure configura startup command: "npm start"
3. Express server avvia e:
   - Serve i file statici da dist/
   - Proxy le chiamate /api/* al backend
```

### Configurazione Azure per Frontend Apps

#### App Settings (Common)
```json
{
  "API_URL": "https://appointment-scheduler-api.azurewebsites.net",
  "NODE_ENV": "production",
  "SCM_DO_BUILD_DURING_DEPLOYMENT": "false",
  "WEBSITE_NODE_DEFAULT_VERSION": "~20"
}
```

#### Startup Command
```bash
npm start
```

Questo esegue `node server.js` che avvia Express.

---

## 3. Flusso di Comunicazione Frontend → Backend

### 3.1 Architettura del Proxy

Ogni frontend app utilizza **http-proxy-middleware** per inoltrare le richieste API al backend:

```
┌─────────────────┐
│   Browser       │
│  (utente)       │
└────────┬────────┘
         │
         │ HTTP Request a /api/merchants
         │
         ▼
┌─────────────────────────────────────┐
│  Azure App Service (Frontend)       │
│  ┌───────────────────────────────┐  │
│  │  Express Server (port 8080)   │  │
│  │                               │  │
│  │  Routes:                      │  │
│  │  • /api/* → PROXY             │  │
│  │  • /* → dist/index.html       │  │
│  └───────────┬───────────────────┘  │
└──────────────┼──────────────────────┘
               │
               │ Proxy Request
               │
               ▼
┌─────────────────────────────────────┐
│  Azure App Service (Backend API)    │
│  ┌───────────────────────────────┐  │
│  │  ASP.NET Core API             │  │
│  │                               │  │
│  │  Routes:                      │  │
│  │  • /api/merchants             │  │
│  │  • /api/bookings              │  │
│  │  • /api/auth                  │  │
│  └───────────┬───────────────────┘  │
└──────────────┼──────────────────────┘
               │
               │ Database Query
               │
               ▼
┌─────────────────────────────────────┐
│  Azure Database for PostgreSQL      │
└─────────────────────────────────────┘
```

### 3.2 Esempio di Chiamata API

**Scenario**: Consumer vuole vedere la lista dei merchant

1. **Browser** → `GET https://consumer-app.azurewebsites.net/api/merchants`
2. **Express Server (Consumer App)** → Intercetta la richiesta tramite proxy middleware
3. **Proxy** → Inoltra a `GET https://appointment-scheduler-api.azurewebsites.net/api/merchants`
4. **Backend API** → Processa la richiesta, autentica JWT, query PostgreSQL
5. **Backend API** → Risponde con JSON
6. **Proxy** → Inoltra la risposta al browser
7. **Browser** → Riceve i dati e li renderizza

### 3.3 Gestione CORS

Il backend API configura CORS per accettare richieste dai 3 frontend:

```csharp
// Program.cs - backend
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>();
// Es: ["https://consumer-app.azurewebsites.net",
//      "https://merchant-app.azurewebsites.net",
//      "https://admin-app.azurewebsites.net"]

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

**IMPORTANTE**: Poiché i frontend usano un proxy, le richieste al backend arrivano dal server Express (non direttamente dal browser), quindi il CORS deve includere gli URL degli App Service frontend.

---

## 4. GitHub Actions - CI/CD Pipeline

### 4.1 Deployment Automatico

Ogni push su branch `main` triggera automaticamente il deploy delle componenti modificate:

| Modifiche in                    | Workflow Attivato                | App Service Target           |
|---------------------------------|----------------------------------|------------------------------|
| `backend/**`                    | `deploy-backend-api.yml`         | Backend API                  |
| `frontend/consumer-app/**`      | `deploy-consumer-app.yml`        | Consumer App                 |
| `frontend/merchant-app/**`      | `deploy-merchant-app.yml`        | Merchant App                 |
| `frontend/admin-app/**`         | `deploy-admin-app.yml`           | Admin App                    |

### 4.2 Secrets Richiesti in GitHub

Per il deployment automatico, i seguenti secrets devono essere configurati nel repository GitHub:

#### Backend API
```
AZURE_WEBAPP_NAME
AZURE_WEBAPP_PUBLISH_PROFILE
```

#### Consumer App
```
AZURE_WEBAPP_NAME_CONSUMER
AZURE_WEBAPP_PUBLISH_PROFILE_CONSUMER
AZURE_CREDENTIALS
AZURE_RESOURCE_GROUP
VITE_API_URL
API_URL
```

#### Merchant App
```
AZURE_WEBAPP_NAME_MERCHANT
AZURE_WEBAPP_PUBLISH_PROFILE_MERCHANT
AZURE_CREDENTIALS
AZURE_RESOURCE_GROUP
VITE_API_URL
API_URL
```

#### Admin App
```
AZURE_WEBAPP_NAME_ADMIN
AZURE_WEBAPP_PUBLISH_PROFILE_ADMIN
AZURE_CREDENTIALS
AZURE_RESOURCE_GROUP
VITE_API_URL
API_URL
```

### 4.3 Workflow Dispatch Manuale

Tutti i workflow possono essere triggerati manualmente tramite GitHub UI:
1. Vai su **Actions**
2. Seleziona il workflow desiderato
3. Click su **Run workflow**
4. Seleziona il branch `main`
5. Click **Run workflow**

---

## 5. Monitoring e Health Checks

### 5.1 Health Check Endpoints

Tutti i servizi espongono endpoint di health check:

#### Backend API
- `GET /health` - Health check generale (include check del database)
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

#### Frontend Apps
- `GET /health` - Health check generale
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### 5.2 Azure Application Insights

Per monitorare le performance e gli errori in produzione, si consiglia di configurare Azure Application Insights per:
- Tracciamento delle richieste HTTP
- Logging degli errori
- Metriche di performance
- Dependency tracking (database, API calls)

---

## 6. Sicurezza

### 6.1 Autenticazione e Autorizzazione

**Backend API** gestisce l'autenticazione tramite JWT:

#### Policies di Autorizzazione
```csharp
- "AdminOnly"     → Solo ruolo Admin
- "MerchantOnly"  → Ruoli Merchant e Admin
- "UserOnly"      → Ruoli User, Merchant e Admin
```

#### Flow di Autenticazione
1. User effettua login tramite frontend
2. Backend valida credenziali e genera JWT token
3. Frontend memorizza il token (localStorage/sessionStorage)
4. Ogni richiesta successiva include header: `Authorization: Bearer <token>`
5. Backend valida il token e autorizza la richiesta

### 6.2 HTTPS

In produzione, Azure App Service fornisce automaticamente certificati SSL/TLS:
- Tutti gli App Service sono accessibili tramite HTTPS
- Il backend forza HTTPS redirect in ambiente non-development

---

## 7. Database

### Tecnologia
**Azure Database for PostgreSQL** (Flexible Server)

### Connection String Format
```
Server=<server>.postgres.database.azure.com;Database=<dbname>;Port=5432;User Id=<username>;Password=<password>;Ssl Mode=Require;
```

### Migrazioni

Il backend applica automaticamente le migrazioni EF Core all'avvio se:
- `RUN_MIGRATIONS` non è impostato o è `true`
- La connection string è configurata

**ATTENZIONE**: Le migrazioni vengono eseguite anche in produzione. Per disabilitare, impostare `RUN_MIGRATIONS=false`.

### Seed dei Dati

- **Development**: Seed abilitato di default
- **Production**: Seed disabilitato di default (per sicurezza)

Per abilitare il seed in produzione: `SEED_DATABASE=true`

---

## 8. Troubleshooting

### 8.1 Controllare i Log

Per vedere i log di un App Service:
```bash
az webapp log tail --name <app-service-name> --resource-group <resource-group>
```

Oppure via Azure Portal:
1. App Service → Monitoring → Log stream

### 8.2 Verificare Configurazione Frontend

L'Admin App espone un endpoint di debug:
```
GET /api-debug/config
```

Restituisce:
```json
{
  "apiUrl": "https://appointment-scheduler-api.azurewebsites.net",
  "nodeEnv": "production",
  "port": "8080",
  "timestamp": "2026-01-09T14:00:00.000Z"
}
```

### 8.3 Problemi Comuni

#### Build Failure: "npx tsc installs wrong package"
**Soluzione**: Usare `tsc` direttamente invece di `npx tsc` nel build script.

#### Backend non riceve richieste API
**Verificare**:
1. CORS configurato correttamente con gli URL dei frontend
2. API_URL impostato correttamente nei frontend App Services
3. Backend è raggiungibile (`GET /health`)

#### Frontend mostra "502 Bad Gateway"
**Causa**: Express server non è partito correttamente
**Verificare**:
1. Startup command è `npm start`
2. `node_modules` è stato deployato
3. `server.js` esiste nella root del package deployato

---

## 9. Costi Stimati

### App Services (4x)
- 1x Backend API (Basic B1): ~13€/mese
- 3x Frontend Apps (Basic B1): ~39€/mese

### Database
- Azure Database for PostgreSQL (Burstable B1ms): ~15€/mese

**Totale stimato**: ~67€/mese

### Ottimizzazioni Possibili
- Usare App Service Plan condiviso per tutti i frontend (riduzione a ~26€/mese totale frontend)
- Scalare il tier in base al traffico reale

---

## 10. Prossimi Passi e Miglioramenti

1. **Configurare Application Insights** per monitoring avanzato
2. **Setup Azure CDN** per servire assets statici più velocemente
3. **Implementare caching** (Redis Cache) per migliorare performance
4. **Setup staging slots** per deploy blue/green
5. **Configurare auto-scaling** in base al carico
6. **Implementare backup automatici** del database
7. **Setup alerting** per downtime e errori critici

---

## Riepilogo Architettura

```
┌──────────────────────────────────────────────────────────────┐
│                      Internet (HTTPS)                         │
└────────────┬─────────────┬─────────────┬─────────────────────┘
             │             │             │
             │             │             │
    ┌────────▼────────┐   │   ┌─────────▼────────┐
    │  Consumer App   │   │   │   Merchant App   │
    │  (Node.js 20)   │   │   │   (Node.js 20)   │
    │  Azure App Svc  │   │   │   Azure App Svc  │
    └────────┬────────┘   │   └─────────┬────────┘
             │         ┌───▼───┐        │
             │         │ Admin │        │
             │         │  App  │        │
             │         │(Node) │        │
             │         └───┬───┘        │
             │             │            │
             └─────────────┼────────────┘
                           │
                  Proxy /api/* requests
                           │
                     ┌─────▼─────┐
                     │  Backend  │
                     │    API    │
                     │ (.NET 8)  │
                     │   Azure   │
                     │ App Svc   │
                     └─────┬─────┘
                           │
                     ┌─────▼─────┐
                     │PostgreSQL │
                     │  Database │
                     │   Azure   │
                     └───────────┘
```

**Ogni frontend è un App Service separato** che:
- Serve una React SPA builddata con Vite
- Usa Express.js per proxy API e servire file statici
- Si connette al backend API tramite proxy middleware

**Il backend** è un singolo App Service che:
- Espone REST API con ASP.NET Core
- Gestisce autenticazione JWT e autorizzazione
- Si connette a PostgreSQL per persistenza dati
