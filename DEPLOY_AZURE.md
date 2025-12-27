# Guida Deployment su Azure

Questa guida ti porterà passo-passo nel deployment della tua applicazione su Azure.

## Prerequisiti

- Account Azure (puoi iniziare con credito gratuito di $200)
- Repository GitHub già configurato
- Azure CLI installato (opzionale, ma consigliato)

## Architettura Finale

```
Frontend (3 app React) → Azure Static Web Apps (GRATIS)
Backend (ASP.NET Core) → Azure App Service (~12€/mese)
Database → Azure PostgreSQL Flexible Server (~15€/mese)
```

---

## PARTE 1: Creare le Risorse Azure

### Step 1: Creare 3 Azure Static Web Apps (Frontend)

**Per CONSUMER APP:**

1. Vai su https://portal.azure.com
2. Cerca "Static Web Apps" nella barra di ricerca
3. Clicca "+ Create"
4. Compila:
   - **Subscription**: Seleziona la tua subscription
   - **Resource Group**: Crea nuovo → `appointment-scheduler-rg`
   - **Name**: `appointment-consumer-app`
   - **Plan type**: **Free**
   - **Region**: West Europe (o più vicino a te)
   - **Deployment details**:
     - Source: **GitHub**
     - Organization: Il tuo username GitHub
     - Repository: `appointment-scheduler`
     - Branch: `main`
     - Build Presets: **React**
     - App location: `/frontend/consumer-app`
     - Output location: `dist`
5. Clicca **Review + Create**
6. Clicca **Create**
7. **IMPORTANTE**: Vai su "Deployment token" e COPIA il token (lo userai dopo)

**Ripeti per MERCHANT APP:**
- Name: `appointment-merchant-app`
- App location: `/frontend/merchant-app`
- Output location: `dist`
- **SALVA il deployment token**

**Ripeti per ADMIN APP:**
- Name: `appointment-admin-app`
- App location: `/frontend/admin-app`
- Output location: `dist`
- **SALVA il deployment token**

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

### Step 3: Creare Azure App Service (Backend)

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
3. Si scaricherà un file XML → **SALVALO** (lo userai dopo)

---

## PARTE 2: Configurare GitHub Secrets

Ora devi aggiungere i token/credenziali come secrets in GitHub.

### Step 1: Vai su GitHub Secrets

1. Vai su https://github.com/[tuo-username]/appointment-scheduler
2. Clicca su **Settings** (in alto)
3. Nel menu a sinistra, vai su **Secrets and variables** → **Actions**
4. Clicca **"New repository secret"**

### Step 2: Aggiungi questi Secrets

Crea questi 5 secrets UNO PER UNO:

**Secret 1:**
- Name: `AZURE_STATIC_WEB_APPS_API_TOKEN_CONSUMER`
- Value: [Il deployment token della Consumer Static Web App]

**Secret 2:**
- Name: `AZURE_STATIC_WEB_APPS_API_TOKEN_MERCHANT`
- Value: [Il deployment token della Merchant Static Web App]

**Secret 3:**
- Name: `AZURE_STATIC_WEB_APPS_API_TOKEN_ADMIN`
- Value: [Il deployment token della Admin Static Web App]

**Secret 4:**
- Name: `AZURE_WEBAPP_NAME`
- Value: `appointment-scheduler-api` (il nome che hai dato all'App Service)

**Secret 5:**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Value: [Apri il file XML del publish profile e COPIA TUTTO il contenuto]

---

## PARTE 3: Configurare gli URL API nei Frontend

Prima di fare il primo deploy, devi dire ai frontend dove si trova il backend.

### Step 1: Trova l'URL del tuo App Service

1. Vai nell'App Service `appointment-scheduler-api` nel portale Azure
2. Nella pagina principale, trovi l'**URL**: `https://appointment-scheduler-api.azurewebsites.net`
3. **COPIA** questo URL

### Step 2: Configura le variabili d'ambiente nei frontend

Per ogni Static Web App creata, aggiungi la configurazione dell'API:

**Per Consumer App:**
1. Nel portale Azure, vai su `appointment-consumer-app`
2. Menu laterale → **Configuration**
3. Clicca **"New application setting"**
   - Name: `VITE_API_URL`
   - Value: `https://appointment-scheduler-api.azurewebsites.net`
4. Clicca **Save**

**Ripeti per Merchant App** (`appointment-merchant-app`)
**Ripeti per Admin App** (`appointment-admin-app`)

---

## PARTE 4: Primo Deploy

Ora tutto è pronto! Fai il primo deploy.

### Step 1: Commit e Push

I file sono già stati creati nella tua repository. Devi solo committare:

```bash
git add .
git commit -m "Add Azure deployment configuration"
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

## PARTE 5: Testa l'Applicazione

### URL Finali

Dopo il deploy, le tue app saranno disponibili a:

**Frontend:**
- Consumer: `https://appointment-consumer-app.azurestaticapps.net`
- Merchant: `https://appointment-merchant-app.azurestaticapps.net`
- Admin: `https://appointment-admin-app.azurestaticapps.net`

**Backend API:**
- API: `https://appointment-scheduler-api.azurewebsites.net`
- Swagger: `https://appointment-scheduler-api.azurewebsites.net/swagger`

### Test di Base

1. Vai sul Swagger: `https://appointment-scheduler-api.azurewebsites.net/swagger`
2. Prova a fare una chiamata di test (es: `/api/auth/register`)
3. Vai sulla Consumer App e prova a registrarti
4. Vai sulla Merchant App e prova a fare login

---

## PARTE 6: Domini Custom (Opzionale)

Se vuoi usare i tuoi domini:

**Per Static Web Apps:**
1. Vai sulla Static Web App nel portale Azure
2. Menu laterale → **Custom domains**
3. Clicca **"Add"**
4. Segui le istruzioni per aggiungere il record CNAME nel tuo DNS

**Per App Service:**
1. Vai sull'App Service nel portale Azure
2. Menu laterale → **Custom domains**
3. Clicca **"Add custom domain"**
4. Segui le istruzioni

---

## Troubleshooting

### Frontend non si collega al Backend

**Problema:** Errori CORS o "Network Error"

**Soluzione:**
1. Verifica che l'URL dell'API sia corretto in Configuration delle Static Web Apps
2. Nel backend, aggiorna il CORS in `Program.cs`:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowSpecificOrigins", policy =>
       {
           policy.WithOrigins(
               "https://appointment-consumer-app.azurestaticapps.net",
               "https://appointment-merchant-app.azurestaticapps.net",
               "https://appointment-admin-app.azurestaticapps.net"
           )
           .AllowAnyMethod()
           .AllowAnyHeader();
       });
   });
   ```

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

1. Vai su Static Web App → **Monitor** → **Application Insights**

---

## Costi Mensili Stimati

- Static Web Apps (x3): **GRATIS** (piano Free)
- App Service B1: **~12€/mese**
- PostgreSQL B1ms: **~15€/mese**
- **TOTALE: ~27€/mese**

Per risparmiare durante sviluppo:
- Usa App Service F1 (Free) invece di B1
- Ferma le risorse quando non le usi

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
1. Controlla i log in Azure Portal
2. Verifica i workflow su GitHub Actions
3. Controlla che tutti i secrets siano configurati correttamente

Buon deployment! 🚀
