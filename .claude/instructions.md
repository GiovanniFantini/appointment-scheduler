# Claude Instructions - Appointment Scheduler

Istruzioni permanenti per lo sviluppo di questo progetto.

## Stack Tecnologico

- **Backend:** ASP.NET Core 8 + EF Core + PostgreSQL
- **Frontend:** React 18 + TypeScript + Vite + Tailwind CSS
- **Auth:** JWT Bearer Token

## Principi di Sviluppo

### 1. User-Friendly
- Interfacce intuitive
- Messaggi di errore chiari (non tecnici)
- Feedback immediato
- Loading states visibili
- Validazione client e server

### 2. Dependency Injection
- Usare SEMPRE DI per servizi
- Registrare in Program.cs
- Creare interfacce per servizi
- Non usare `new` per istanziare servizi
- Preferire Scoped per servizi con DbContext

### 3. Commenti e Documentazione

**C# - XML Comments:**
```csharp
/// <summary>Descrizione metodo</summary>
/// <param name="parametro">Descrizione</param>
/// <returns>Descrizione return</returns>
public async Task<Result> MetodoAsync(Parameter parametro)
```

**TypeScript - JSDoc:**
```typescript
/** Descrizione funzione
 * @param param - Descrizione
 * @returns Descrizione return */
export const myFunction = (param: string): void => {}
```

**Regole:**
- Commentare TUTTI i metodi public/export
- NON commentare codice ovvio
- NO emoji in commenti o messaggi app
- Preferire codice auto-esplicativo

### 4. Testabilita'
- Configurazioni esternalizzate (appsettings, .env)
- Docker-compose per dipendenze
- Seed data per test
- Setup documentato in SETUP.md

### 5. NO Emoji
NO emoji in:
- Commenti codice
- Messaggi errore/successo
- Log applicativi
- Documentazione tecnica

OK solo in README/documentazione user-facing

## Architettura

```
AppointmentScheduler.API/       # Controllers, Program.cs, middleware
AppointmentScheduler.Core/      # Services, business logic, interfaces
AppointmentScheduler.Data/      # DbContext, repositories, migrations
AppointmentScheduler.Shared/    # Models, DTOs, enums
```

### Servizi - Sempre con interfaccia
```csharp
// Interfaccia
public interface IBookingService { Task<BookingDto> CreateBookingAsync(...); }

// Implementazione
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    public BookingService(ApplicationDbContext context) { _context = context; }
}

// Registrazione
builder.Services.AddScoped<IBookingService, BookingService>();
```

### Controllers - Thin, delegano ai servizi
```csharp
[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public BookingsController(IBookingService bookingService) { _bookingService = bookingService; }

    /// <summary>Crea nuova prenotazione</summary>
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
// Chiaro
setError("Impossibile completare. Riprova piu' tardi.");

// NO - tecnico
setError("Error 500: Internal Server Error");
```

## Database

### DateTime e PostgreSQL - CRITICO
PostgreSQL con Npgsql richiede che TUTTI i DateTime salvati su colonne `timestamp with time zone` abbiano `Kind=Utc`.
I DateTime che arrivano dalla deserializzazione JSON del frontend hanno `Kind=Unspecified` e causano:
`System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'`

**Regola:** Normalizzare SEMPRE a UTC prima di salvare su DB:
```csharp
// Helper da usare nei servizi per DateTime nullable
private static DateTime? NormalizeToUtc(DateTime? dateTime)
{
    if (dateTime == null) return null;
    var dt = dateTime.Value;
    return dt.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        : dt.ToUniversalTime();
}

// Per DateTime non-nullable
var utcDate = dt.Kind == DateTimeKind.Unspecified
    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    : dt.ToUniversalTime();
```

**Dove applicare:**
- Ogni campo DateTime che arriva da un DTO/Request del frontend (ExpiresAt, StartDate, EndDate, ecc.)
- NON serve per `DateTime.UtcNow` (gia' UTC)
- NON serve per campi calcolati internamente con `DateTime.UtcNow`

### Async sempre
```csharp
// GIUSTO
var users = await _context.Users.ToListAsync();

// SBAGLIATO
var users = _context.Users.ToList();
```

### Include esplicito
```csharp
var booking = await _context.Bookings
    .Include(b => b.Service)
    .Include(b => b.User)
    .FirstOrDefaultAsync(b => b.Id == id);
```

## Frontend

### Custom Hooks
```typescript
export const useAuth = () => {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);
    return { user, loading, login, logout };
};
```

### Loading States
```typescript
<button disabled={loading}>
    {loading ? 'Caricamento...' : 'Salva'}
</button>
```

## Git Workflow

### Commit Messages
```
feat: Aggiunge servizio prenotazioni
fix: Corregge validazione email
refactor: Migliora gestione errori
docs: Aggiorna SETUP
test: Aggiunge test BookingService
```

### Non Committare
- Password o secrets
- appsettings.Local.json
- node_modules/, bin/, obj/, dist/
- File IDE personali

## Versioning Automatico

Sistema **Git-based semantic versioning** automatico.

**Formato:** `{tag}+{commit}` (es. `0.0.1+abc1234`)

### Regole CRITICHE

#### ❌ NON FARE:
1. NO modificare versioni in `package.json`
2. NO modificare versioni in `.csproj`
3. NO hardcodare versioni
4. NO rimuovere `vite-version-plugin.ts`
5. NO rimuovere env vars injection nei workflow

#### ✅ FARE:
**Incrementare versione con Git tags:**
```bash
# Patch: 0.0.1 → 0.0.2
git tag v0.0.2 && git push origin v0.0.2

# Minor: 0.0.2 → 0.1.0
git tag v0.1.0 && git push origin v0.1.0

# Major: 0.1.0 → 1.0.0
git tag v1.0.0 && git push origin v1.0.0
```

### File Critici (NON modificare)

**Backend:**
- `Controllers/VersionController.cs` - Legge VERSION, GIT_COMMIT_SHA da env

**Frontend (tutti):**
- `vite-version-plugin.ts` - Inietta versione Git
- `vite.config.ts` - DEVE includere viteVersionPlugin()
- `src/vite-env.d.ts` - Tipi TypeScript
- `src/components/VersionInfo.tsx` - Mostra versioni

**GitHub Actions:**
- `.github/workflows/deploy-*.yml` - Step "Extract version info" OBBLIGATORIO

### Quando Creare Tag
✅ Feature completa
✅ Bugfix critico
✅ Breaking changes
✅ Prima merge in `main`

**Procedura:**
```bash
git status                                              # Verifica clean
git tag -a v0.1.0 -m "Release 0.1.0: Feature X"       # Crea tag
git push origin v0.1.0                                  # Push tag
```

### Troubleshooting

**Frontend mostra "0.0.1+dev":**
```bash
ls frontend/consumer-app/vite-version-plugin.ts      # Verifica esista
grep "viteVersionPlugin" vite.config.ts              # Verifica import
npm run build                                         # Rebuild
```

**Backend mostra "0.0.0+dev":**
```bash
git tag -l           # Verifica tags
git tag v0.0.1       # Crea tag se mancante
git push origin v0.0.1
```

## Code Review Checklist

- [ ] XML/JSDoc comments su metodi public
- [ ] Dependency injection corretta
- [ ] Nessun secret hardcoded
- [ ] Messaggi utente chiari
- [ ] No emoji in codice/messaggi
- [ ] Async/await per DB
- [ ] DateTime da DTO normalizzati a UTC prima di salvare su DB
- [ ] Loading states frontend
- [ ] Gestione errori user-friendly
- [ ] Versioning automatico NON modificato
- [ ] vite-version-plugin.ts presente
- [ ] GitHub Actions include "Extract version info"

## Testing Locale

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

- [PROJECT_GUIDELINES.md](../PROJECT_GUIDELINES.md) - Dettagli completi
- [SETUP.md](../SETUP.md) - Setup rapido
- [backend/CONFIGURATION.md](../backend/CONFIGURATION.md) - Configurazione
