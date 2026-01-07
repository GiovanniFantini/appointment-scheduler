# Guida Deployment su Azure

Questa guida ti porterà passo-passo nel deployment della tua applicazione su Azure.

## Prerequisiti

- Account Azure (puoi iniziare con credito gratuito di $200)
- Repository GitHub già configurato
- Azure CLI installato (opzionale, ma consigliato)

## Architettura Finale

```
Frontend (3 app React) → Azure Web App (App Service) (~12€/mese per app o piano condiviso)
Backend (ASP.NET Core) → Azure Web App (App Service) (~12€/mese)
Database → Azure PostgreSQL Flexible Server (~15€/mese)
```

**Nota:** Con questa architettura tutti i frontend usano Azure Web App invece di Azure Static Web Apps, garantendo migliore compatibilità con le SPA React e il pieno controllo del routing.

---

## PARTE 1: Creare le Risorse Azure

### Step 1: Creare 3 Azure Web Apps (Frontend)

**Per CONSUMER APP:**

1. Vai su https://portal.azure.com
2. Cerca "App Services" nella barra di ricerca
3. Clicca "+ Create"
4. Compila:
   - **Subscription**: Seleziona la tua subscription
   - **Resource Group**: Crea nuovo → `appointment-scheduler-rg`
   - **Name**: `appointment-consumer-app` (deve essere univoco globalmente)
   - **Publish**: Code
   - **Runtime stack**: Node 20 LTS
   - **Operating System**: Linux
   - **Region**: West Europe (o più vicino a te)
   - **Pricing plan**:
     - Clicca "Explore pricing plans"
     - Scegli **Basic B1** (~12€/mese) oppure **Free F1** per test
     - **IMPORTANTE:** Puoi usare lo stesso piano per tutte e 3 le app frontend per risparmiare!
5. Clicca **Review + Create** → **Create**
6. **SCARICARE IL PUBLISH PROFILE:**
   - Nella pagina principale dell'App Service
   - Clicca **"Get publish profile"** in alto
   - Si scaricherà un file XML → **SALVALO** come `consumer-publish-profile.xml`

**Ripeti per MERCHANT APP:**
- Name: `appointment-merchant-app`
- Usa lo **STESSO pricing plan** di Consumer App (per risparmiare)
- **SALVA il publish profile** come `merchant-publish-profile.xml`

**Ripeti per ADMIN APP:**
- Name: `appointment-admin-app`
- Usa lo **STESSO pricing plan** (per risparmiare)
- **SALVA il publish profile** come `admin-publish-profile.xml`

**Tip per risparmiare:** Quando crei la seconda e terza app, nel campo "Pricing plan" seleziona lo stesso piano della prima app. In questo modo paghi solo un piano B1 (~12€/mese) invece di tre!

---

### Step 2: Creare Azure Database for PostgreSQL

1. Nel portale Azure, cerca "Azure Database for PostgreSQL Flexible Server"
2. Clicca "+ Create"
3. Compila:
   - **Resource Group**: `appointment-scheduler-rg` (usa lo stesso)
   - **Server name**: `appointment-scheduler-db` (deve essere univoco globalmente)
   - **Region**: West Europe (stessa del frontend)
   - **PostgreSQL version**: 16
   - **Workload type**: Development (più economico)
   - **Compute + Storage**:
     - Clicca "Configure server"
     - Scegli **Burstable, B1ms** (1 vCore, 2GB RAM) → ~15€/mese
   - **Admin username**: `adminuser`
   - **Password**: Crea una password sicura e **SALVALA**
4. Tab "Networking":
   - **Connectivity method**: Public access (0.0.0.0 - 255.255.255.255)
   - ✅ Allow public access from any Azure service
5. Clicca **Review + Create** → **Create**

**DOPO LA CREAZIONE:**
1. Vai alla risorsa creata
2. Nel menu a sinistra, clicca "Connection strings"
3. **COPIA** la connection string ADO.NET (tipo):
   ```
   Server=appointment-scheduler-db.postgres.database.azure.com;Database=AppointmentScheduler;Port=5432;User Id=adminuser;Password={la_tua_password};Ssl Mode=Require;
   ```
4. Sostituisci `{la_tua_password}` con la password che hai scelto

---

### Step 3: Creare Azure App Service (Backend API)

1. Nel portale Azure, cerca "App Services"
2. Clicca "+ Create"
3. Compila:
   - **Resource Group**: `appointment-scheduler-rg`
   - **Name**: `appointment-scheduler-api` (deve essere univoco)
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux
   - **Region**: West Europe
   - **Pricing plan**:
     - Clicca "Explore pricing plans"
     - Scegli **Basic B1** (~12€/mese) oppure **Free F1** per test
4. Clicca **Review + Create** → **Create**

**DOPO LA CREAZIONE:**
1. Vai alla risorsa creata
2. Nel menu a sinistra, clicca "Configuration"
3. Clicca "New connection string" e aggiungi:
   - **Name**: `DefaultConnection`
   - **Value**: [INCOLLA la connection string PostgreSQL che hai salvato prima]
   - **Type**: PostgreSQL
4. Clicca "New application setting" e aggiungi:
   - **Name**: `JwtSettings__SecretKey`
   - **Value**: Una stringa random di almeno 32 caratteri (es: `super-secure-jwt-key-minimum-32-chars-production-2024`)
5. Clicca **Save** in alto

**SCARICARE IL PUBLISH PROFILE:**
1. Nella pagina principale dell'App Service
2. Clicca **"Get publish profile"** in alto
3. Si scaricherà un file XML → **SALVALO** come `backend-publish-profile.xml`

---

## PARTE 2: Configurare GitHub Secrets

Ora devi aggiungere i token/credenziali come secrets in GitHub.

### Step 1: Vai su GitHub Secrets

1. Vai su https://github.com/[tuo-username]/appointment-scheduler
2. Clicca su **Settings** (in alto)
3. Nel menu a sinistra, vai su **Secrets and variables** → **Actions**
4. Clicca **"New repository secret"**

### Step 2: Aggiungi questi Secrets

Crea questi secrets UNO PER UNO:

**Secret 1:**
- Name: `AZURE_WEBAPP_NAME_CONSUMER`
- Value: `appointment-consumer-app` (il nome della Web App)

**Secret 2:**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE_CONSUMER`
- Value: [Apri consumer-publish-profile.xml e COPIA TUTTO il contenuto]

**Secret 3:**
- Name: `AZURE_WEBAPP_NAME_MERCHANT`
- Value: `appointment-merchant-app`

**Secret 4:**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE_MERCHANT`
- Value: [Apri merchant-publish-profile.xml e COPIA TUTTO il contenuto]

**Secret 5:**
- Name: `AZURE_WEBAPP_NAME_ADMIN`
- Value: `appointment-admin-app`

**Secret 6:**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE_ADMIN`
- Value: [Apri admin-publish-profile.xml e COPIA TUTTO il contenuto]

**Secret 7:**
- Name: `AZURE_WEBAPP_NAME`
- Value: `appointment-scheduler-api` (il nome dell'App Service backend)

**Secret 8:**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Value: [Apri backend-publish-profile.xml e COPIA TUTTO il contenuto]

**Secret 9:**
- Name: `VITE_API_URL`
- Value: `https://appointment-scheduler-api.azurewebsites.net` (l'URL del backend)

---

## PARTE 3: Primo Deploy

Ora tutto è pronto! Fai il primo deploy.

### Step 1: Commit e Push

I file sono già stati creati nella tua repository. Devi solo committare:

```bash
git add .
git commit -m "Migrate from Static Web Apps to Web App for frontend hosting"
git push origin main
```

### Step 2: Monitora i Deploy

1. Vai su GitHub → **Actions**
2. Vedrai partire 4 workflow:
   - Deploy Consumer App
   - Deploy Merchant App
   - Deploy Admin App
   - Deploy Backend API

3. Clicca su ognuno per vedere il progresso
4. Se tutto va bene, vedrai checkmark verdi ✅

### Step 3: Inizializza il Database

Il database è vuoto. Devi eseguire le migrations:

**Opzione A - Da locale (più facile):**

1. Nel tuo `appsettings.Local.json`, cambia temporaneamente la connection string con quella di Azure
2. Esegui:
   ```bash
   cd backend/AppointmentScheduler.API
   dotnet ef database update
   ```
3. Ripristina la connection string locale

**Opzione B - Da Azure Cloud Shell:**

1. Nel portale Azure, clicca sull'icona Cloud Shell (>_) in alto a destra
2. Scegli "Bash"
3. Esegui:
   ```bash
   # Installa .NET SDK
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

   # Clone repository
   git clone https://github.com/[tuo-username]/appointment-scheduler.git
   cd appointment-scheduler/backend/AppointmentScheduler.API

   # Aggiorna database (usa la connection string di Azure)
   dotnet ef database update --connection "[TUA_CONNECTION_STRING]"
   ```

---

## PARTE 4: Testa l'Applicazione

### URL Finali

Dopo il deploy, le tue app saranno disponibili a:

**Frontend:**
- Consumer: `https://appointment-consumer-app.azurewebsites.net`
- Merchant: `https://appointment-merchant-app.azurewebsites.net`
- Admin: `https://appointment-admin-app.azurewebsites.net`

**Backend API:**
- API: `https://appointment-scheduler-api.azurewebsites.net`
- Swagger: `https://appointment-scheduler-api.azurewebsites.net/swagger`

### Test di Base

1. Vai sul Swagger: `https://appointment-scheduler-api.azurewebsites.net/swagger`
2. Prova a fare una chiamata di test (es: `/api/auth/register`)
3. Vai sulla Consumer App e prova a registrarti
4. Vai sulla Merchant App e prova a fare login

---

## PARTE 5: Domini Custom (Opzionale)

Se vuoi usare i tuoi domini:

**Per Web Apps:**
1. Vai sulla Web App nel portale Azure
2. Menu laterale → **Custom domains**
3. Clicca **"Add custom domain"**
4. Segui le istruzioni per aggiungere il record CNAME nel tuo DNS

---

## Troubleshooting

### Frontend non si collega al Backend

**Problema:** Errori CORS o "Network Error"

**Soluzione:**
1. Verifica che `VITE_API_URL` sia configurato correttamente nei secrets GitHub
2. Nel backend, aggiorna il CORS in `Program.cs`:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowSpecificOrigins", policy =>
       {
           policy.WithOrigins(
               "https://appointment-consumer-app.azurewebsites.net",
               "https://appointment-merchant-app.azurewebsites.net",
               "https://appointment-admin-app.azurewebsites.net"
           )
           .AllowAnyMethod()
           .AllowAnyHeader();
       });
   });
   ```

### Deploy Frontend Fallisce

**Problema:** "Build failed" o errori durante npm install

**Soluzione:**
- Verifica che il path in `.github/workflows/deploy-*-app.yml` sia corretto
- Controlla i log in GitHub Actions per vedere l'errore specifico

### Pagina Bianca o 404 su Route

**Problema:** La app mostra una pagina bianca o errori 404 quando navighi a una route

**Soluzione:**
- Verifica che il file `web.config` sia presente nella cartella `public` di ogni frontend
- Il file `web.config` gestisce il routing SPA su IIS (il web server di Azure Web App)

### Deploy Backend Fallisce

**Problema:** "Could not find project"

**Soluzione:**
- Verifica che il path in `.github/workflows/deploy-backend-api.yml` sia corretto

### Database Connection Error

**Problema:** Backend non si connette al database

**Soluzione:**
1. Nel portale Azure → PostgreSQL → Networking
2. Verifica che "Allow public access from any Azure service" sia abilitato
3. Verifica la connection string nelle Configuration dell'App Service

---

## Monitoraggio e Log

### Vedere i Log del Backend

1. Vai su App Service → **Log stream** (menu laterale)
2. Vedrai i log in tempo reale

### Vedere i Log dei Frontend

1. Vai su App Service → **Log stream** (menu laterale)
2. Per log più dettagliati, vai su **Diagnose and solve problems**

---

## Costi Mensili Stimati

**Con App Service Plan condiviso tra i 3 frontend:**
- App Service B1 (condiviso per 3 frontend): **~12€/mese**
- App Service B1 (backend): **~12€/mese**
- PostgreSQL B1ms: **~15€/mese**
- **TOTALE: ~39€/mese**

**Con piano Free per sviluppo:**
- App Service F1 (x4): **GRATIS** (con limitazioni)
- PostgreSQL B1ms: **~15€/mese**
- **TOTALE: ~15€/mese**

Per risparmiare durante sviluppo:
- Usa App Service F1 (Free) invece di B1
- Ferma le risorse quando non le usi
- Usa un solo piano B1 condiviso per tutti i frontend

---

## Vantaggi di Web App vs Static Web Apps

**Perché abbiamo migrato a Web App:**
- ✅ Pieno controllo sul routing delle SPA
- ✅ Nessun problema con MIME types o file statici
- ✅ Migliore gestione delle variabili d'ambiente
- ✅ Compatibilità garantita con React Router
- ✅ Facile debugging con log stream in tempo reale
- ✅ Stesso tipo di risorsa del backend (più semplice da gestire)
- ✅ Supporto completo per web.config e configurazioni IIS

**Limitazioni di Static Web Apps che abbiamo risolto:**
- ❌ Problemi con il routing client-side
- ❌ Configurazione complessa per SPA
- ❌ Limitazioni sul piano Free
- ❌ Gestione complicata delle variabili d'ambiente build-time

---

## Prossimi Passi

- ✅ Configura backup automatici del database
- ✅ Configura Application Insights per monitoring
- ✅ Aggiungi domini custom
- ✅ Configura SSL/TLS personalizzato
- ✅ Setup CI/CD per branch di staging

---

## Supporto

Se hai problemi:
1. Controlla i log in Azure Portal (Log Stream)
2. Verifica i workflow su GitHub Actions
3. Controlla che tutti i secrets siano configurati correttamente
4. Verifica che il file `web.config` sia presente in ogni frontend

Buon deployment! 🚀
