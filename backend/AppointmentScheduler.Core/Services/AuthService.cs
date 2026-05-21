using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class AuthService : IAuthService
{
    /// <summary>
    /// Lunghezza minima della password, applicata in modo uniforme a tutti i
    /// flussi (registrazione merchant/employee e reset). I form frontend usano
    /// lo stesso valore come attributo minLength, ma la validazione autorevole
    /// è qui: una chiamata diretta all'API non può aggirarla.
    /// </summary>
    public const int MinPasswordLength = 8;

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // ── Admin Login ────────────────────────────────────────────────────────
    public async Task<AuthResponse?> LoginAdminAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower()
                                   && u.AccountType == AccountType.Admin
                                   && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user.Id, user.Email, "Admin");
        return BuildAuthResponse(user, token);
    }

    // ── Merchant Login ─────────────────────────────────────────────────────
    public async Task<AuthResponse?> LoginMerchantAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower()
                                   && u.AccountType == AccountType.Merchant
                                   && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // L'azienda è operativa solo se attiva E approvata dall'admin. Un merchant
        // disattivato (o non ancora approvato) può autenticarsi ma non operare:
        // niente claim di approvazione → il frontend mostra la schermata di attesa.
        var merchantOperational = (user.Merchant?.IsActive ?? false)
                                  && (user.Merchant?.IsApproved ?? false);

        var allFeatures = Enum.GetValues<MerchantFeature>().Select(f => f.ToString()).ToList();
        var featureLevels = BuildMerchantFeatureLevels();
        var token = GenerateJwtToken(user.Id, user.Email, "Merchant", user.Merchant?.Id,
            features: allFeatures, featureLevels: featureLevels.Select(kv => $"{kv.Key}:{kv.Value}").ToList(),
            merchantApproved: merchantOperational);
        var response = BuildAuthResponse(user, token);
        response.MerchantId = user.Merchant?.Id;
        response.CompanyName = user.Merchant?.CompanyName;
        response.IsApproved = merchantOperational;
        response.ActiveFeatures = allFeatures;
        response.FeatureLevels = featureLevels;
        return response;
    }

    // ── Merchant Register ──────────────────────────────────────────────────
    public async Task<AuthResponse?> RegisterMerchantAsync(RegisterMerchantRequest request)
    {
        ValidatePassword(request.Password);

        var email = request.Email.ToLower();
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return null;

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            AccountType = AccountType.Merchant,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Crea profilo merchant (richiede approvazione admin)
        var merchant = new Merchant
        {
            UserId = user.Id,
            CompanyName = request.CompanyName,
            VatNumber = request.VatNumber,
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Phone = request.BusinessPhone,
            BusinessEmail = request.BusinessEmail ?? email,
            IsApproved = false,
            IsActive = true
        };

        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        // Crea la filiale HQ di default (sede principale).
        var headquarters = new MerchantBranch
        {
            MerchantId = merchant.Id,
            Name = string.IsNullOrWhiteSpace(request.CompanyName) ? "Sede principale" : request.CompanyName,
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Phone = request.BusinessPhone,
            IsHeadquarters = true,
            IsActive = true
        };
        _context.MerchantBranches.Add(headquarters);
        await _context.SaveChangesAsync();

        // Crea il ruolo "Responsabile App" con tutte le feature attive
        var defaultRole = new MerchantRole
        {
            MerchantId = merchant.Id,
            Name = "Responsabile App",
            IsDefault = true
        };
        _context.MerchantRoles.Add(defaultRole);
        await _context.SaveChangesAsync();

        var allFeatures = Enum.GetValues<MerchantFeature>();
        foreach (var feature in allFeatures)
        {
            _context.RoleFeatures.Add(new RoleFeature
            {
                RoleId = defaultRole.Id,
                Feature = feature,
                IsEnabled = true,
                // Il ruolo predefinito ha pieni poteri: livello Manager sul Magazzino.
                AccessLevel = feature == MerchantFeature.Magazzino
                    ? FeatureAccessLevel.Manager
                    : null
            });
        }
        await _context.SaveChangesAsync();

        // Crea l'employee owner (il responsabile/CEO) e lo associa
        var ownerEmployee = new Employee
        {
            UserId = user.Id,
            Email = email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };
        _context.Employees.Add(ownerEmployee);
        await _context.SaveChangesAsync();

        _context.EmployeeMemberships.Add(new EmployeeMembership
        {
            EmployeeId = ownerEmployee.Id,
            MerchantId = merchant.Id,
            RoleId = defaultRole.Id,
            HomeBranchId = headquarters.Id,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var featureNames = allFeatures.Select(f => f.ToString()).ToList();
        var featureLevels = BuildMerchantFeatureLevels();
        // Subito dopo la registrazione il merchant non è approvato: niente claim.
        var token = GenerateJwtToken(user.Id, user.Email, "Merchant", merchant.Id,
            features: featureNames, featureLevels: featureLevels.Select(kv => $"{kv.Key}:{kv.Value}").ToList(),
            merchantApproved: merchant.IsApproved);
        var response = BuildAuthResponse(user, token);
        response.MerchantId = merchant.Id;
        response.CompanyName = merchant.CompanyName;
        response.IsApproved = merchant.IsApproved; // false subito dopo la registrazione
        response.ActiveFeatures = featureNames;
        response.FeatureLevels = featureLevels;
        return response;
    }

    // ── Employee Login ─────────────────────────────────────────────────────
    public async Task<AuthResponse?> LoginEmployeeAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Memberships)
                    .ThenInclude(m => m.Merchant)
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Memberships)
                    .ThenInclude(m => m.Role)
                        .ThenInclude(r => r.Features)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower()
                                   && (u.AccountType == AccountType.Employee || u.AccountType == AccountType.Merchant)
                                   && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var employee = user.Employee;
        if (employee == null) return null;

        // Token base senza company — frontend mostra select-company
        var token = GenerateJwtToken(user.Id, user.Email, "Employee", employeeId: employee.Id);
        var response = BuildAuthResponse(user, token);
        response.EmployeeId = employee.Id;

        // Popola lista aziende disponibili. Un'azienda non ancora approvata
        // dall'admin non è operativa: l'employee non deve poterla selezionare.
        response.Companies = employee.Memberships
            .Where(m => m.IsActive && m.Merchant.IsActive && m.Merchant.IsApproved)
            .Select(m => new EmployeeCompanyDto
            {
                MerchantId = m.MerchantId,
                CompanyName = m.Merchant.CompanyName,
                City = m.Merchant.City,
                RoleId = m.RoleId,
                RoleName = m.Role.Name
            }).ToList();

        // Garantisce che Companies sia sempre presente (anche se vuoto)
        if (response.Companies == null)
            response.Companies = new List<EmployeeCompanyDto>();

        // Se ha una sola azienda, auto-seleziona
        if (response.Companies.Count == 1)
        {
            return await SelectCompanyAsync(user.Id, response.Companies[0].MerchantId);
        }

        return response;
    }

    // ── Employee Register ──────────────────────────────────────────────────
    public async Task<AuthResponse?> RegisterEmployeeAsync(EmployeeRegisterRequest request)
    {
        ValidatePassword(request.Password);

        var email = request.Email.ToLower();

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return null;

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            AccountType = AccountType.Employee,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Controlla se esiste già un Employee pre-creato dal merchant con questa email
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email && e.UserId == null);

        if (employee != null)
        {
            // Collega l'Employee esistente (con le sue Memberships) al nuovo User
            employee.UserId = user.Id;
            employee.FirstName = request.FirstName;
            employee.LastName = request.LastName;
            employee.IsActive = true;
        }
        else
        {
            // Nessun Employee pre-caricato: crea uno nuovo
            employee = new Employee
            {
                UserId = user.Id,
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };
            _context.Employees.Add(employee);
        }
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user.Id, user.Email, "Employee", employeeId: employee.Id);
        return BuildAuthResponse(user, token);
    }

    // ── Select Company (Employee) ──────────────────────────────────────────
    public async Task<AuthResponse?> SelectCompanyAsync(int userId, int merchantId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || (user.AccountType != AccountType.Employee && user.AccountType != AccountType.Merchant)) return null;

        var employee = await _context.Employees
            .Include(e => e.Memberships)
                .ThenInclude(m => m.Merchant)
            .Include(e => e.Memberships)
                .ThenInclude(m => m.Role)
                    .ThenInclude(r => r.Features)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

        if (employee == null) return null;

        var membership = employee.Memberships
            .FirstOrDefault(m => m.MerchantId == merchantId && m.IsActive);

        if (membership == null) return null;

        // L'azienda deve essere operativa: attiva e approvata dall'admin.
        // Un employee non può lavorare per un merchant non ancora approvato.
        if (!membership.Merchant.IsActive || !membership.Merchant.IsApproved)
            return null;

        var enabledFeatures = membership.Role.Features
            .Where(f => f.IsEnabled)
            .ToList();

        var features = enabledFeatures
            .Select(f => f.Feature.ToString())
            .ToList();

        // Livello di accesso per feature. Significativo solo dove valorizzato
        // (Magazzino): una feature abilitata senza livello è trattata come ReadOnly.
        var featureLevels = enabledFeatures
            .Where(f => f.AccessLevel.HasValue)
            .ToDictionary(
                f => f.Feature.ToString(),
                f => f.AccessLevel!.Value.ToString());

        var token = GenerateJwtToken(userId, user.Email, "Employee",
            merchantId: merchantId, employeeId: employee.Id, features: features,
            featureLevels: featureLevels.Select(kv => $"{kv.Key}:{kv.Value}").ToList());

        var response = BuildAuthResponse(user, token);
        response.EmployeeId = employee.Id;
        response.MerchantId = merchantId;
        response.ActiveFeatures = features;
        response.FeatureLevels = featureLevels;
        return response;
    }

    // ── Feature Levels ─────────────────────────────────────────────────────
    /// <summary>
    /// Livelli di accesso del Merchant: il merchant è configuratore con pieni
    /// poteri, quindi ha sempre il livello massimo (Manager) sulle feature che
    /// usano i livelli (oggi solo Magazzino).
    /// </summary>
    private static Dictionary<string, string> BuildMerchantFeatureLevels()
        => new()
        {
            [MerchantFeature.Magazzino.ToString()] = FeatureAccessLevel.Manager.ToString(),
        };

    // ── JWT Generation ─────────────────────────────────────────────────────
    public string GenerateJwtToken(int userId, string email, string role,
        int? merchantId = null, int? employeeId = null, List<string>? features = null,
        List<string>? featureLevels = null, bool merchantApproved = false)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        if (merchantId.HasValue)
            claims.Add(new Claim("MerchantId", merchantId.Value.ToString()));

        if (employeeId.HasValue)
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));

        // Presente solo se l'azienda è approvata dall'admin. La policy
        // "ApprovedMerchantOnly" lo richiede per i merchant: un token emesso
        // prima dell'approvazione non lo contiene e resta valido solo per le
        // rotte non gated (login, profilo) finché l'utente non rifà login.
        if (merchantApproved)
            claims.Add(new Claim("MerchantApproved", "true"));

        if (features != null)
            foreach (var feature in features)
                claims.Add(new Claim("Feature", feature));

        // Claim aggiuntivo, separato da "Feature" per non rompere i consumatori
        // esistenti. Valore nel formato "<Feature>:<Level>" (es. "Magazzino:Manager").
        if (featureLevels != null)
            foreach (var featureLevel in featureLevels)
                claims.Add(new Claim("FeatureLevel", featureLevel));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationMinutes"] ?? "1440")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida la password in fase di registrazione. Lancia ArgumentException
    /// (mappata a 400 dal controller) se non rispetta la lunghezza minima.
    /// </summary>
    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            throw new ArgumentException(
                $"La password deve contenere almeno {MinPasswordLength} caratteri.");
    }

    private static AuthResponse BuildAuthResponse(User user, string token) => new()
    {
        Token = token,
        UserId = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        AccountType = user.AccountType,
        Companies = new List<EmployeeCompanyDto>() // garantisce sempre la proprietà
    };
}
