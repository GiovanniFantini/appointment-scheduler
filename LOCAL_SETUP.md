# 🚀 Setup Locale - Guida Rapida

## Problema Riscontrato

**Errore:** `status: undefined, data: undefined` in `/auth/register`

**Causa:** Backend non in esecuzione su localhost:5000

## ✅ Soluzione Rapida

### 1. Avvia il Backend (.NET 8)

```bash
cd backend
dotnet restore
dotnet run --project AppointmentScheduler.API
```

Il backend partirà su:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

**Output atteso:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### 2. Avvia il Frontend Merchant App

In un **nuovo terminale**:

```bash
cd frontend/merchant-app
npm install  # solo la prima volta
npm run dev
```

Il frontend partirà su: `http://localhost:5174`

### 3. Avvia il Frontend Employee App (opzionale)

In un **altro terminale**:

```bash
cd frontend/employee-app
npm install  # solo la prima volta
npm run dev
```

Il frontend partirà su: `http://localhost:5173`

---

## 🔧 Troubleshooting

### Problema: Backend non compila

**Causa possibile:** Migration non applicate

**Soluzione:**
```bash
cd backend
dotnet ef database update --startup-project AppointmentScheduler.API --project AppointmentScheduler.Data
```

### Problema: Errore "Cannot connect to PostgreSQL"

**Causa:** Database PostgreSQL non configurato/avviato

**Soluzione 1 - Docker (RACCOMANDATO):**
```bash
docker run --name postgres-dev -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16
```

**Soluzione 2 - Configura Connection String:**

Crea `backend/AppointmentScheduler.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=appointment_scheduler_dev;Username=postgres;Password=postgres"
  }
}
```

### Problema: Backend compila ma Swagger non si apre

**Causa:** Porta 5000 già in uso

**Soluzione:**
Cambia porta in `backend/AppointmentScheduler.API/Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:5011;http://localhost:5010"
```

E aggiorna il proxy in `frontend/merchant-app/vite.config.ts`:
```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5010',  // Nuova porta
    changeOrigin: true
  }
}
```

---

## 📊 Verifica Sistema Timbratura

Una volta avviato il backend, verifica che gli endpoint funzionino:

### Test API (usando curl o browser):

```bash
# Test backend vivo
curl http://localhost:5000/api/version

# Test health check
curl http://localhost:5000/health

# Test Swagger UI (browser)
open http://localhost:5000/swagger
```

### Test Sistema Timbratura:

Gli endpoint dovrebbero essere visibili in Swagger:
- `POST /api/timbrature/check-in`
- `POST /api/timbrature/check-out`
- `GET /api/timbrature/status`
- `GET /api/timbrature/wellbeing`
- E altri 8 endpoint...

---

## 🎯 Workflow Completo Sviluppo

### Setup Iniziale (una volta):

```bash
# 1. Backend - Restore packages
cd backend
dotnet restore

# 2. Backend - Applica migration
dotnet ef database update --startup-project AppointmentScheduler.API

# 3. Frontend Merchant - Install dependencies
cd ../frontend/merchant-app
npm install

# 4. Frontend Employee - Install dependencies
cd ../employee-app
npm install
```

### Ogni Giorno (3 terminali):

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

### Accesso:
- Merchant App: `http://localhost:5174`
- Employee App: `http://localhost:5173`
- API Swagger: `http://localhost:5000/swagger`

---

## ✅ Checklist Pre-Test Sistema Timbratura

Prima di testare il sistema di timbratura intelligente:

- [ ] Backend in esecuzione (porta 5000)
- [ ] Migration applicata al database
- [ ] PostgreSQL in esecuzione
- [ ] Frontend merchant-app in esecuzione (porta 5174)
- [ ] Frontend employee-app in esecuzione (porta 5173)
- [ ] Utente merchant creato e loggato
- [ ] Almeno 1 employee creato
- [ ] Almeno 1 turno (shift) creato per l'employee

### Per testare Timbratura:

1. **Login come Employee** su `http://localhost:5173`
2. Naviga a `/timbratura`
3. Dovresti vedere:
   - Orologio live
   - Pulsante ENTRA/ESCI verde/rosso
   - Stato corrente
   - Dashboard ore lavorate

4. **Login come Merchant** su `http://localhost:5174`
5. Naviga a `/timbrature`
6. Dovresti vedere:
   - Tabella turni da validare
   - Pulsante auto-validazione
   - Approvazione batch
   - Statistiche

---

## 🐛 Errori Comuni e Soluzioni

### ❌ "status: undefined, data: undefined"
→ **Backend non in esecuzione**
→ Soluzione: Avvia backend con `dotnet run`

### ❌ "CORS policy error"
→ **CORS non configurato per localhost**
→ Soluzione: Dovrebbe funzionare con proxy Vite

### ❌ "Cannot connect to database"
→ **PostgreSQL non avviato**
→ Soluzione: `docker run postgres` o installa PostgreSQL

### ❌ "Migration not applied"
→ **Database non aggiornato**
→ Soluzione: `dotnet ef database update`

### ❌ "TimbratureController not found"
→ **Codice non compilato/registrato**
→ Soluzione: Verifica Program.cs ha `AddScoped<ITimbratureService>`

---

## 📝 Note

- **CORS** in locale funziona tramite proxy Vite (non serve configurazione)
- **HTTPS** in locale può dare warning certificato (normale in dev)
- **Hot Reload** funziona sia backend (.NET) che frontend (Vite)
- **Database** viene creato automaticamente se non esiste
- **Migration** vengono applicate automaticamente se RUN_MIGRATIONS=true

---

*Ultimo aggiornamento: 2026-01-17*
*Sistema Timbratura Intelligente - Branch: claude/smart-timbratura-system-k4e2C*
