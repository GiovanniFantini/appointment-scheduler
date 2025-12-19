# Backend Configuration

## File di Configurazione

### Gerarchia di Configurazione

ASP.NET Core carica le configurazioni in questo ordine (l'ultimo sovrascrive i precedenti):

1. `appsettings.json` - Configurazione base (versionata)
2. `appsettings.{Environment}.json` - Configurazione per ambiente (versionata)
3. `appsettings.Local.json` - Configurazione locale personale (**NON versionata**)
4. User Secrets - Secrets in development (solo in modalità Development)
5. Variabili d'ambiente - Override da sistema operativo

### File Versionati (in Git)

#### `appsettings.json`
- Configurazione base condivisa
- Contiene **placeholder** per valori sensibili
- **NON inserire password reali qui**

#### `appsettings.Development.json`
- Configurazione per ambiente di sviluppo
- Usa valori di default per sviluppo locale
- Può contenere connection string di test

### File NON Versionati (in .gitignore)

#### `appsettings.Local.json` ⚠️
- Per configurazioni personali locali
- **Usa questo file per le tue credenziali reali**
- Sovrascrive tutti gli altri appsettings

#### `appsettings.Production.json` ⚠️
- NON versionato per sicurezza
- Deve essere configurato solo sul server di produzione

## Setup Locale

### 1. Crea il tuo file locale (copia dall'esempio):

```bash
cp appsettings.Local.json.example appsettings.Local.json
```

### 2. Modifica `appsettings.Local.json` con le tue credenziali:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AppointmentScheduler;Username=postgres;Password=TUA_PASSWORD_VERA"
  },
  "JwtSettings": {
    "SecretKey": "la-tua-chiave-segreta-molto-lunga-e-sicura-almeno-32-caratteri",
    "Issuer": "AppointmentScheduler.API",
    "Audience": "AppointmentScheduler.Client",
    "ExpirationMinutes": 1440
  }
}
```

### 3. Alternative a appsettings.Local.json

#### User Secrets (consigliato per Development)

```bash
# Inizializza User Secrets
dotnet user-secrets init

# Aggiungi secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=..."
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"

# Lista tutti i secrets
dotnet user-secrets list
```

#### Variabili d'Ambiente

```bash
# Linux/macOS
export ConnectionStrings__DefaultConnection="Host=localhost;..."
export JwtSettings__SecretKey="your-secret-key"

# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Host=localhost;..."
$env:JwtSettings__SecretKey="your-secret-key"

# Windows CMD
set ConnectionStrings__DefaultConnection=Host=localhost;...
set JwtSettings__SecretKey=your-secret-key
```

## Sicurezza

### ⚠️ IMPORTANTE - NON versionare MAI:
- Password reali
- Chiavi API
- Connection string di produzione
- Certificati o chiavi private
- Token o secrets

### ✅ OK da versionare:
- Placeholder e valori di default
- Struttura della configurazione
- Configurazioni non sensibili (porte, timeout, ecc.)

## Database Setup

### PostgreSQL (default)

```bash
# Installa PostgreSQL
# Crea database
createdb AppointmentScheduler

# Esegui migrazioni
dotnet ef database update
```

### SQL Server (alternativa)

Modifica `Program.cs` e commenta/decommenta:

```csharp
// PostgreSQL (default)
options.UseNpgsql(connectionString);

// SQL Server (alternativa)
// options.UseSqlServer(connectionString);
```

Connection string per SQL Server:
```
Server=localhost;Database=AppointmentScheduler;User Id=sa;Password=YourPassword;TrustServerCertificate=true
```

## Troubleshooting

### "JWT SecretKey not configured"
- Assicurati che `JwtSettings:SecretKey` sia configurato
- La chiave deve essere lunga almeno 32 caratteri

### "Unable to connect to database"
- Verifica che PostgreSQL sia in esecuzione
- Controlla username e password
- Verifica che il database esista

### Configurazione non trovata
- Verifica il nome del file (case-sensitive su Linux)
- Controlla che il file sia nella stessa directory di `.csproj`
- Riavvia l'applicazione dopo modifiche ai file di configurazione
