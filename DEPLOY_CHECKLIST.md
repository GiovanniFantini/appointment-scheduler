# Checklist Deploy Azure - Da Fare MANUALMENTE

Segui questa checklist punto per punto. Ogni ✅ rappresenta un passo completato.

## FASE 1: Creare Risorse Azure (Portal)

### Static Web Apps (Frontend - GRATIS)

- [ ] **Consumer App**
  - [ ] Creata risorsa `appointment-consumer-app`
  - [ ] Deployment token salvato
  - [ ] App location: `/frontend/consumer-app`

- [ ] **Merchant App**
  - [ ] Creata risorsa `appointment-merchant-app`
  - [ ] Deployment token salvato
  - [ ] App location: `/frontend/merchant-app`

- [ ] **Admin App**
  - [ ] Creata risorsa `appointment-admin-app`
  - [ ] Deployment token salvato
  - [ ] App location: `/frontend/admin-app`

### Database PostgreSQL

- [ ] **PostgreSQL Flexible Server**
  - [ ] Creato server `appointment-scheduler-db`
  - [ ] Piano: Burstable B1ms
  - [ ] Username admin salvato
  - [ ] Password salvata
  - [ ] Connection string copiata
  - [ ] Networking: Public access abilitato

### App Service (Backend)

- [ ] **App Service**
  - [ ] Creata risorsa `appointment-scheduler-api`
  - [ ] Runtime: .NET 8 (LTS)
  - [ ] OS: Linux
  - [ ] Piano: B1 Basic (o F1 per test)
  - [ ] Publish profile scaricato (file XML)

### Configurazione App Service

- [ ] **Connection String**
  - [ ] Aggiunto `DefaultConnection` (PostgreSQL)

- [ ] **Application Settings**
  - [ ] Aggiunto `JwtSettings__SecretKey` (min 32 caratteri)

### Configurazione Static Web Apps

Per OGNI Static Web App (Consumer, Merchant, Admin):

- [ ] **Consumer App - Configuration**
  - [ ] Aggiunto `VITE_API_URL` = URL dell'App Service

- [ ] **Merchant App - Configuration**
  - [ ] Aggiunto `VITE_API_URL` = URL dell'App Service

- [ ] **Admin App - Configuration**
  - [ ] Aggiunto `VITE_API_URL` = URL dell'App Service

---

## FASE 2: GitHub Secrets

Vai su: `Settings` → `Secrets and variables` → `Actions`

- [ ] **AZURE_STATIC_WEB_APPS_API_TOKEN_CONSUMER**
  - [ ] Token della Consumer Static Web App incollato

- [ ] **AZURE_STATIC_WEB_APPS_API_TOKEN_MERCHANT**
  - [ ] Token della Merchant Static Web App incollato

- [ ] **AZURE_STATIC_WEB_APPS_API_TOKEN_ADMIN**
  - [ ] Token della Admin Static Web App incollato

- [ ] **AZURE_WEBAPP_NAME**
  - [ ] Nome: `appointment-scheduler-api`

- [ ] **AZURE_WEBAPP_PUBLISH_PROFILE**
  - [ ] Tutto il contenuto del file XML publish profile incollato

---

## FASE 3: Deploy

- [ ] **Commit e Push**
  ```bash
  git add .
  git commit -m "Add Azure deployment configuration"
  git push origin main
  ```

- [ ] **Monitorare GitHub Actions**
  - [ ] Workflow "Deploy Consumer App" completato ✅
  - [ ] Workflow "Deploy Merchant App" completato ✅
  - [ ] Workflow "Deploy Admin App" completato ✅
  - [ ] Workflow "Deploy Backend API" completato ✅

---

## FASE 4: Database Migration

Scegli UNA delle due opzioni:

**Opzione A - Da Locale:**
- [ ] Modificato connection string in `appsettings.Local.json`
- [ ] Eseguito `dotnet ef database update`
- [ ] Ripristinato connection string locale

**Opzione B - Da Azure Cloud Shell:**
- [ ] Aperto Cloud Shell nel portale Azure
- [ ] Eseguito script di migration

---

## FASE 5: Test Finale

- [ ] **Backend API**
  - [ ] Swagger accessibile: `https://appointment-scheduler-api.azurewebsites.net/swagger`
  - [ ] Endpoint `/api/auth/register` funzionante

- [ ] **Consumer App**
  - [ ] App accessibile: `https://appointment-consumer-app.azurestaticapps.net`
  - [ ] Registrazione utente funzionante
  - [ ] Login funzionante

- [ ] **Merchant App**
  - [ ] App accessibile: `https://appointment-merchant-app.azurestaticapps.net`
  - [ ] Login funzionante

- [ ] **Admin App**
  - [ ] App accessibile: `https://appointment-admin-app.azurestaticapps.net`
  - [ ] Login funzionante

---

## FASE 6: CORS (Se necessario)

Se vedi errori CORS:

- [ ] Aggiornato `Program.cs` con gli URL corretti delle Static Web Apps
- [ ] Ricompilato e ridistribuito backend
- [ ] Testato nuovamente

---

## Informazioni da Salvare

**URL Finali:**
- Consumer App: `https://appointment-consumer-app.azurestaticapps.net`
- Merchant App: `https://appointment-merchant-app.azurestaticapps.net`
- Admin App: `https://appointment-admin-app.azurestaticapps.net`
- Backend API: `https://appointment-scheduler-api.azurewebsites.net`

**Credenziali Database:**
- Server: `appointment-scheduler-db.postgres.database.azure.com`
- Database: `AppointmentScheduler`
- Username: [il tuo username]
- Password: [la tua password]

**JWT Secret:**
- [la tua chiave segreta di 32+ caratteri]

---

## Problemi Comuni

**Deploy fallisce su GitHub Actions:**
- ✅ Verifica che tutti i secrets siano configurati
- ✅ Controlla i log del workflow

**Frontend non si collega al Backend:**
- ✅ Verifica `VITE_API_URL` nelle Static Web Apps
- ✅ Verifica CORS nel backend

**Database connection error:**
- ✅ Verifica connection string nell'App Service
- ✅ Verifica networking del PostgreSQL

---

**DEPLOY COMPLETATO! 🎉**

Prossimi passi:
- Monitora i log in Azure Portal
- Configura backup automatici
- Aggiungi Application Insights
- Configura domini custom (opzionale)
