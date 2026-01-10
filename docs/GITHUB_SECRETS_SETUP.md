# Configurazione GitHub Secrets per CI/CD

Questa guida spiega come configurare correttamente i **GitHub Secrets** per garantire che le build e i deployment continuino a funzionare dopo la rimozione dei valori hardcodati dal codice.

## Indice

1. [Panoramica](#panoramica)
2. [Accesso a GitHub Secrets](#accesso-a-github-secrets)
3. [Secrets Richiesti per il Backend](#secrets-richiesti-per-il-backend)
4. [Secrets Richiesti per il Frontend](#secrets-richiesti-per-il-frontend)
5. [Configurazione per Azure Deployment](#configurazione-per-azure-deployment)
6. [Verifica della Configurazione](#verifica-della-configurazione)
7. [Troubleshooting](#troubleshooting)

---

## Panoramica

I **GitHub Secrets** sono variabili d'ambiente criptate che possono essere utilizzate nei workflow di GitHub Actions. Sono essenziali per:

- ✅ Mantenere le credenziali sensibili fuori dal codice
- ✅ Configurare diversi ambienti (dev, staging, production)
- ✅ Permettere il deployment automatico su Azure
- ✅ Garantire la sicurezza dell'applicazione

---

## Accesso a GitHub Secrets

### Passo 1: Navigare nelle Impostazioni del Repository

1. Vai al tuo repository GitHub: `https://github.com/GiovanniFantini/appointment-scheduler`
2. Clicca su **Settings** (Impostazioni)
3. Nel menu laterale, vai su **Secrets and variables** → **Actions**
4. Clicca su **New repository secret** per aggiungere un nuovo secret

### Passo 2: Aggiungere un Secret

Per ogni secret:
1. Inserisci il **Name** (nome esatto come indicato nelle sezioni seguenti)
2. Inserisci il **Value** (valore del secret)
3. Clicca su **Add secret**

---

## Secrets Richiesti per il Backend

### 🔴 CRITICI - Da configurare IMMEDIATAMENTE

Questi secrets sono **obbligatori** per il funzionamento dell'applicazione:

#### 1. `POSTGRESQLCONNSTR_DEFAULTCONNECTION`
- **Descrizione**: Connection string per il database PostgreSQL
- **Formato**: `Host=HOST;Port=5432;Database=DB_NAME;Username=USERNAME;Password=PASSWORD`
- **Esempio**:
  ```
  Host=my-postgres-server.postgres.database.azure.com;Port=5432;Database=AppointmentScheduler;Username=adminuser;Password=SuperSecurePassword123!
  ```
- **Dove trovarlo**:
  - Azure Portal → Database PostgreSQL → Connection strings
  - Oppure il tuo provider di database PostgreSQL

#### 2. `JWT_SECRET_KEY`
- **Descrizione**: Chiave segreta per la firma dei token JWT (minimo 32 caratteri)
- **Come generarla**:
  ```bash
  # Usando OpenSSL (consigliato):
  openssl rand -base64 32

  # Output esempio: k8jH3nR9mL2pQ5vT7wX1yA4bC6dE8fG0hI2jK4lM6n
  ```
- **⚠️ IMPORTANTE**:
  - Usa una chiave DIVERSA per ogni ambiente (dev, staging, production)
  - NON condividere questa chiave
  - Se compromessa, cambiala immediatamente

#### 3. `ADMIN_DEFAULT_EMAIL`
- **Descrizione**: Email dell'amministratore di default
- **Esempio**: `admin@yourdomain.com`
- **⚠️ IMPORTANTE**: NON usare `admin@admin.com` in produzione!

#### 4. `ADMIN_DEFAULT_PASSWORD`
- **Descrizione**: Password dell'amministratore di default
- **Esempio**: `MySecureAdminPassword123!`
- **⚠️ IMPORTANTE**:
  - Usa una password FORTE
  - NON usare `password` in produzione!
  - Cambiala dopo il primo login

---

### 🟡 CONSIGLIATI - Per una configurazione completa

#### 5. `JWT_ISSUER`
- **Descrizione**: Chi emette il token JWT
- **Default**: `AppointmentScheduler.API`
- **Esempio**: `https://api.yourdomain.com`

#### 6. `JWT_AUDIENCE`
- **Descrizione**: Chi può usare il token JWT
- **Default**: `AppointmentScheduler.Client`
- **Esempio**: `https://yourdomain.com`

#### 7. `JWT_EXPIRATION_MINUTES`
- **Descrizione**: Durata del token JWT in minuti
- **Default**: `1440` (24 ore)
- **Esempio**: `480` (8 ore per maggiore sicurezza)

#### 8. `CORS_ORIGINS`
- **Descrizione**: Origins permessi per CORS (separati da virgola)
- **Formato**: `https://domain1.com,https://domain2.com`
- **Esempio**:
  ```
  https://appointment-consumer-app.azurewebsites.net,https://appointment-merchant-app.azurewebsites.net,https://appointment-admin-app.azurewebsites.net
  ```

#### 9. `ADMIN_DEFAULT_FIRSTNAME`
- **Descrizione**: Nome dell'admin di default
- **Default**: `Admin`

#### 10. `ADMIN_DEFAULT_LASTNAME`
- **Descrizione**: Cognome dell'admin di default
- **Default**: `User`

---

## Secrets Richiesti per il Frontend

### Per Consumer App, Merchant App e Admin App

Questi secrets sono necessari per configurare le app frontend durante la build:

#### 1. `VITE_API_URL`
- **Descrizione**: URL dell'API backend
- **Esempio**: `https://appointment-scheduler-api.azurewebsites.net`
- **⚠️ IMPORTANTE**: NON includere `/api` alla fine

#### 2. `VITE_API_BASE_PATH` (opzionale)
- **Descrizione**: Base path per le chiamate API
- **Default**: `/api`

#### 3. `VITE_API_TIMEOUT` (opzionale)
- **Descrizione**: Timeout per le richieste API in millisecondi
- **Default**: `5000`

---

## Configurazione per Azure Deployment

Se stai usando Azure App Service, devi configurare le variabili d'ambiente anche lì.

### Metodo 1: Tramite Azure Portal (Manuale)

1. Vai su **Azure Portal** → **App Services**
2. Seleziona la tua app (es. `appointment-scheduler-api`)
3. Nel menu laterale, vai su **Configuration** → **Application settings**
4. Clicca su **+ New application setting**
5. Aggiungi ogni variabile d'ambiente con nome e valore
6. Clicca su **Save**
7. **Restart** l'app per applicare le modifiche

### Metodo 2: Tramite GitHub Actions (Automatico)

Se hai già configurato GitHub Actions per il deployment su Azure, puoi passare i secrets automaticamente.

Esempio di workflow (`.github/workflows/deploy-backend.yml`):

```yaml
name: Deploy Backend to Azure

on:
  push:
    branches:
      - main

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'appointment-scheduler-api'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}

      - name: Configure App Settings
        uses: azure/appservice-settings@v1
        with:
          app-name: 'appointment-scheduler-api'
          app-settings-json: |
            [
              {
                "name": "POSTGRESQLCONNSTR_DefaultConnection",
                "value": "${{ secrets.POSTGRESQLCONNSTR_DEFAULTCONNECTION }}",
                "slotSetting": false
              },
              {
                "name": "JWT_SECRET_KEY",
                "value": "${{ secrets.JWT_SECRET_KEY }}",
                "slotSetting": false
              },
              {
                "name": "ADMIN_DEFAULT_EMAIL",
                "value": "${{ secrets.ADMIN_DEFAULT_EMAIL }}",
                "slotSetting": false
              },
              {
                "name": "ADMIN_DEFAULT_PASSWORD",
                "value": "${{ secrets.ADMIN_DEFAULT_PASSWORD }}",
                "slotSetting": false
              },
              {
                "name": "CORS_ORIGINS",
                "value": "${{ secrets.CORS_ORIGINS }}",
                "slotSetting": false
              }
            ]
```

### Secrets Azure Richiesti

Per il deployment su Azure, aggiungi anche questi secrets:

#### `AZURE_WEBAPP_PUBLISH_PROFILE`
- **Descrizione**: Profilo di pubblicazione per Azure Web App
- **Dove trovarlo**:
  1. Azure Portal → App Services → La tua app
  2. Clicca su **Get publish profile**
  3. Si scaricherà un file `.PublishSettings`
  4. Copia tutto il contenuto del file e incollalo come secret

---

## Verifica della Configurazione

### Checklist Pre-Deployment

Prima di fare il deployment, verifica di aver configurato:

#### Backend (App API)
- [ ] `POSTGRESQLCONNSTR_DEFAULTCONNECTION`
- [ ] `JWT_SECRET_KEY`
- [ ] `ADMIN_DEFAULT_EMAIL`
- [ ] `ADMIN_DEFAULT_PASSWORD`
- [ ] `CORS_ORIGINS`
- [ ] `JWT_ISSUER` (opzionale)
- [ ] `JWT_AUDIENCE` (opzionale)
- [ ] `JWT_EXPIRATION_MINUTES` (opzionale)

#### Frontend (Consumer/Merchant/Admin Apps)
- [ ] `VITE_API_URL`
- [ ] `VITE_API_BASE_PATH` (opzionale)
- [ ] `VITE_API_TIMEOUT` (opzionale)

#### Azure (se applicabile)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE` (per ogni app)

### Test della Configurazione

1. **Dopo aver configurato i secrets**, fai un push su GitHub per triggerare il workflow
2. Vai su **Actions** nel repository per vedere il progresso
3. Se la build fallisce, controlla i logs per eventuali errori

### Test Locale

Per testare localmente prima del deployment:

1. Crea un file `.env` nella cartella `backend/AppointmentScheduler.API/`:
   ```bash
   cp backend/AppointmentScheduler.API/.env.example backend/AppointmentScheduler.API/.env
   ```

2. Modifica il file `.env` con i tuoi valori

3. Crea un file `.env` per ogni frontend app:
   ```bash
   cp frontend/consumer-app/.env.example frontend/consumer-app/.env
   cp frontend/merchant-app/.env.example frontend/merchant-app/.env
   cp frontend/admin-app/.env.example frontend/admin-app/.env
   ```

4. Testa l'applicazione localmente:
   ```bash
   # Backend
   cd backend/AppointmentScheduler.API
   dotnet run

   # Frontend (in finestre separate)
   cd frontend/consumer-app && npm run dev
   cd frontend/merchant-app && npm run dev
   cd frontend/admin-app && npm run dev
   ```

---

## Troubleshooting

### Problema: Build fallisce con "JWT SecretKey not configured"

**Soluzione**: Verifica di aver configurato il secret `JWT_SECRET_KEY` su GitHub.

### Problema: Database connection failed

**Soluzione**:
1. Verifica che `POSTGRESQLCONNSTR_DEFAULTCONNECTION` sia configurato correttamente
2. Controlla che il database sia accessibile dall'IP di Azure/GitHub Actions
3. Verifica username e password nella connection string

### Problema: CORS blocking requests in produzione

**Soluzione**:
1. Verifica che `CORS_ORIGINS` includa l'URL esatto delle tue frontend apps
2. Assicurati di includere HTTPS (non HTTP) in produzione
3. Riavvia l'app dopo aver modificato le impostazioni

### Problema: Admin user not created

**Soluzione**:
1. Verifica i secrets `ADMIN_DEFAULT_EMAIL` e `ADMIN_DEFAULT_PASSWORD`
2. Controlla i logs dell'applicazione
3. Assicurati che `SEED_DATABASE` non sia impostato a `false`

### Problema: Frontend non riesce a chiamare l'API

**Soluzione**:
1. Verifica che `VITE_API_URL` sia configurato correttamente
2. Controlla che l'URL non abbia `/api` alla fine
3. Verifica che CORS sia configurato correttamente sul backend

---

## Sicurezza - Best Practices

### ✅ DA FARE

1. **Rotazione periodica dei secrets**: Cambia i secrets sensibili ogni 3-6 mesi
2. **Secrets diversi per ambiente**: Usa secrets diversi per dev/staging/production
3. **Accesso limitato**: Solo chi ha bisogno dovrebbe avere accesso ai secrets
4. **Monitoring**: Monitora l'uso dei secrets e eventuali accessi non autorizzati
5. **Backup sicuro**: Salva i secrets in un password manager (es. 1Password, LastPass)

### ❌ DA NON FARE

1. **NON committare secrets** nel codice (anche in file `.env`)
2. **NON condividere secrets** via email, chat, o altri canali insicuri
3. **NON usare secrets di default** in produzione (es. `password`, `admin@admin.com`)
4. **NON riutilizzare secrets** tra diversi progetti o ambienti
5. **NON loggare secrets** nei logs dell'applicazione

---

## Riferimenti

- [GitHub Actions - Encrypted secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure App Service - Environment variables](https://learn.microsoft.com/en-us/azure/app-service/configure-common)
- [ASP.NET Core - Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Vite - Environment Variables](https://vitejs.dev/guide/env-and-mode.html)

---

## Supporto

Per problemi o domande:
1. Controlla la sezione [Troubleshooting](#troubleshooting)
2. Verifica i logs dell'applicazione
3. Consulta la documentazione di riferimento
4. Apri un issue su GitHub se il problema persiste
