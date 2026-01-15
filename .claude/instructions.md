# Claude Instructions - Appointment Scheduler Project

Queste sono le istruzioni permanenti per lo sviluppo di questo progetto. Seguile sempre quando lavori su questo codebase.

## Stack Tecnologico

- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core + PostgreSQL
- **Frontend:** React 18 + TypeScript + Vite + Tailwind CSS
- **Auth:** JWT Bearer Token
- **Database:** PostgreSQL (con supporto SQL Server opzionale)

## Principi di Sviluppo

### 0. Build Verification (PRIORITÀ MASSIMA)
**SEMPRE verificare build e integrazione dopo ogni modifica al codice.**
- Backend modificato → `dotnet build` DEVE passare
- Frontend modificato → `npm run build` DEVE passare
- Models/DTOs condivisi modificati → Verifica ENTRAMBI backend E frontend
- **NON committare MAI codice che non builda**
- Vedi sezione "Build & Run Verification Protocol" per dettagli completi

### 1. User-Friendly
- L'applicazione deve essere intuitiva e facile da usare
- Messaggi di errore chiari e comprensibili (non tecnici)
- Feedback immediato su tutte le azioni
- Loading states visibili
- Validazione client-side e server-side

### 2. Dependency Injection
- Usare SEMPRE dependency injection per i servizi
- Registrare tutti i servizi in Program.cs
- Creare interfacce per tutti i servizi
- Non usare mai `new` per istanziare servizi
- Preferire Scoped lifetime per servizi con DbContext

### 3. Commenti e Documentazione

**C# - XML Comments obbligatori:**
```csharp
/// <summary>
/// Descrizione chiara del metodo
/// </summary>
/// <param name="parametro">Descrizione parametro</param>
/// <returns>Descrizione return value</returns>
public async Task<Result> MetodoAsync(Parameter parametro)
```

**TypeScript - JSDoc per funzioni esportate:**
```typescript
/**
 * Descrizione della funzione
 * @param param - Descrizione parametro
 * @returns Descrizione return
 */
export const myFunction = (param: string): void => {}
```

**Regole:**
- Commentare TUTTI i metodi public/export
- NON commentare codice ovvio
- NO emoji nei commenti o messaggi dell'applicazione
- Preferire codice auto-esplicativo ai commenti inline

### 4. Testabilita' Locale
- Configurazioni esternalizzate (appsettings, .env)
- Docker-compose per dipendenze
- Seed data per test rapidi
- Setup documentato in QUICK_START.md

### 5. NO Emoji
- Non usare emoji in:
  - Commenti del codice
  - Messaggi di errore/successo
  - Log applicativi
  - Documentazione tecnica
- OK emoji solo in README/documentazione user-facing

## Architettura e Pattern

### Backend Structure
```
AppointmentScheduler.API/       - Controllers, Program.cs, middleware
AppointmentScheduler.Core/      - Services, business logic, interfaces
AppointmentScheduler.Data/      - DbContext, repositories, migrations
AppointmentScheduler.Shared/    - Models, DTOs, enums (condivisi)
```

### Servizi - Sempre con interfaccia
```csharp
// Interfaccia
public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request);
}

// Implementazione
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }
}

// Registrazione in Program.cs
builder.Services.AddScoped<IBookingService, BookingService>();
```

### Controllers - Thin, delegano ai servizi
```csharp
[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Crea una nuova prenotazione
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        var result = await _bookingService.CreateBookingAsync(request);
        return Ok(result);
    }
}
```

## Gestione Errori

### Backend
```csharp
// User-friendly
return BadRequest(new { message = "Email gia' in uso. Scegli un'altra email." });

// NO - troppo tecnico
return BadRequest(new { error = "SqlException: UNIQUE constraint violation" });
```

### Frontend
```typescript
// Chiaro per l'utente
setError("Impossibile completare la prenotazione. Riprova piu' tardi.");

// NO - troppo tecnico
setError("Error 500: Internal Server Error at POST /api/bookings");
```

## Database

### Async sempre
```csharp
// GIUSTO
var users = await _context.Users.ToListAsync();

// SBAGLIATO
var users = _context.Users.ToList();
```

### Include esplicito quando servono relazioni
```csharp
var booking = await _context.Bookings
    .Include(b => b.Service)
    .Include(b => b.User)
    .FirstOrDefaultAsync(b => b.Id == id);
```

## Frontend

### Custom Hooks per logica condivisa
```typescript
export const useAuth = () => {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);
    // ...
    return { user, loading, login, logout };
};
```

### Loading e Disabled States
```typescript
<button
    disabled={loading}
    className="..."
>
    {loading ? 'Caricamento...' : 'Salva'}
</button>
```

## Git Workflow

### Commit Messages
```
feat: Aggiunge servizio prenotazioni con dependency injection
fix: Corregge validazione email case-insensitive
refactor: Migliora gestione errori in AuthService
docs: Aggiorna QUICK_START con setup Docker
test: Aggiunge test per BookingService
```

### Non Committare
- Password o secrets
- appsettings.Local.json
- node_modules/, bin/, obj/, dist/
- File personali IDE

## Versioning Automatico

Questo progetto usa **Git-based semantic versioning** automatico. Le versioni sono gestite tramite Git tags e NON devono essere modificate manualmente.

### Sistema di Versioning

**Formato versione:** `{tag}+{commit}` (es. `0.0.1+abc1234`)

**Fonti:**
- **Version number**: Git tag (es. `v0.0.1`, `v1.2.3`)
- **Commit SHA**: Git commit hash corto (es. `abc1234`)
- **Build number**: GitHub Actions run number
- **Build time**: Timestamp UTC del build

### Regole CRITICHE

#### ❌ NON FARE MAI:
1. **NON** modificare manualmente versioni in `package.json`
2. **NON** modificare manualmente versioni in `.csproj`
3. **NON** hardcodare versioni nel codice
4. **NON** rimuovere o modificare `vite-version-plugin.ts`
5. **NON** rimuovere injection di env vars nei workflow GitHub Actions

#### ✅ FARE SEMPRE:
1. **USA Git tags** per incrementare le versioni:
   ```bash
   # Patch (0.0.1 → 0.0.2)
   git tag v0.0.2
   git push origin v0.0.2

   # Minor (0.0.2 → 0.1.0)
   git tag v0.1.0
   git push origin v0.1.0

   # Major (0.1.0 → 1.0.0)
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Verifica versioning** dopo modifiche ai frontend/backend:
   ```bash
   # Test build frontend
   cd frontend/consumer-app && npm run build

   # Verifica che vite-version-plugin.ts sia incluso
   # Verifica che vite.config.ts usi viteVersionPlugin()
   ```

### File Critici per Versioning

**NON MODIFICARE questi file senza consultare la documentazione:**

```
Backend:
✓ backend/AppointmentScheduler.API/Controllers/VersionController.cs
  - Legge VERSION, GIT_COMMIT_SHA da environment variables
  - Fallback a git commands per sviluppo locale

Frontend (tutti e 3: consumer, merchant, admin):
✓ vite-version-plugin.ts
  - Inietta versione Git durante build
✓ vite.config.ts
  - DEVE includere viteVersionPlugin() nei plugins
✓ src/vite-env.d.ts
  - Definisce tipi TypeScript per __APP_VERSION__, __GIT_COMMIT__
✓ src/components/VersionInfo.tsx
  - Mostra versioni frontend e backend

GitHub Actions:
✓ .github/workflows/deploy-*.yml
  - Step "Extract version info" OBBLIGATORIO
  - Injection env vars durante build OBBLIGATORIA
```

### Come Funziona

**Sviluppo Locale:**
```bash
# Il vite-version-plugin esegue automaticamente:
git describe --tags --abbrev=0  # → v0.0.1
git rev-parse --short HEAD      # → abc1234

# Risultato: versione "0.0.1+abc1234"
```

**GitHub Actions (Production):**
```yaml
# Workflow estrae Git info
- name: Extract version info
  run: |
    GIT_TAG=$(git describe --tags --abbrev=0)
    GIT_COMMIT=$(git rev-parse --short HEAD)
    # ...

# Build con env vars
- name: Build
  env:
    VERSION: ${{ steps.version.outputs.VERSION }}
    GIT_COMMIT_SHA: ${{ steps.version.outputs.GIT_COMMIT }}
```

### Quando Creare un Tag

Crea un nuovo Git tag quando:
- ✅ Feature completa pronta per rilascio
- ✅ Bugfix critico in produzione
- ✅ Breaking changes (major version)
- ✅ Prima del merge in `main`

**Procedura:**
```bash
# 1. Verifica che tutto sia committato
git status

# 2. Crea tag annotato con messaggio
git tag -a v0.1.0 -m "Release 0.1.0: Aggiunge sistema prenotazioni"

# 3. Push del tag
git push origin v0.1.0

# 4. GitHub Actions userà automaticamente questo tag
```

### Troubleshooting Versioning

**Problema:** Frontend mostra "0.0.1+dev" invece del commit SHA
**Soluzione:**
```bash
# Verifica che vite-version-plugin.ts esista
ls frontend/consumer-app/vite-version-plugin.ts

# Verifica che vite.config.ts lo importi
grep "viteVersionPlugin" frontend/consumer-app/vite.config.ts

# Rebuild
npm run build
```

**Problema:** Backend mostra "0.0.0+dev"
**Soluzione:**
```bash
# Verifica Git tags
git tag -l

# Se nessun tag esiste, creane uno
git tag v0.0.1
git push origin v0.0.1

# Verifica VersionController.cs
grep "GetGitTag" backend/AppointmentScheduler.API/Controllers/VersionController.cs
```

**Problema:** GitHub Actions non inietta versioni
**Soluzione:**
```yaml
# Verifica che il workflow abbia lo step "Extract version info"
# Verifica che le env vars siano passate al build:
env:
  VERSION: ${{ steps.version.outputs.VERSION }}
  GIT_COMMIT_SHA: ${{ steps.version.outputs.GIT_COMMIT }}
```

## Build & Run Verification Protocol

**CRITICO:** Ad ogni modifica di codice, DEVI sempre verificare che l'applicazione buildi e funzioni correttamente.

### Workflow Obbligatorio

Dopo OGNI modifica al codice, segui questi step:

1. **Verifica Build Backend** (se hai modificato backend)
   ```bash
   cd backend/AppointmentScheduler.API
   dotnet build

   # Se il build fallisce:
   # - Leggi gli errori di compilazione
   # - Correggi TUTTI gli errori
   # - Riprova il build
   # - Non procedere fino a build SUCCESS
   ```

2. **Verifica Build Frontend** (se hai modificato frontend)
   ```bash
   # Per merchant-app
   cd frontend/merchant-app
   npm run build

   # Per consumer-app
   cd frontend/consumer-app
   npm run build

   # Per admin-app
   cd frontend/admin-app
   npm run build

   # Se il build fallisce:
   # - Leggi gli errori TypeScript/Vite
   # - Correggi TUTTI gli errori e warning
   # - Riprova il build
   # - Non procedere fino a build SUCCESS
   ```

3. **Verifica Integrazione** (sempre)
   - Dopo aver fixato tutti i build errors, verifica che:
     - Backend compila senza errori
     - Frontend(s) modificato/i buildano senza errori
     - Non ci sono import mancanti o riferimenti a file/componenti rimossi
     - Le modifiche al backend sono compatibili con il frontend
     - Le modifiche al frontend usano correttamente le API backend

4. **Test Runtime (quando possibile)**
   - Se .NET SDK è disponibile: avvia il backend e verifica che parta
   - Se Node è disponibile: avvia il frontend in dev mode e verifica UI
   - Testa il flusso modificato end-to-end

### Quando Verificare

✅ **SEMPRE verificare build dopo:**
- Modifica a Models/DTOs condivisi tra backend e frontend
- Aggiunta/rimozione di proprietà da classi esistenti
- Refactoring che tocca più file
- Cambio di interfacce o contratti API
- Rimozione di file, componenti, o servizi
- Modifica a dependency injection registrations
- Aggiunta di nuove dependencies/imports

⚠️ **Verifica DOPPIA (backend + frontend) quando:**
- Modifichi DTOs (es. BusinessHoursDto)
- Modifichi Models condivisi (es. BusinessHours, BusinessHoursShift)
- Aggiungi/rimuovi proprietà da entità database
- Modifichi API endpoints o request/response format

### Errori Comuni da Prevenire

❌ **NON fare:**
- Modificare Models senza verificare che il frontend ancora compili
- Rimuovere proprietà da DTOs senza aggiornare tutti i file che le usano
- Committare codice che non builda
- Assumere che "probabilmente funziona" senza verificare

✅ **FARE sempre:**
- Build verification PRIMA di ogni commit
- Correggere TUTTI gli errori di compilazione
- Verificare che TUTTI i frontend buildino se hai toccato codice condiviso
- Testare che backend e frontend siano sincronizzati

### Procedura di Commit Corretta

```bash
# 1. Fai le tue modifiche al codice
# ...

# 2. VERIFICA BUILD (OBBLIGATORIO)
# Backend
cd backend/AppointmentScheduler.API
dotnet build
# ✅ Deve dire "Build succeeded"

# Frontend (tutti quelli modificati)
cd frontend/merchant-app
npm run build
# ✅ Deve completare senza errori

# 3. Solo SE tutti i build passano → Commit
git add .
git commit -m "..."
git push

# 4. Se il build fallisce → NON committare, fixa prima!
```

### Note Importanti

- **Non usare `dotnet` se non disponibile**: Se .NET SDK non è installato, verifica manualmente la sintassi e i riferimenti
- **Build errors = blocco totale**: Mai ignorare errori di build
- **Warning ≠ Errori**: I warning vanno valutati, ma non bloccano necessariamente
- **Integrazione > Sintassi**: Verificare che backend e frontend parlino la stessa lingua

## Code Review Checklist

Prima di ogni commit verificare:
- [ ] **✅ BUILD VERIFICATION COMPLETATA (backend + frontend modificati)**
- [ ] **✅ Nessun errore di compilazione**
- [ ] **✅ Integrazione backend-frontend verificata**
- [ ] XML/JSDoc comments su metodi public
- [ ] Dependency injection usata correttamente
- [ ] Nessun secret hardcoded
- [ ] Messaggi utente chiari (no tecnicismi)
- [ ] No emoji in codice/messaggi app
- [ ] Async/await per operazioni DB
- [ ] Loading states nel frontend
- [ ] Gestione errori user-friendly
- [ ] **Versioning automatico NON modificato manualmente**
- [ ] **vite-version-plugin.ts presente in tutti i frontend**
- [ ] **GitHub Actions workflows includono "Extract version info" step**

## Testing Locale - Setup Standard

```bash
# 1. Database
docker run --name appointment-db -e POSTGRES_PASSWORD=dev123 -p 5432:5432 -d postgres

# 2. Backend
cd backend/AppointmentScheduler.API
cp appsettings.Local.json.example appsettings.Local.json
dotnet ef database update
dotnet run

# 3. Frontend
cd frontend/consumer-app
npm install && npm run dev
```

## Riferimenti

- Dettagli completi: PROJECT_GUIDELINES.md
- Setup rapido: QUICK_START.md
- Configurazione: backend/CONFIGURATION.md
- Architettura: README.md
