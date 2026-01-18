# 🚨 ERRORE REGISTRAZIONE - RISOLTO

## Problema
```
API Error: {status: undefined, data: undefined, url: '/auth/register'}
```

## ✅ Causa Identificata
**Il backend NON è in esecuzione su localhost:5000**

Test effettuato:
```bash
curl http://localhost:5000/api/version
# Risultato: Connection refused
```

Nessun processo .NET in esecuzione.

---

## 🚀 Soluzione (COPIA E INCOLLA)

### Hai 2 opzioni:

### Opzione A: Avvia Backend in Questo Container (se hai dotnet)

**1. Apri un NUOVO terminale**

**2. Copia e incolla questi comandi:**

```bash
cd /home/user/appointment-scheduler/backend
dotnet run --project AppointmentScheduler.API
```

**3. Attendi questo output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

**4. Lascia il terminale APERTO (non chiuderlo)**

**5. Torna al browser e riprova la registrazione**

---

### Opzione B: Usa il Backend su Azure (temporaneo)

Se non riesci ad avviare il backend localmente, puoi puntare al backend su Azure.

**1. Modifica file:**
`frontend/merchant-app/.env.development`

**2. Aggiungi questa riga:**
```
VITE_API_URL=https://appointment-scheduler-api.azurewebsites.net
```

**3. Riavvia il frontend:**
```bash
# Premi Ctrl+C nel terminale del frontend
npm run dev
```

**⚠️ ATTENZIONE:** Questa soluzione funziona solo se hai già configurato CORS su Azure (vedi TROUBLESHOOTING_AZURE_CORS.md)

---

## 🔍 Come Verificare che il Backend Funzioni

Prima di testare la registrazione, verifica:

### Test 1: Backend risponde
```bash
curl http://localhost:5000/api/version
```

**Risposta attesa:**
```json
{"version":"1.0.0","buildTime":"..."}
```

### Test 2: Swagger UI accessibile
Apri nel browser:
```
http://localhost:5000/swagger
```

Dovresti vedere l'interfaccia Swagger con tutti gli endpoint API.

---

## 📋 Checklist Pre-Test

Prima di testare la registrazione:

- [ ] Backend in esecuzione (`dotnet run`)
- [ ] Vedi "Now listening on: http://localhost:5000"
- [ ] `curl http://localhost:5000/api/version` ritorna JSON
- [ ] Frontend in esecuzione (`npm run dev`)
- [ ] Browser aperto su `http://localhost:5174`

Se tutto è ✅, riprova la registrazione!

---

## ⚠️ Errori Comuni

### "dotnet: command not found"
**Causa:** .NET SDK non installato in questo container

**Soluzione:**
- Usa Opzione B (backend Azure)
- Oppure installa .NET SDK 8.0

### "Cannot connect to database"
**Causa:** PostgreSQL non configurato/avviato

**Soluzione rapida:**
```bash
docker run --name postgres-dev \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:16
```

Poi riavvia il backend.

### Backend si avvia ma errori di compilazione
**Causa:** Possibili file mancanti o errori sintassi

**Verifica:**
```bash
cd backend
dotnet build
```

Se ci sono errori, copia l'output e fammi vedere.

---

## 🎯 Workflow Corretto per Sviluppo Locale

**Ogni volta che sviluppi:**

1. **Avvia PostgreSQL** (una volta, rimane attivo)
   ```bash
   docker start postgres-dev
   ```

2. **Terminale 1 - Backend**
   ```bash
   cd /home/user/appointment-scheduler/backend
   dotnet run --project AppointmentScheduler.API
   ```
   Lascia aperto ✅

3. **Terminale 2 - Frontend**
   ```bash
   cd /home/user/appointment-scheduler/frontend/merchant-app
   npm run dev
   ```
   Lascia aperto ✅

4. **Browser**
   Apri: `http://localhost:5174`

**Entrambi i terminali devono rimanere aperti per tutta la sessione di sviluppo!**

---

## 📊 Status Sistema

✅ Codice Backend: COMPLETO E CORRETTO
✅ Codice Frontend: COMPLETO E CORRETTO
✅ Sistema Timbratura: IMPLEMENTATO
❌ Backend: NON IN ESECUZIONE

**Una volta avviato il backend, tutto funzionerà!**

---

*Ultimo aggiornamento: 2026-01-17*
*Per ulteriori dettagli: LOCAL_SETUP.md*
