# Azure Communication Services - Guida all'Integrazione

Questo documento descrive come configurare Azure Communication Services (ACS) Email per il sistema di notifiche transazionali del progetto, con particolare attenzione al flusso di recupero password.

---

## Prerequisiti

- Subscription Azure attiva
- Permessi per creare risorse nel resource group del progetto
- (Opzionale per dominio custom) Accesso al pannello DNS del dominio

---

## 1. Creare la Risorsa Azure Communication Services

1. Accedi al [portale Azure](https://portal.azure.com)
2. Clicca su **+ Crea una risorsa**
3. Cerca **Communication Services** e seleziona il servizio di Microsoft
4. Compila il form:
   - **Subscription**: seleziona la tua subscription
   - **Resource group**: usa il resource group del progetto (es. `rg-appointment-scheduler`)
   - **Resource name**: es. `acs-appointment-scheduler`
   - **Data location**: `Europe` (consigliato per GDPR)
5. Clicca **Review + create** → **Create**

---

## 2. Configurare Email Communication Service

Il servizio ACS principale non gestisce l'email direttamente. E' necessario aggiungere una risorsa **Email Communication Services** separata e collegarla all'ACS.

### 2a. Creare Email Communication Service

1. Nel portale Azure, crea una nuova risorsa: cerca **Email Communication Services**
2. Compila il form:
   - **Subscription** e **Resource group**: stessi della risorsa ACS
   - **Resource name**: es. `ecs-appointment-scheduler`
   - **Data location**: `Europe`
3. Clicca **Review + create** → **Create**

### 2b. Collegare al Communication Services principale

1. Vai alla risorsa **Communication Services** creata al punto 1
2. Nel menu laterale, clicca **Email** → **Connect your email domains**
3. Segui la procedura per collegare l'Email Communication Service

---

## 3. Configurare il Dominio Email

Hai due opzioni:

### Opzione A: Dominio Azure gratuito (sviluppo / test)

E' il metodo piu' rapido. Azure ti fornisce un sottodominio del tipo `xxxxxxxx.azurecomm.net`.

1. Nella risorsa **Email Communication Services**, vai a **Provision Domains**
2. Seleziona **Azure subdomain**
3. Clicca **Add domain** - Azure crea il dominio e lo verifica automaticamente
4. Il tuo indirizzo mittente sara' del tipo: `DoNotReply@xxxxxxxx.azurecomm.net`

### Opzione B: Dominio custom (produzione)

Permette di inviare email da un indirizzo come `no-reply@tuodominio.it`.

1. In **Provision Domains**, seleziona **Custom domain**
2. Inserisci il dominio (es. `mail.tuodominio.it`)
3. Azure ti fornisce i record DNS da aggiungere al tuo provider:
   - Record TXT per la verifica della proprieta' del dominio
   - Record MX per il DKIM
   - Record CNAME per il DKIM signing
4. Aggiungi i record nel pannello DNS del tuo provider
5. Torna su Azure e clicca **Verify** - la propagazione DNS puo' richiedere da qualche minuto a 48 ore
6. Una volta verificato, abilita **DKIM** e **DKIM2** per la deliverability

---

## 4. Ottenere la Connection String

1. Vai alla risorsa **Communication Services** (quella principale, non Email)
2. Nel menu laterale, clicca **Settings** → **Keys**
3. Copia la **Connection string** (Primary o Secondary)
   - Formato: `endpoint=https://acs-xxx.communication.azure.com/;accesskey=BASE64KEY==`

**Attenzione**: non committare la connection string nel repository.

---

## 5. Configurazione Locale (Sviluppo)

Aggiungi le seguenti chiavi al file `backend/AppointmentScheduler.API/appsettings.Local.json` (il file e' nel `.gitignore`):

```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://acs-xxx.communication.azure.com/;accesskey=LA_TUA_KEY==",
    "SenderAddress": "DoNotReply@xxxxxxxx.azurecomm.net",
    "SenderDisplayName": "Appointment Scheduler",
    "FrontendBaseUrl": "http://localhost:5173"
  }
}
```

**Nota sul FrontendBaseUrl**: questo URL viene incluso nel link di reset password inviato via email. In sviluppo punta alla consumer app (porta 5173). Per testare altre app, modifica il valore.

### Modalita' sviluppo senza ACS configurato

Se `ConnectionString` e' impostata al valore placeholder `CONFIGURE_IN_PRODUCTION_OR_USE_DEVELOPMENT_SETTINGS`, il servizio non invia email ma **logga il contenuto nel console dell'applicazione**. Questo permette di testare il flusso senza un account Azure:

```
info: AppointmentScheduler.Core.Services.AzureEmailService[0]
      MODALITA' SVILUPPO - Email non inviata. Destinatario: user@example.com, Oggetto: Recupero password - Appointment Scheduler
```

Il link di reset compare anche nei log del `PasswordResetService`.

---

## 6. Test del Flusso Reset Password End-to-End

Con il backend in esecuzione (`dotnet run`):

### Test 1: Richiesta reset

```bash
curl -X POST http://localhost:5000/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@admin.com"}'
```

Risposta attesa (200):
```json
{"message": "Se l'email risulta registrata, riceverai le istruzioni per il recupero della password."}
```

### Test 2: Reset con token

1. Recupera il token dai log del backend (in modalita' sviluppo)
2. Invia la richiesta di reset:

```bash
curl -X POST http://localhost:5000/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{"token": "IL_TOKEN_DAI_LOG", "newPassword": "NuovaPassword123"}'
```

Risposta attesa (200):
```json
{"message": "Password aggiornata con successo. Puoi ora accedere con la nuova password."}
```

### Test 3: Token gia' usato

Ripetere la richiesta del Test 2 con lo stesso token.

Risposta attesa (400):
```json
{"message": "Il link non e' piu' valido. Richiedi un nuovo recupero password."}
```

---

## 7. Deploy in Produzione

### Opzione A: Azure App Service - Variabili di ambiente

Nel portale Azure, vai alla tua App Service → **Configuration** → **Application settings** e aggiungi:

| Nome | Valore |
|------|--------|
| `AzureCommunicationServices__ConnectionString` | `endpoint=https://...` |
| `AzureCommunicationServices__SenderAddress` | `DoNotReply@tuodominio.azurecomm.net` |
| `AzureCommunicationServices__SenderDisplayName` | `Appointment Scheduler` |
| `AzureCommunicationServices__FrontendBaseUrl` | `https://appointment-consumer-app.azurewebsites.net` |

**Nota**: ASP.NET Core converte automaticamente i doppi underscore `__` in separatori gerarchici, quindi `AzureCommunicationServices__ConnectionString` corrisponde a `AzureCommunicationServices:ConnectionString` in appsettings.json.

### Opzione B: Azure Key Vault (consigliato per produzione)

1. Crea un Key Vault nella stessa subscription
2. Aggiungi il secret `AzureCommunicationServices--ConnectionString` (con trattino doppio come separatore)
3. Concedi alla Managed Identity dell'App Service il ruolo **Key Vault Secrets User**
4. In `Program.cs`, aggiungi il Key Vault come configuration provider (richiede il pacchetto `Azure.Extensions.AspNetCore.Configuration.Secrets`)

---

## 8. Troubleshooting

### L'email non viene inviata (ACS configurato)

1. **Verifica il dominio**: nel portale ACS → Email → il dominio deve essere in stato `Verified`
2. **Quota**: ACS ha un limite di 100 email/minuto nel piano gratuito
3. **Log**: controlla i log dell'applicazione per errori `RequestFailedException`
4. **SPF/DKIM**: per i domini custom, assicurati che i record DNS siano propagati

### Errore "SendMessageFailed"

Probabilmente la connection string e' scaduta o non e' valida. Rigenera le chiavi in Azure → Communication Services → Keys.

### Il link nell'email non funziona

Verifica che `FrontendBaseUrl` in configurazione corrisponda all'URL effettivo dell'app frontend. Il link ha il formato `{FrontendBaseUrl}/reset-password?token={token}`.

### Token scaduto dopo pochi minuti

Il token ha una validita' di 1 ora. Se l'ora di sistema del server e' desincronizzata (clock skew), il token potrebbe apparire scaduto prematuramente. Verifica che il server usi NTP.

### Rate limiting: "troppo presto per un nuovo reset"

Il sistema accetta al massimo una richiesta di reset ogni 60 secondi per lo stesso account. Questo e' un limite intenzionale per prevenire l'abuso del servizio email.
