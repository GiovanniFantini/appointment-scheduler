# Guida al Setup dell'Ambiente di Sviluppo

Questa guida ti aiuterà a configurare l'ambiente di sviluppo locale per il progetto Appointment Scheduler dopo le modifiche ai settings hardcodati.

## Indice

1. [Prerequisiti](#prerequisiti)
2. [Configurazione Backend](#configurazione-backend)
3. [Configurazione Frontend](#configurazione-frontend)
4. [Variabili d'Ambiente Richieste](#variabili-dambiente-richieste)
5. [Avvio dell'Applicazione](#avvio-dellapplicazione)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisiti

Assicurati di avere installato:

- **.NET 8.0 SDK** o superiore
- **Node.js 18+** e **npm**
- **PostgreSQL 14+** (locale o remoto)
- **Git**
- Un editor di codice (consigliato: Visual Studio Code)

### Verifica Versioni

```bash
# .NET
dotnet --version
# Output: 8.0.x o superiore

# Node.js
node --version
# Output: v18.x.x o superiore

# PostgreSQL
psql --version
# Output: psql (PostgreSQL) 14.x o superiore
```

---

## Configurazione Backend

### Passo 1: Creare il Database PostgreSQL

```bash
# Connettiti a PostgreSQL
psql -U postgres

# Crea il database
CREATE DATABASE "AppointmentScheduler";

# Crea un utente (opzionale, se non vuoi usare postgres)
CREATE USER appointmentuser WITH PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE "AppointmentScheduler" TO appointmentuser;

# Esci
\q
```

### Passo 2: Configurare le Variabili d'Ambiente

1. Naviga nella cartella backend:
   ```bash
   cd backend/AppointmentScheduler.API
   ```

2. Copia il file `.env.example` in `.env`:
   ```bash
   cp .env.example .env
   ```

3. Modifica il file `.env` con i tuoi valori:
   ```bash
   # Usa il tuo editor preferito
   nano .env
   # oppure
   code .env
   ```

4. **Configurazione minima richiesta**:
   ```env
   # Database
   POSTGRESQLCONNSTR_DefaultConnection=Host=localhost;Port=5432;Database=AppointmentScheduler;Username=postgres;Password=yourpassword

   # JWT (genera una chiave sicura!)
   JWT_SECRET_KEY=your-super-secret-key-minimum-32-characters-change-this

   # Admin Default (per sviluppo locale)
   ADMIN_DEFAULT_EMAIL=admin@localhost.dev
   ADMIN_DEFAULT_PASSWORD=DevPassword123!
   ADMIN_DEFAULT_FIRSTNAME=Admin
   ADMIN_DEFAULT_LASTNAME=Dev

   # CORS (per sviluppo locale)
   CORS_ORIGINS=http://localhost:5173,http://localhost:5174,http://localhost:5175
   ```

### Passo 3: Generare una Chiave JWT Sicura

```bash
# Usando OpenSSL (consigliato)
openssl rand -base64 32

# Copia l'output e incollalo come valore di JWT_SECRET_KEY nel file .env
```

### Passo 4: Applicare le Migrations

```bash
# Dalla cartella backend/AppointmentScheduler.API
dotnet ef database update

# Oppure, se preferisci, l'applicazione lo farà automaticamente al primo avvio
```

### Passo 5: Testare il Backend

```bash
# Dalla cartella backend/AppointmentScheduler.API
dotnet run

# Dovresti vedere:
# "Now listening on: http://localhost:5000"
# "Now listening on: https://localhost:5001"
```

Apri il browser su: `http://localhost:5000/swagger` per vedere l'API documentation.

---

## Configurazione Frontend

### Passo 1: Installare le Dipendenze

Per ogni app frontend (consumer, merchant, admin):

```bash
# Consumer App
cd frontend/consumer-app
npm install

# Merchant App
cd ../merchant-app
npm install

# Admin App
cd ../admin-app
npm install
```

### Passo 2: Configurare le Variabili d'Ambiente

Per ogni app, crea un file `.env`:

#### Consumer App

```bash
cd frontend/consumer-app
cp .env.example .env
```

Contenuto `.env`:
```env
# API Configuration
VITE_API_URL=http://localhost:5000
VITE_API_BASE_PATH=/api
VITE_API_TIMEOUT=5000

# Dev Server
VITE_PORT=5173

# Features
VITE_ENABLE_API_DEBUG=true
VITE_ENABLE_DEBUG_LOGS=false
```

#### Merchant App

```bash
cd frontend/merchant-app
cp .env.example .env
```

Contenuto `.env`:
```env
# API Configuration
VITE_API_URL=http://localhost:5000
VITE_API_BASE_PATH=/api
VITE_API_TIMEOUT=5000

# Dev Server
VITE_PORT=5174

# Service Configuration
VITE_DEFAULT_SERVICE_DURATION=60
VITE_MIN_SERVICE_CAPACITY=1
VITE_MAX_SERVICE_CAPACITY=1000

# Features
VITE_ENABLE_API_DEBUG=true
```

#### Admin App

```bash
cd frontend/admin-app
cp .env.example .env
```

Contenuto `.env`:
```env
# API Configuration
VITE_API_URL=http://localhost:5000
VITE_API_BASE_PATH=/api
VITE_API_TIMEOUT=5000

# Dev Server
VITE_PORT=5175

# Features
VITE_ENABLE_API_DEBUG=true
```

---

## Variabili d'Ambiente Richieste

### Backend (obbligatorie)

| Variabile | Descrizione | Esempio |
|-----------|-------------|---------|
| `POSTGRESQLCONNSTR_DefaultConnection` | Connection string PostgreSQL | `Host=localhost;Port=5432;Database=AppointmentScheduler;Username=postgres;Password=yourpassword` |
| `JWT_SECRET_KEY` | Chiave segreta JWT (min 32 char) | Generata con `openssl rand -base64 32` |
| `ADMIN_DEFAULT_EMAIL` | Email admin di default | `admin@localhost.dev` |
| `ADMIN_DEFAULT_PASSWORD` | Password admin di default | `DevPassword123!` |
| `CORS_ORIGINS` | Origins permessi (separati da virgola) | `http://localhost:5173,http://localhost:5174,http://localhost:5175` |

### Backend (opzionali con valori di default)

| Variabile | Default | Descrizione |
|-----------|---------|-------------|
| `JWT_ISSUER` | `AppointmentScheduler.API` | Issuer JWT |
| `JWT_AUDIENCE` | `AppointmentScheduler.Client` | Audience JWT |
| `JWT_EXPIRATION_MINUTES` | `1440` | Durata token (minuti) |
| `ADMIN_DEFAULT_FIRSTNAME` | `Admin` | Nome admin |
| `ADMIN_DEFAULT_LASTNAME` | `User` | Cognome admin |

### Frontend (obbligatorie)

| Variabile | Descrizione | Valore Dev |
|-----------|-------------|------------|
| `VITE_API_URL` | URL API backend | `http://localhost:5000` |

### Frontend (opzionali)

| Variabile | Default | Descrizione |
|-----------|---------|-------------|
| `VITE_API_BASE_PATH` | `/api` | Base path API |
| `VITE_API_TIMEOUT` | `5000` | Timeout richieste (ms) |
| `VITE_PORT` | `5173/5174/5175` | Porta dev server |
| `VITE_ENABLE_API_DEBUG` | `true` | Abilita console debug |

---

## Avvio dell'Applicazione

### Opzione 1: Avvio Manuale (consigliato per debug)

Apri 4 terminali separati:

**Terminal 1 - Backend:**
```bash
cd backend/AppointmentScheduler.API
dotnet run
# Attendi: "Now listening on: http://localhost:5000"
```

**Terminal 2 - Consumer App:**
```bash
cd frontend/consumer-app
npm run dev
# Attendi: "Local: http://localhost:5173"
```

**Terminal 3 - Merchant App:**
```bash
cd frontend/merchant-app
npm run dev
# Attendi: "Local: http://localhost:5174"
```

**Terminal 4 - Admin App:**
```bash
cd frontend/admin-app
npm run dev
# Attendi: "Local: http://localhost:5175"
```

### Opzione 2: Usando script (se configurato)

Se hai script di avvio:

```bash
# Dalla root del progetto
./start-dev.sh
# oppure
npm run dev:all
```

### Accesso alle Applicazioni

- **Backend API (Swagger)**: http://localhost:5000/swagger
- **Consumer App**: http://localhost:5173
- **Merchant App**: http://localhost:5174
- **Admin App**: http://localhost:5175

### Credenziali di Default

Dopo il primo avvio, usa le credenziali configurate nel file `.env`:

- **Email**: `admin@localhost.dev` (o quello che hai configurato)
- **Password**: `DevPassword123!` (o quello che hai configurato)

**⚠️ IMPORTANTE**: Cambia queste credenziali dopo il primo accesso!

---

## Troubleshooting

### Problema: "Database connection failed"

**Causa**: PostgreSQL non è in esecuzione o le credenziali sono errate.

**Soluzione**:
```bash
# Verifica che PostgreSQL sia attivo
sudo systemctl status postgresql
# oppure su macOS
brew services list | grep postgresql

# Avvia PostgreSQL se non è attivo
sudo systemctl start postgresql
# oppure su macOS
brew services start postgresql

# Verifica le credenziali
psql -U postgres -d AppointmentScheduler
```

### Problema: "JWT SecretKey not configured"

**Causa**: La variabile `JWT_SECRET_KEY` non è impostata nel file `.env`.

**Soluzione**:
```bash
# Genera una chiave
openssl rand -base64 32

# Aggiungila al file .env
echo "JWT_SECRET_KEY=la_tua_chiave_generata" >> backend/AppointmentScheduler.API/.env
```

### Problema: "CORS blocking requests"

**Causa**: L'origin del frontend non è incluso in `CORS_ORIGINS`.

**Soluzione**:
```env
# Nel file backend/.env, assicurati di avere:
CORS_ORIGINS=http://localhost:5173,http://localhost:5174,http://localhost:5175
```

Riavvia il backend dopo la modifica.

### Problema: "Port already in use"

**Causa**: La porta è già occupata da un altro processo.

**Soluzione**:
```bash
# Trova il processo che usa la porta (esempio: 5000)
lsof -i :5000
# oppure su Windows
netstat -ano | findstr :5000

# Termina il processo
kill -9 <PID>
# oppure su Windows
taskkill /PID <PID> /F

# Oppure cambia la porta nel file .env
VITE_PORT=5176  # per frontend
ASPNETCORE_URLS=http://localhost:5001  # per backend
```

### Problema: "Module not found" nel frontend

**Causa**: Le dipendenze non sono installate o il file `constants/index.ts` non esiste.

**Soluzione**:
```bash
# Reinstalla le dipendenze
cd frontend/consumer-app  # o merchant-app / admin-app
rm -rf node_modules package-lock.json
npm install

# Verifica che esista il file constants
ls -la src/constants/index.ts
```

### Problema: "Migration failed"

**Causa**: Il database non è accessibile o ci sono migrazioni conflittuali.

**Soluzione**:
```bash
# Resetta il database (ATTENZIONE: cancella tutti i dati!)
cd backend/AppointmentScheduler.API
dotnet ef database drop
dotnet ef database update

# Oppure crea manualmente il database
psql -U postgres
CREATE DATABASE "AppointmentScheduler";
\q
```

### Problema: "Cannot find constants module"

**Causa**: Il file TypeScript delle costanti non è stato compilato.

**Soluzione**:
```bash
# Riavvia il dev server
cd frontend/consumer-app  # o merchant-app / admin-app
npm run dev
```

---

## Best Practices per lo Sviluppo

### 1. File .env

- ✅ **Usa file `.env` diversi** per dev/staging/production
- ✅ **Non committare mai** file `.env` nel repository
- ✅ **Documenta tutte le variabili** in `.env.example`
- ✅ **Usa valori sicuri** anche in sviluppo

### 2. Database

- ✅ **Fai backup regolari** del database di sviluppo
- ✅ **Usa migrazioni** per tutte le modifiche allo schema
- ✅ **Resetta il database** periodicamente per testare le migrazioni

### 3. Secrets e Credenziali

- ✅ **Usa password manager** per salvare le credenziali
- ✅ **Cambia le password** dopo il primo accesso
- ✅ **Non condividere** secrets via chat o email
- ✅ **Ruota i secrets** regolarmente

### 4. Testing

Prima di committare:
```bash
# Test backend
cd backend/AppointmentScheduler.API
dotnet build
dotnet test

# Test frontend
cd frontend/consumer-app
npm run build
npm run lint
```

---

## Prossimi Passi

Dopo aver configurato l'ambiente di sviluppo:

1. ✅ Leggi la documentazione API su `/swagger`
2. ✅ Familiarizza con le costanti in `Constants/AppConstants.cs` e `constants/index.ts`
3. ✅ Consulta `GITHUB_SECRETS_SETUP.md` per il deployment
4. ✅ Configura i GitHub Secrets per CI/CD

---

## Risorse Utili

- [.NET Environment Variables](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Vite Environment Variables](https://vitejs.dev/guide/env-and-mode.html)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

---

## Supporto

Per problemi o domande:
1. Controlla la sezione [Troubleshooting](#troubleshooting)
2. Verifica i logs dell'applicazione
3. Consulta i file `.env.example` per la configurazione corretta
4. Apri un issue su GitHub se il problema persiste
