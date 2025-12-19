# Project Guidelines - Appointment Scheduler

Questo documento contiene le linee guida e best practices per lo sviluppo del progetto.

## Principi Fondamentali

### 1. User-Friendly First
- L'esperienza utente e' prioritaria
- Interfacce intuitive e chiare
- Messaggi di errore comprensibili
- Feedback immediato sulle azioni
- Design responsive e accessibile

### 2. Dependency Injection
- Usare sempre la DI per gestire le dipendenze
- Registrare servizi in `Program.cs`
- Preferire interfacce per i servizi
- Lifetime appropriati: Scoped per DbContext, Singleton per configurazioni
- Evitare ServiceLocator pattern

### 3. Testabilita'
- Codice facilmente testabile in locale
- Configurazioni esterne (appsettings, env variables)
- Mock-friendly (interfacce, DI)
- Seed data per test locali
- Docker-compose per dipendenze (DB, cache, ecc.)

## Coding Standards

### C# Backend

#### Commenti e Documentazione
```csharp
/// <summary>
/// Autentica un utente e genera un token JWT
/// </summary>
/// <param name="request">Credenziali di login</param>
/// <returns>Token JWT e dati utente se successo, null altrimenti</returns>
public async Task<AuthResponse?> LoginAsync(LoginRequest request)
{
    // Implementazione
}
```

**Regole:**
- Commentare SEMPRE i metodi public con XML comments
- Evitare commenti ovvi (es: `// Incrementa i++`)
- Commentare solo logica complessa o non immediata
- Preferire nomi di variabili/metodi auto-esplicativi
- NO emoji nei commenti o messaggi di errore del codice

#### Dependency Injection
```csharp
// GIUSTO
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingService> _logger;

    public BookingService(ApplicationDbContext context, ILogger<BookingService> logger)
    {
        _context = context;
        _logger = logger;
    }
}

// SBAGLIATO - evitare new diretto
public class BookingService
{
    private readonly ApplicationDbContext _context = new ApplicationDbContext();
}
```

#### Gestione Errori
```csharp
// User-friendly: messaggi chiari
return BadRequest(new { message = "Email gia' registrata. Usa un'altra email o effettua il login." });

// EVITARE: messaggi tecnici
return BadRequest(new { error = "UNIQUE constraint failed: Users.Email" });
```

### TypeScript Frontend

#### Commenti
```typescript
/**
 * Hook per gestire l'autenticazione utente
 * @returns {user, login, logout, loading}
 */
export const useAuth = () => {
    // Implementazione
}
```

#### Gestione Stato
```typescript
// Preferire custom hooks per logica condivisa
const { user, loading } = useAuth();

// Messaggi utente chiari
setError("Email o password non corretti. Riprova.");
```

#### UI/UX
- Loading states per tutte le operazioni async
- Disabilitare pulsanti durante submit
- Validazione client-side prima di chiamare API
- Messaggi di successo/errore chiari

## Architettura

### Backend Structure
```
AppointmentScheduler.API/
├── Controllers/          # Endpoint HTTP
├── Program.cs           # DI registration
└── appsettings.json     # Configurazione

AppointmentScheduler.Core/
├── Services/            # Business logic (con interfacce)
├── Interfaces/          # Contratti servizi
└── Validators/          # Validazione input

AppointmentScheduler.Data/
├── ApplicationDbContext.cs
├── Repositories/        # Data access layer
└── Migrations/

AppointmentScheduler.Shared/
├── Models/              # Entita' database
├── DTOs/               # Data Transfer Objects
└── Enums/              # Enumerazioni
```

### Registrazione Servizi (Program.cs)
```csharp
// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services (Scoped per request HTTP)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();

// Utilities (Singleton per configurazioni)
builder.Services.AddSingleton<IEmailService, EmailService>();
```

## Testing Locale

### Setup Rapido
```bash
# 1. Database con Docker
docker run --name appointment-db -e POSTGRES_PASSWORD=dev123 -p 5432:5432 -d postgres

# 2. Configurazione locale
cp backend/AppointmentScheduler.API/appsettings.Local.json.example \
   backend/AppointmentScheduler.API/appsettings.Local.json

# 3. Migrazioni
cd backend/AppointmentScheduler.API
dotnet ef database update

# 4. Avvia backend
dotnet run

# 5. Avvia frontend (altra finestra)
cd frontend/consumer-app
npm install && npm run dev
```

### Seed Data
Creare un `DbInitializer` per popolare dati di test:
```csharp
public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        if (context.Users.Any()) return;

        // Crea utente admin di test
        var admin = new User { /* ... */ };
        context.Users.Add(admin);
        context.SaveChanges();
    }
}
```

## Git Workflow

### Commit Messages
```
feat: Aggiunge autenticazione JWT
fix: Corregge validazione email nella registrazione
refactor: Migliora gestione errori in BookingService
docs: Aggiorna README con istruzioni Docker
```

Formato: `<type>: <description>`
- `feat`: Nuova funzionalita'
- `fix`: Bug fix
- `refactor`: Refactoring
- `docs`: Documentazione
- `test`: Test
- `chore`: Manutenzione

### Branch Strategy
- `main`: Produzione
- `claude/*`: Feature branches (create automaticamente)
- Merge tramite Pull Request con review

## Security Best Practices

### Password
- SEMPRE usare BCrypt o PBKDF2
- Mai salvare password in chiaro
- Validare complessita' password

### JWT
- Secret key minimo 32 caratteri
- Expiration time appropriato (non troppo lungo)
- Refresh token per sessioni lunghe

### SQL Injection
- Usare SEMPRE parametrized queries (EF Core lo fa automaticamente)
- Mai concatenare stringhe per query

### Secrets
- Mai committare password/API keys
- Usare User Secrets in development
- Environment variables in production

## Performance

### Database
- Usare `async/await` per tutte le operazioni DB
- Eager loading con `.Include()` quando necessario
- Paginazione per liste grandi
- Indici su colonne frequentemente cercate

### Frontend
- Lazy loading per route
- Debounce per input search
- Caching per dati statici
- Ottimizzazione immagini

## Accessibilita'

- Label per tutti gli input
- Contrasto colori WCAG AA
- Navigazione da tastiera
- Screen reader friendly

## Code Review Checklist

Prima di committare, verifica:
- [ ] Tutti i metodi public hanno XML comments
- [ ] Nessun console.log dimenticato
- [ ] Messaggi di errore user-friendly
- [ ] Dependency injection usata correttamente
- [ ] Nessuna password/secret hardcoded
- [ ] Test locali funzionanti
- [ ] Codice formattato correttamente
- [ ] Nessuna emoji in testi/messaggi

## Risorse

- [ASP.NET Core Dependency Injection](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [Entity Framework Core Best Practices](https://docs.microsoft.com/ef/core/performance/)
- [React Best Practices](https://react.dev/learn)
- [Tailwind CSS](https://tailwindcss.com/docs)
