# ⚡ DEPLOYMENT QUICK START

**Azioni immediate da completare PRIMA del prossimo deploy**

---

## 🚨 **STEP 1: Configura Variabili d'Ambiente su Azure (5 minuti)**

### **Vai su Azure Portal:**

```
https://portal.azure.com
→ App Services
→ appointment-scheduler-api
→ Configuration
→ Application settings
```

### **Aggiungi queste 5 variabili (clicca "+ New application setting" per ognuna):**

| Nome | Valore | Esempio |
|------|--------|---------|
| `POSTGRESQLCONNSTR_DefaultConnection` | La tua connection string PostgreSQL | `Host=mydb.postgres.database.azure.com;Port=5432;Database=AppointmentScheduler;Username=admin;Password=MySecurePass123!` |
| `JWT_SECRET_KEY` | **Genera con:** `openssl rand -base64 32` | `k8jH3nR9mL2pQ5vT7wX1yA4bC6dE8fG0` |
| `ADMIN_DEFAULT_EMAIL` | Email admin (NON usare admin@admin.com!) | `admin@yourdomain.com` |
| `ADMIN_DEFAULT_PASSWORD` | Password sicura | `MySecureAdminPass123!` |
| `CORS_ORIGINS` | URL frontend apps (separati da virgola) | `https://appointment-consumer-app.azurewebsites.net,https://appointment-merchant-app.azurewebsites.net,https://appointment-admin-app.azurewebsites.net` |

### **Dopo aver aggiunto le variabili:**

1. Clicca **SAVE** (in alto)
2. Clicca **Restart** (in alto) per riavviare l'app

---

## ✅ **STEP 2: Verifica Secrets GitHub (2 minuti)**

### **Vai su GitHub:**

```
https://github.com/GiovanniFantini/appointment-scheduler/settings/secrets/actions
```

### **Verifica che esistano questi secrets:**

- ✅ `AZURE_WEBAPP_NAME`
- ✅ `AZURE_WEBAPP_PUBLISH_PROFILE`
- ✅ `AZURE_WEBAPP_NAME_CONSUMER`
- ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_CONSUMER`
- ✅ `AZURE_CREDENTIALS`
- ✅ `AZURE_RESOURCE_GROUP`

**Se mancano:** Aggiungili seguendo la guida in `docs/GITHUB_SECRETS_SETUP.md`

---

## 🚀 **STEP 3: Deploy (1 minuto)**

### **Opzione A: Via GitHub (automatico)**

```bash
# Fai merge del branch su main
git checkout main
git merge claude/analyze-hardcoded-settings-OWR2R
git push origin main

# GitHub Actions farà deploy automaticamente
# Vai su: https://github.com/GiovanniFantini/appointment-scheduler/actions
```

### **Opzione B: Via Pull Request (raccomandato)**

```bash
# Crea PR dal branch
https://github.com/GiovanniFantini/appointment-scheduler/pull/new/claude/analyze-hardcoded-settings-OWR2R

# Review e merge
# GitHub Actions farà deploy dopo il merge
```

---

## 🔍 **STEP 4: Verifica Deploy (3 minuti)**

### **1. Controlla GitHub Actions:**

```
https://github.com/GiovanniFantini/appointment-scheduler/actions
```

Verifica che tutti i 4 workflow completino:
- ✅ Deploy Backend API
- ✅ Deploy Consumer App
- ✅ Deploy Merchant App
- ✅ Deploy Admin App

### **2. Testa le applicazioni:**

```bash
# Backend API (Swagger)
https://appointment-scheduler-api.azurewebsites.net/swagger

# Backend Health Check
https://appointment-scheduler-api.azurewebsites.net/health

# Consumer App
https://appointment-consumer-app.azurewebsites.net

# Merchant App
https://appointment-merchant-app.azurewebsites.net

# Admin App
https://appointment-admin-app.azurewebsites.net
```

### **3. Test Login:**

```
Email: [il valore che hai messo in ADMIN_DEFAULT_EMAIL]
Password: [il valore che hai messo in ADMIN_DEFAULT_PASSWORD]
```

---

## ⚠️ **STEP 5: Sicurezza Post-Deploy (2 minuti)**

### **SUBITO dopo il primo login:**

1. **Cambia la password admin**
   - Login → Profilo → Cambia Password
   - Usa una password FORTE (min 12 caratteri)

2. **Salva le nuove credenziali** in un password manager sicuro

---

## 🆘 **TROUBLESHOOTING RAPIDO**

### **❌ Build fallisce con "JWT SecretKey not configured"**

```bash
PROBLEMA: JWT_SECRET_KEY non configurato su Azure
SOLUZIONE:
1. Vai su Azure Portal → App Service → Configuration
2. Verifica che JWT_SECRET_KEY esista
3. Salva e Restart l'app
```

### **❌ "Database connection failed"**

```bash
PROBLEMA: Connection string non valida
SOLUZIONE:
1. Verifica POSTGRESQLCONNSTR_DefaultConnection su Azure
2. Controlla username/password
3. Verifica che il database PostgreSQL sia accessibile
4. Controlla firewall rules del database
```

### **❌ "CORS blocking requests"**

```bash
PROBLEMA: CORS_ORIGINS non configurato correttamente
SOLUZIONE:
1. Verifica CORS_ORIGINS su Azure Portal
2. Deve includere gli URL ESATTI: https://...azurewebsites.net
3. Separati da virgola, senza spazi
4. Restart l'app
```

### **❌ Frontend non carica / pagina bianca**

```bash
PROBLEMA: API non raggiungibile
SOLUZIONE:
1. Apri DevTools (F12) → Console
2. Controlla errori
3. Verifica che API_URL nel workflow punti all'URL corretto
4. Testa: https://appointment-scheduler-api.azurewebsites.net/health
```

---

## 📞 **SUPPORTO**

- **Problemi deployment?** → Vedi `docs/BUILD_VALIDATION_REPORT.md`
- **Configurazione ambiente dev?** → Vedi `docs/DEVELOPMENT_SETUP.md`
- **GitHub Secrets?** → Vedi `docs/GITHUB_SECRETS_SETUP.md`

---

## ✅ **CHECKLIST FINALE**

Prima di considerare il deployment completato:

- [ ] Variabili d'ambiente configurate su Azure Portal
- [ ] App backend riavviata su Azure
- [ ] GitHub Secrets verificati
- [ ] Branch mergiato su main
- [ ] Tutti i 4 workflow GitHub Actions completati con successo
- [ ] Swagger API accessibile
- [ ] Health check ritorna 200 OK
- [ ] Tutte le 3 frontend apps accessibili
- [ ] Login admin funzionante
- [ ] Password admin cambiata
- [ ] Nuove credenziali salvate in password manager

---

**🎉 Deployment completato! Il tuo sistema è ora configurato correttamente e pronto per l'uso.**

---

**Tempo totale stimato: ~15 minuti**
