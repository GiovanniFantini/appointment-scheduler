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
    private readonly JwtTokenOptions _jwtOptions = new()
    {
        SecretKey = "super-secret-key-super-secret-key-12345",
        Issuer = "tests",
        Audience = "tests-audience",
        ExpirationMinutes = 90
    };

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

        _passwordHasher.Setup(x => x.HashPassword("secret123")).Returns("hash");
        var service = CreateService(context.Object);

        var result = await service.RegisterMerchantAsync(new RegisterMerchantRequest
        {
            Email = "merchant@example.com",
            Password = "secret123",
            FirstName = "Mario",
            LastName = "Rossi",
            CompanyName = "Contoso"
        });

        result.Should().NotBeNull();
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.AppManager);
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.InternalBase);
        roles.Select(r => r.Name).Should().Contain(SystemRoleNames.ExternalBase);
    }

    private AuthService CreateService(IApplicationDbContext context)
    {
        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc));
        return new AuthService(context, _passwordHasher.Object, _clock.Object, _jwtOptions);
    }
}