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

        var allFeatures = Enum.GetValues<MerchantFeature>().Select(f => f.ToString()).ToList();
        var token = GenerateJwtToken(user.Id, user.Email, "Merchant", user.Merchant?.Id);
        var response = BuildAuthResponse(user, token);
        response.MerchantId = user.Merchant?.Id;
        response.CompanyName = user.Merchant?.CompanyName;
        response.ActiveFeatures = allFeatures;
        return response;
    }

    // ── Merchant Register ──────────────────────────────────────────────────
    public async Task<AuthResponse?> RegisterMerchantAsync(RegisterMerchantRequest request)
    {
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
                IsEnabled = true
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
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user.Id, user.Email, "Merchant", merchant.Id);
        var response = BuildAuthResponse(user, token);
        response.MerchantId = merchant.Id;
        response.CompanyName = merchant.CompanyName;
        response.ActiveFeatures = allFeatures.Select(f => f.ToString()).ToList();
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

        // Popola lista aziende disponibili
        response.Companies = employee.Memberships
            .Where(m => m.IsActive && m.Merchant.IsActive)
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
                .ThenInclude(m => m.Role)
                    .ThenInclude(r => r.Features)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

        if (employee == null) return null;

        var membership = employee.Memberships
            .FirstOrDefault(m => m.MerchantId == merchantId && m.IsActive);

        if (membership == null) return null;

        var features = membership.Role.Features
            .Where(f => f.IsEnabled)
            .Select(f => f.Feature.ToString())
            .ToList();

        var token = GenerateJwtToken(userId, user.Email, "Employee",
            merchantId: merchantId, employeeId: employee.Id, features: features);

        var response = BuildAuthResponse(user, token);
        response.EmployeeId = employee.Id;
        response.MerchantId = merchantId;
        response.ActiveFeatures = features;
        return response;
    }

    // ── JWT Generation ─────────────────────────────────────────────────────
    public string GenerateJwtToken(int userId, string email, string role,
        int? merchantId = null, int? employeeId = null, List<string>? features = null)
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

        if (features != null)
            foreach (var feature in features)
                claims.Add(new Claim("Feature", feature));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationMinutes"] ?? "1440")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
