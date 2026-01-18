# 🔴 BACKEND ATTIVO MA FRONTEND NON SI CONNETTE

## Situazione Attuale

✅ **Backend**: In esecuzione su `http://localhost:5000`
✅ **CORS**: Configurato correttamente
❌ **Problema**: Frontend non raggiunge il backend (status: undefined)

Il backend **non riceve nemmeno la chiamata** → problema di configurazione frontend.

---

## 🔍 Causa Probabile

Il frontend è stato **buildato in modalità PRODUZIONE** invece che in modalità **SVILUPPO**.

Quando il frontend è in produzione, cerca di chiamare direttamente:
```
https://appointment-scheduler-api.azurewebsites.net/api/auth/register
```

Invece che usare il proxy Vite locale:
```
http://localhost:5174/api → proxy → http://localhost:5000/api
```

---

## ✅ SOLUZIONE

### Step 1: Ferma il Frontend

Nel terminale dove hai avviato il frontend, premi:
```
Ctrl + C
```

### Step 2: Verifica di Essere nella Cartella Corretta

```bash
cd C:\Repos\appointment-scheduler\frontend\merchant-app
```

### Step 3: Avvia in Modalità SVILUPPO

**IMPORTANTE: Usa `npm run dev` NON `npm run build` o `npm start`**

```bash
npm run dev
```

### Step 4: Verifica Output

Dovresti vedere:
```
VITE v6.0.3  ready in XXX ms

➜  Local:   http://localhost:5174/
➜  Network: use --host to expose
➜  press h + enter to show help
```

### Step 5: Apri Browser

Vai su: `http://localhost:5174`

**NON usare**:
- ❌ `https://appointment-merchant-app.azurewebsites.net`
- ❌ `http://localhost:5174` se hai buildato prima

---

## 🔧 Se il Problema Persiste

### Verifica 1: Cancella Build Precedente

```bash
# Nella cartella frontend/merchant-app
rmdir /s /q dist
npm run dev
```

### Verifica 2: Cancella Cache Browser

1. Apri DevTools (F12)
2. Tab "Application" → "Storage" → "Clear site data"
3. Ricarica pagina (Ctrl + Shift + R)

### Verifica 3: Controlla Console DevTools

Nel browser, apri DevTools (F12) e verifica:

**Network Tab:**
- La richiesta a `/api/auth/register` dovrebbe andare a `http://localhost:5174/api/auth/register`
- NON deve andare a `https://appointment-scheduler-api.azurewebsites.net`

**Console Tab:**
- Controlla se ci sono errori diversi

---

## 📋 Checklist Completa

Prima di testare la registrazione:

- [ ] Backend in esecuzione (`dotnet run`)
  - Vedi: "Now listening on: http://localhost:5000" ✅

- [ ] Frontend in modalità DEV (`npm run dev`)
  - Vedi: "Local: http://localhost:5174" ✅

- [ ] Browser su `http://localhost:5174` (NON https)

- [ ] Cache browser cancellata

- [ ] DevTools Network tab aperta per verificare chiamate

---

## 🎯 Test Rapido Proxy

Prima di testare la registrazione, verifica che il proxy funzioni:

1. Apri DevTools (F12)
2. Vai sulla Console
3. Incolla questo comando:

```javascript
fetch('/api/version').then(r => r.json()).then(console.log)
```

**Risposta attesa:**
```json
{version: "1.0.0", buildTime: "..."}
```

Se vedi questo, il proxy funziona! ✅

Se vedi errore CORS o 404, il proxy NON funziona ❌

---

## 📊 Debug Avanzato

### Verifica Modalità Build

Nel browser, apri Console e digita:
```javascript
import.meta.env.MODE
```

**Deve dire:** `"development"`

Se dice `"production"`, hai buildato il frontend in produzione.

### Verifica BaseURL Axios

Console:
```javascript
// Guarda la prima richiesta fallita nel Network tab
// Controlla l'URL completo
```

Deve essere:
- ✅ `http://localhost:5174/api/auth/register`

NON deve essere:
- ❌ `https://appointment-scheduler-api.azurewebsites.net/api/auth/register`

---

## 🚀 Comandi Corretti

### Setup Completo (solo la prima volta):

```bash
cd C:\Repos\appointment-scheduler\frontend\merchant-app
npm install
```

### Ogni Volta (2 Terminali):

**Terminale 1 - Backend:**
```bash
cd C:\Repos\appointment-scheduler\backend
dotnet run --project AppointmentScheduler.API
```

**Terminale 2 - Frontend:**
```bash
cd C:\Repos\appointment-scheduler\frontend\merchant-app
npm run dev
```

**NON usare:**
- ❌ `npm run build` (crea build di produzione)
- ❌ `npm start` (avvia server di produzione)
- ❌ `npm run preview` (preview build di produzione)

---

## 💡 Differenza DEV vs PROD

### Modalità SVILUPPO (`npm run dev`):
- Usa Vite dev server
- Proxy attivo: `/api` → `http://localhost:5000`
- Hot reload funziona
- Source maps disponibili

### Modalità PRODUZIONE (`npm run build`):
- Crea file statici in `dist/`
- Proxy NON disponibile
- Chiama direttamente Azure: `https://appointment-scheduler-api.azurewebsites.net`
- Richiede deploy per testare

**Per sviluppo locale, usa SEMPRE `npm run dev`!**

---

*Ultimo aggiornamento: 2026-01-18*
