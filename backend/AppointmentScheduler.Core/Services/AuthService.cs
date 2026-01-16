using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
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

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Merchant)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        if (!user.IsActive)
            return null;

        // Costruisci la lista di ruoli basata sui flags
        var roles = GetUserRoles(user);
        var token = GenerateJwtToken(user.Id, user.Email, roles, user.Merchant?.Id, user.Employee?.Id);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            IsAdmin = user.IsAdmin,
            IsConsumer = user.IsConsumer,
            IsMerchant = user.IsMerchant,
            IsEmployee = user.IsEmployee,
            MerchantId = user.Merchant?.Id,
            EmployeeId = user.Employee?.Id
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Verifica se l'email esiste già
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        // Verifica che BusinessName sia presente se vuole registrarsi come Merchant
        if (request.RegisterAsMerchant && string.IsNullOrWhiteSpace(request.BusinessName))
            throw new ArgumentException("BusinessName è obbligatorio per i Merchant");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            // Tutti partono come consumer di default
            IsConsumer = true,
            IsMerchant = request.RegisterAsMerchant,
            IsEmployee = false,
            IsAdmin = false,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Se si registra anche come Merchant, crea il profilo business
        if (request.RegisterAsMerchant && !string.IsNullOrEmpty(request.BusinessName))
        {
            var merchant = new Merchant
            {
                UserId = user.Id,
                BusinessName = request.BusinessName,
                VatNumber = request.VatNumber,
                IsApproved = false, // Deve essere approvato dall'admin
                User = user // Imposta la navigation property per evitare errori di salvataggio
            };

            _context.Merchants.Add(merchant);
            await _context.SaveChangesAsync();

            user.Merchant = merchant;
        }

        var roles = GetUserRoles(user);
        var token = GenerateJwtToken(user.Id, user.Email, roles, user.Merchant?.Id);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            IsAdmin = user.IsAdmin,
            IsConsumer = user.IsConsumer,
            IsMerchant = user.IsMerchant,
            IsEmployee = user.IsEmployee,
            MerchantId = user.Merchant?.Id
        };
    }

    public async Task<AuthResponse?> RegisterEmployeeAsync(EmployeeRegisterRequest request)
    {
        // Verifica che l'email corrisponda a un employee esistente
        var employee = await _context.Employees
            .Include(e => e.Merchant)
            .FirstOrDefaultAsync(e => e.Email == request.Email && e.IsActive);

        if (employee == null)
            throw new ArgumentException("Nessun dipendente trovato con questa email. Contatta il tuo datore di lavoro.");

        // Verifica che l'employee non abbia già un account
        if (employee.UserId.HasValue)
            throw new ArgumentException("Hai già un account registrato. Usa la funzione di login.");

        // Verifica se l'email esiste già nella tabella Users (non dovrebbe mai succedere, ma per sicurezza)
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new ArgumentException("Email già registrata nel sistema.");

        // Crea un nuovo User per l'employee
        var user = new User
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            PhoneNumber = employee.PhoneNumber,
            IsEmployee = true,
            IsConsumer = true, // Può anche fare booking come consumer
            IsMerchant = false,
            IsAdmin = false,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Collega l'employee al nuovo user
        employee.UserId = user.Id;
        employee.User = user;
        employee.UpdatedAt = DateTime.UtcNow;

        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

        // Genera il token con EmployeeId e MerchantId
        var roles = GetUserRoles(user);
        var token = GenerateJwtToken(user.Id, user.Email, roles, employee.MerchantId, employee.Id);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            IsAdmin = user.IsAdmin,
            IsConsumer = user.IsConsumer,
            IsMerchant = user.IsMerchant,
            IsEmployee = user.IsEmployee,
            EmployeeId = employee.Id,
            MerchantId = employee.MerchantId
        };
    }

    public string GenerateJwtToken(int userId, string email, List<string> roles, int? merchantId = null, int? employeeId = null)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Aggiungi un claim per ogni ruolo
        foreach (var role in roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
        }

        if (merchantId.HasValue)
        {
            claimsList.Add(new Claim("MerchantId", merchantId.Value.ToString()));
        }

        if (employeeId.HasValue)
        {
            claimsList.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
        }

        var claims = claimsList.ToArray();

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationMinutes"] ?? "1440")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private List<string> GetUserRoles(User user)
    {
        var roles = new List<string>();

        if (user.IsAdmin) roles.Add("Admin");
        if (user.IsConsumer) roles.Add("Consumer");
        if (user.IsMerchant) roles.Add("Merchant");
        if (user.IsEmployee) roles.Add("Employee");

        return roles;
    }

    private string HashPassword(string password)
    {
        // In produzione, usa BCrypt o Identity
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
