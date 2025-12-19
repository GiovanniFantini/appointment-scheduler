# Claude Instructions - Appointment Scheduler Project

Queste sono le istruzioni permanenti per lo sviluppo di questo progetto. Seguile sempre quando lavori su questo codebase.

## Stack Tecnologico

- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core + PostgreSQL
- **Frontend:** React 18 + TypeScript + Vite + Tailwind CSS
- **Auth:** JWT Bearer Token
- **Database:** PostgreSQL (con supporto SQL Server opzionale)

## Principi di Sviluppo

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

## Code Review Checklist

Prima di ogni commit verificare:
- [ ] XML/JSDoc comments su metodi public
- [ ] Dependency injection usata correttamente
- [ ] Nessun secret hardcoded
- [ ] Messaggi utente chiari (no tecnicismi)
- [ ] No emoji in codice/messaggi app
- [ ] Async/await per operazioni DB
- [ ] Loading states nel frontend
- [ ] Gestione errori user-friendly

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
