# Troubleshooting Guide

Soluzioni ai problemi comuni di sviluppo e deployment.

## Sviluppo Locale

### Backend non in esecuzione

**Problema:** `API Error: {status: undefined, data: undefined}`

**Causa:** Backend non avviato su localhost:5000

**Soluzione:**
```bash
# Verifica backend
curl http://localhost:5000/api/version

# Se non risponde, avvia backend
cd backend
dotnet run --project AppointmentScheduler.API

# Attendi output:
# Now listening on: http://localhost:5000
```

**Checklist:**
- [ ] Backend in esecuzione
- [ ] PostgreSQL avviato
- [ ] `curl http://localhost:5000/api/version` ritorna JSON
- [ ] Swagger accessibile su `http://localhost:5000/swagger`

### Frontend in modalità produzione

**Problema:** Frontend chiama Azure invece di localhost

**Causa:** Usato `npm run build` invece di `npm run dev`

**Soluzione:**
```bash
# Ferma frontend (Ctrl+C)
# Cancella build precedente
rm -rf dist

# Avvia in modalità sviluppo
npm run dev
```

**Verifica modalità:**
```javascript
// Browser console
import.meta.env.MODE
// Deve dire: "development"
```

**Comandi corretti:**
- ✅ `npm run dev` - Modalità sviluppo (proxy attivo)
- ❌ `npm run build` - Crea build produzione
- ❌ `npm start` - Avvia server produzione
- ❌ `npm run preview` - Preview build produzione

### Database errori

**Reset completo database:**
```bash
cd backend/AppointmentScheduler.API
dotnet ef database drop -f
dotnet ef database update
```

**Migration non applicata:**
```bash
dotnet ef database update --startup-project AppointmentScheduler.API --project AppointmentScheduler.Data
```

**PostgreSQL non in esecuzione:**
```bash
# Docker
docker start appointment-db

# Verifica
docker ps | grep postgres

# Se non esiste, crea nuovo container
docker run --name appointment-db \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:16
```

### Proxy Vite non funziona

**Test proxy:**
```javascript
// Browser console (frontend su localhost:5174)
fetch('/api/version').then(r => r.json()).then(console.log)
// Deve ritornare: {version: "...", buildTime: "..."}
```

**Se fallisce:**
1. Verifica backend su porta 5000
2. Controlla `vite.config.ts` proxy settings:
```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5000',
    changeOrigin: true
  }
}
```
3. Riavvia frontend: `Ctrl+C` → `npm run dev`

## Produzione Azure

### CORS errori

**Problema:** Frontend non raggiunge backend in Azure

**Causa:** CORS non configurato in Azure App Service

**Soluzione:**
1. Azure Portal → App Service (backend)
2. Configuration → Application Settings
3. Aggiungi:
   ```
   CorsOrigins__0 = https://appointment-merchant-app.azurewebsites.net
   CorsOrigins__1 = https://appointment-employee-app.azurewebsites.net
   CorsOrigins__2 = https://appointment-consumer-app.azurewebsites.net
   ```
4. Save → Restart app

**Verifica CORS:**
```bash
# Controlla response headers
curl -I https://appointment-scheduler-api.azurewebsites.net/api/version

# Deve includere:
# Access-Control-Allow-Origin: https://...
```

**Soluzione temporanea (solo test):**
```csharp
// Program.cs - NON per produzione!
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()  // SOLO PER TEST
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Migration database Azure

**Problema:** Tabelle mancanti dopo deploy

**Soluzione:**
```bash
# Locale: crea migration
cd backend
dotnet ef migrations add NomeMigrazione --startup-project AppointmentScheduler.API

# Push su Git
git add .
git commit -m "feat: add database migration"
git push

# Azure applicherà automaticamente se RUN_MIGRATIONS=true
```

**Verifica setting Azure:**
- App Service → Configuration
- `RUN_MIGRATIONS = true`

### Build frontend fallisce

**Problema:** `npx tsc installs wrong package`

**Soluzione:**
Usa `tsc` direttamente (non `npx tsc`):
```yaml
# .github/workflows/deploy-*.yml
- name: Build
  run: |
    npm ci
    tsc        # NON npx tsc
    npm run build
```

## Errori Comuni

### "dotnet: command not found"

**Causa:** .NET SDK non installato

**Soluzione:**
- Installa .NET 8 SDK da https://dotnet.microsoft.com/download
- Oppure usa backend Azure (vedi SETUP.md)

### "Cannot connect to database"

**Locale:**
```bash
# Verifica PostgreSQL
docker ps | grep postgres

# Se non in esecuzione
docker start appointment-db
```

**Azure:**
- Verifica connection string in Configuration
- Connection string format:
```
Server=<server>.postgres.database.azure.com;Database=<dbname>;Port=5432;User Id=<username>;Password=<password>;Ssl Mode=Require;
```

### "JWT SecretKey not configured"

**Locale:**
- Verifica `appsettings.Local.json` esista
- SecretKey deve essere ≥32 caratteri

**Azure:**
- Configuration → Application Settings
- `JwtSettings__SecretKey = your-secret-key-at-least-32-characters`

### withCredentials mismatch

**Problema:** Errore CORS con credenziali

**Soluzione A - Backend:**
```csharp
// Rimuovi .AllowCredentials() se frontend usa withCredentials: false
policy.WithOrigins(corsOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod();
      // .AllowCredentials();  // ← Rimuovi questa linea
```

**Soluzione B - Frontend:**
```typescript
// axios.ts - Imposta withCredentials: true
const api = axios.create({
  withCredentials: true  // ← Cambia da false a true
});
```

## Logging e Debug

### Backend logs

**Locale:**
```bash
# Output console mentre dotnet run è attivo
```

**Azure:**
```bash
# Azure CLI
az webapp log tail --name <app-name> --resource-group <resource-group>

# Oppure Azure Portal
App Service → Monitoring → Log stream
```

### Frontend logs

**Browser:**
- F12 → Console
- F12 → Network (verifica chiamate API)

**Azure:**
```bash
# Log stream
az webapp log tail --name appointment-merchant-app --resource-group <rg>
```

## Checklist Pre-Test

### Sviluppo Locale

- [ ] PostgreSQL in esecuzione (`docker ps`)
- [ ] Backend avviato (`dotnet run`)
- [ ] Vedi "Now listening on: http://localhost:5000"
- [ ] Frontend avviato (`npm run dev`)
- [ ] Browser su `http://localhost:5174` (HTTP, non HTTPS)
- [ ] Cache browser cancellata (Ctrl+Shift+R)

### Deployment Azure

- [ ] CORS configurato (CorsOrigins__*)
- [ ] Migration database applicata
- [ ] RUN_MIGRATIONS=true in settings
- [ ] JWT SecretKey configurato
- [ ] Connection string corretta
- [ ] App Service riavviato dopo config changes

## Riferimenti

- Setup locale completo: [SETUP.md](SETUP.md)
- Configurazione backend: [backend/CONFIGURATION.md](backend/CONFIGURATION.md)
- Architettura produzione: [PRODUCTION_ARCHITECTURE.md](PRODUCTION_ARCHITECTURE.md)
- Guidelines progetto: [PROJECT_GUIDELINES.md](PROJECT_GUIDELINES.md)
