using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Helpers;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class AuthServiceTests
{
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUtcClock> _clock = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly JwtTokenOptions _jwtOptions = new()
    {
        SecretKey = "super-secret-key-super-secret-key-12345",
        Issuer = "tests",
        Audience = "tests-audience",
        ExpirationMinutes = 90
    };

    // Password che soddisfa la policy (≥12 char + maiuscola + minuscola + cifra).
    private const string StrongPassword = "Secret12345!";

    [Fact]
    public async Task LoginAdminAsync_ReturnsNull_WhenPasswordDoesNotMatch()
    {
        var users = new List<User>
        {
            new() { Id = 1, Email = "admin@example.com", PasswordHash = "hash", AccountType = AccountType.Admin, IsActive = true }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.Verify("wrong-password", "hash")).Returns(false);
        var service = CreateService(context.Object);

        var result = await service.LoginAdminAsync(new LoginRequest { Email = "admin@example.com", Password = "wrong-password" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAdminAsync_ReturnsAuthResponse_WhenCredentialsAreValid()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var users = new List<User>
        {
            new() { Id = 7, Email = "admin@example.com", PasswordHash = "hash", FirstName = "Ada", LastName = "Admin", AccountType = AccountType.Admin, IsActive = true }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.Verify("secret123", "hash")).Returns(true);
        var service = CreateService(context.Object);

        var result = await service.LoginAdminAsync(new LoginRequest { Email = "admin@example.com", Password = "secret123" });

        result.Should().NotBeNull();
        result!.UserId.Should().Be(7);
        result.Email.Should().Be("admin@example.com");
        result.AccountType.Should().Be(AccountType.Admin);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "7");
        token.ValidTo.Should().Be(now.AddMinutes(90));
    }

    [Fact]
    public async Task LoginMerchantAsync_ReturnsApprovedMerchantWithManagerFeatureLevels()
    {
        var merchant = new Merchant { Id = 12, CompanyName = "Contoso", IsActive = true, IsApproved = true };
        var users = new List<User>
        {
            new()
            {
                Id = 3,
                Email = "merchant@example.com",
                PasswordHash = "hash",
                AccountType = AccountType.Merchant,
                IsActive = true,
                Merchant = merchant
            }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.Verify("secret123", "hash")).Returns(true);
        var service = CreateService(context.Object);

        var result = await service.LoginMerchantAsync(new LoginRequest { Email = "merchant@example.com", Password = "secret123" });

        result.Should().NotBeNull();
        result!.MerchantId.Should().Be(12);
        result.CompanyName.Should().Be("Contoso");
        result.IsApproved.Should().BeTrue();
        result.ActiveFeatures.Should().Contain(nameof(MerchantFeature.Magazzino));
        result.FeatureLevels.Should().ContainKey(nameof(MerchantFeature.Calendario)).WhoseValue.Should().Be(nameof(FeatureAccessLevel.Manager));
        result.FeatureLevels.Should().ContainKey(nameof(MerchantFeature.Magazzino)).WhoseValue.Should().Be(nameof(FeatureAccessLevel.Manager));
        result.FeatureLevels.Should().ContainKey(nameof(MerchantFeature.Documenti)).WhoseValue.Should().Be(nameof(FeatureAccessLevel.Manager));
        result.FeatureLevels.Should().ContainKey(nameof(MerchantFeature.Richieste)).WhoseValue.Should().Be(nameof(FeatureAccessLevel.Manager));
        result.FeatureLevels.Should().ContainKey(nameof(MerchantFeature.Timbratura)).WhoseValue.Should().Be(nameof(FeatureAccessLevel.Manager));
    }

    [Fact]
    public async Task SelectCompanyAsync_ReturnsNull_WhenMerchantIsNotOperational()
    {
        var role = new MerchantRole { Id = 4, Name = "Operatore" };
        var merchant = new Merchant { Id = 12, CompanyName = "Contoso", IsApproved = false, IsActive = true };
        var membership = new EmployeeMembership { MerchantId = 12, RoleId = 4, IsActive = true, Merchant = merchant, Role = role };
        var employee = new Employee { Id = 9, UserId = 3, Email = "employee@example.com", IsActive = true, Memberships = new List<EmployeeMembership> { membership } };
        var users = new List<User> { new() { Id = 3, Email = "employee@example.com", AccountType = AccountType.Employee, IsActive = true } };
        var employees = new List<Employee> { employee };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .WithSet(x => x.Employees, employees, employee => [employee.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.SelectCompanyAsync(3, 12);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SelectCompanyAsync_ReturnsFeaturesAndLevels_FromMembershipRole()
    {
        var role = new MerchantRole
        {
            Id = 4,
            Name = "Manager",
            Features = new List<RoleFeature>
            {
                new() { Feature = MerchantFeature.Magazzino, IsEnabled = true, AccessLevel = FeatureAccessLevel.Manager },
                new() { Feature = MerchantFeature.Documenti, IsEnabled = true, AccessLevel = FeatureAccessLevel.Operator },
                new() { Feature = MerchantFeature.Filiali, IsEnabled = true }
            }
        };
        var merchant = new Merchant { Id = 12, CompanyName = "Contoso", City = "Milan", IsApproved = true, IsActive = true };
        var membership = new EmployeeMembership { MerchantId = 12, RoleId = 4, IsActive = true, Merchant = merchant, Role = role };
        var employee = new Employee { Id = 9, UserId = 3, Email = "employee@example.com", IsActive = true, Memberships = new List<EmployeeMembership> { membership } };
        var users = new List<User> { new() { Id = 3, Email = "employee@example.com", FirstName = "Eva", LastName = "Employee", AccountType = AccountType.Employee, IsActive = true } };
        var employees = new List<Employee> { employee };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .WithSet(x => x.Employees, employees, employee => [employee.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.SelectCompanyAsync(3, 12);

        result.Should().NotBeNull();
        result!.EmployeeId.Should().Be(9);
        result.MerchantId.Should().Be(12);
        result.ActiveFeatures.Should().BeEquivalentTo(new[] { "Magazzino", "Documenti", "Filiali" });
        result.FeatureLevels.Should().Contain(new KeyValuePair<string, string>("Magazzino", "Manager"));
        result.FeatureLevels.Should().Contain(new KeyValuePair<string, string>("Documenti", "Operator"));
        result.FeatureLevels.Should().NotContainKey("Filiali");
    }

    [Fact]
    public async Task RegisterMerchantAsync_CreatesSystemRoles_ForAppManagerInternalAndExternalBase()
    {
        var users = new List<User>();
        var merchants = new List<Merchant>();
        var branches = new List<MerchantBranch>();
        var roles = new List<MerchantRole>();
        var roleFeatures = new List<RoleFeature>();
        var employees = new List<Employee>();
        var memberships = new List<EmployeeMembership>();

        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .WithSet(x => x.Merchants, merchants, merchant => [merchant.Id])
            .WithSet(x => x.MerchantBranches, branches, branch => [branch.Id])
            .WithSet(x => x.MerchantRoles, roles, role => [role.Id])
            .WithSet(x => x.RoleFeatures, roleFeatures, feature => [feature.Id])
            .WithSet(x => x.Employees, employees, employee => [employee.Id])
            .WithSet(x => x.EmployeeMemberships, memberships, membership => [membership.Id])
            .Build();

        _passwordHasher.Setup(x => x.HashPassword("Secret12345!")).Returns("hash");
        var service = CreateService(context.Object);

        var result = await service.RegisterMerchantAsync(new RegisterMerchantRequest
        {
            Email = "merchant@example.com",
            // Rispetta la policy: ≥12 char, maiuscola, minuscola, cifra.
            Password = "Secret12345!",
            FirstName = "Mario",
            LastName = "Rossi",
            CompanyName = "Contoso"
        });

        result.Should().NotBeNull();
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.AppManager);
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.InternalBase);
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.ExternalBase);
    }

    // ─── ValidatePassword ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]                  // vuota
    [InlineData("Short1!")]           // 7 char
    [InlineData("Aa1Aa1Aa1A")]        // 10 char con tutta la complessità ma sotto soglia
    public void ValidatePassword_Throws_WhenTooShort(string password)
    {
        var act = () => AuthService.ValidatePassword(password);
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*almeno {AuthService.MinPasswordLength} caratteri*");
    }

    [Theory]
    [InlineData("alllowercase1")]    // manca maiuscola
    [InlineData("ALLUPPERCASE1")]    // manca minuscola
    [InlineData("NoDigitsHereAB")]   // manca cifra
    public void ValidatePassword_Throws_WhenComplexityMissing(string password)
    {
        var act = () => AuthService.ValidatePassword(password);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*maiuscola*minuscola*cifra*");
    }

    [Theory]
    [InlineData("Secret12345!")]             // 12 char con tutto
    [InlineData("MySuperSecure2026Pwd")]      // più lunga
    [InlineData("Aa1Aa1Aa1Aa1")]             // esattamente al minimo
    public void ValidatePassword_DoesNotThrow_WhenPolicyMet(string password)
    {
        var act = () => AuthService.ValidatePassword(password);
        act.Should().NotThrow();
    }

    [Fact]
    public void MinPasswordLength_IsTwelve()
    {
        // Sentinella: l'unica costante deve restare allineata col DTO MinLength
        // e con la regex del frontend. Se cambia, vanno aggiornati i tre punti.
        AuthService.MinPasswordLength.Should().Be(12);
    }

    // ─── Anti-timing su login ─────────────────────────────────────────────

    [Fact]
    public async Task LoginAdminAsync_InvokesPasswordVerify_EvenWhenUserDoesNotExist()
    {
        // Mitigazione timing attack: anche con utente inesistente il servizio
        // chiama BCrypt.Verify contro un hash "civetta" così che il tempo di
        // risposta resti paragonabile al caso reale.
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("dummy-hash");
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var service = CreateService(context.Object);

        var result = await service.LoginAdminAsync(new LoginRequest { Email = "ghost@example.com", Password = "anything12345!" });

        result.Should().BeNull();
        // Punto chiave del test: Verify viene chiamato anche se l'utente non esiste.
        _passwordHasher.Verify(x => x.Verify("anything12345!", It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoginMerchantAsync_InvokesPasswordVerify_EvenWhenUserDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("dummy-hash");
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var service = CreateService(context.Object);

        var result = await service.LoginMerchantAsync(new LoginRequest { Email = "ghost@example.com", Password = "anything12345!" });

        result.Should().BeNull();
        _passwordHasher.Verify(x => x.Verify("anything12345!", It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoginEmployeeAsync_InvokesPasswordVerify_EvenWhenUserDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .WithSet(x => x.Employees, new List<Employee>(), employee => [employee.Id])
            .Build();
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("dummy-hash");
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var service = CreateService(context.Object);

        var result = await service.LoginEmployeeAsync(new LoginRequest { Email = "ghost@example.com", Password = "anything12345!" });

        result.Should().BeNull();
        _passwordHasher.Verify(x => x.Verify("anything12345!", It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoginAdminAsync_DoesNotReturnAuthResponse_WhenUserNullEvenIfVerifyTrue()
    {
        // Patologico: anche se l'hasher mockato dovesse ritornare true contro
        // il dummy hash, l'utente non esiste → la risposta deve restare null.
        // Verifica che VerifyPasswordConstantTime AND user != null e ok.
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .Build();
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("dummy-hash");
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var service = CreateService(context.Object);

        var result = await service.LoginAdminAsync(new LoginRequest { Email = "ghost@example.com", Password = "anything12345!" });

        result.Should().BeNull();
    }

    // ─── Anti-enumeration nel register ────────────────────────────────────

    [Fact]
    public async Task RegisterMerchantAsync_ReturnsNullAndNotifiesExistingUser_WhenEmailIsTaken()
    {
        var existing = new User
        {
            Id = 42,
            Email = "merchant@example.com",
            FirstName = "Mario",
            LastName = "Rossi",
            AccountType = AccountType.Merchant,
            IsActive = true
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { existing }, user => [user.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.RegisterMerchantAsync(new RegisterMerchantRequest
        {
            Email = "merchant@example.com",
            Password = StrongPassword,
            FirstName = "Impostor",
            LastName = "Account",
            CompanyName = "Fake S.r.l."
        });

        result.Should().BeNull();
        _emailService.Verify(
            x => x.SendAsync(
                "merchant@example.com",
                "Mario Rossi",
                "Tentativo di registrazione con la tua email",
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterEmployeeAsync_ReturnsNullAndNotifiesExistingUser_WhenEmailIsTaken()
    {
        var existing = new User
        {
            Id = 7,
            Email = "employee@example.com",
            FirstName = "Eva",
            LastName = "Employee",
            AccountType = AccountType.Employee,
            IsActive = true
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { existing }, user => [user.Id])
            .WithSet(x => x.Employees, new List<Employee>(), employee => [employee.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.RegisterEmployeeAsync(new EmployeeRegisterRequest
        {
            FirstName = "Impostor",
            LastName = "Account",
            Email = "employee@example.com",
            Password = StrongPassword
        });

        result.Should().BeNull();
        _emailService.Verify(
            x => x.SendAsync(
                "employee@example.com",
                "Eva Employee",
                "Tentativo di registrazione con la tua email",
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterMerchantAsync_DoesNotThrow_WhenEmailServiceFails()
    {
        // L'invio della notifica è best-effort: un errore di IEmailService non
        // deve risalire al chiamante (altrimenti l'attaccante saprebbe per
        // timing/errore che l'email è già usata).
        var existing = new User
        {
            Id = 42,
            Email = "merchant@example.com",
            FirstName = "Mario",
            LastName = "Rossi",
            AccountType = AccountType.Merchant
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { existing }, user => [user.Id])
            .Build();
        _emailService
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var service = CreateService(context.Object);

        var act = () => service.RegisterMerchantAsync(new RegisterMerchantRequest
        {
            Email = "merchant@example.com",
            Password = StrongPassword,
            FirstName = "Impostor",
            LastName = "Account",
            CompanyName = "Fake S.r.l."
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RegisterMerchantAsync_ThrowsArgumentException_WhenPasswordTooWeak()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User>(), user => [user.Id])
            .Build();
        var service = CreateService(context.Object);

        var act = () => service.RegisterMerchantAsync(new RegisterMerchantRequest
        {
            Email = "new@example.com",
            Password = "short",  // troppo corta
            FirstName = "Nuovo",
            LastName = "Merchant",
            CompanyName = "Acme"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private AuthService CreateService(IApplicationDbContext context)
    {
        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc));
        return new AuthService(
            context,
            _passwordHasher.Object,
            _clock.Object,
            _jwtOptions,
            _emailService.Object,
            logger: null);
    }
}