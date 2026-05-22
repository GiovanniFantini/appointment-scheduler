using System.Net;
using System.Text;
using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AppointmentScheduler.API.Tests;

public class DiRegressionTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public DiRegressionTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ServiceProvider_Resolves_AllApplicationServices()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var serviceTypes = new[]
        {
            typeof(JwtTokenOptions),
            typeof(FrontendUrlOptions),
            typeof(AzureBlobStorageOptions),
            typeof(AzureEmailOptions),
            typeof(IApplicationDbContext),
            typeof(IUtcClock),
            typeof(IWallClock),
            typeof(IPasswordHasher),
            typeof(IPasswordResetTokenGenerator),
            typeof(IAuthService),
            typeof(IShiftConflictValidator),
            typeof(IEventService),
            typeof(IMerchantRoleService),
            typeof(INotificationService),
            typeof(IMerchantService),
            typeof(IEmployeeService),
            typeof(IEmployeeRequestService),
            typeof(ISkillService),
            typeof(IBranchService),
            typeof(ITimeClockService),
            typeof(IInventoryService),
            typeof(IEmployeeInventoryService),
            typeof(ISupplierService),
            typeof(IPurchaseOrderService),
            typeof(IInventoryReportingService),
            typeof(IFileStorageService),
            typeof(IHRDocumentService),
            typeof(IEmailService),
            typeof(IPasswordResetService)
        };

        foreach (var serviceType in serviceTypes)
        {
            var service = provider.GetRequiredService(serviceType);
            service.Should().NotBeNull($"{serviceType.Name} should be resolvable from DI");
        }
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ReturnsUnauthorized_WithoutToken()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/employee/select-company/7", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ReturnsOk_WithAuthenticatedEmployee()
    {
        var authService = new Mock<IAuthService>();
        authService
            .Setup(service => service.SelectCompanyAsync(123, 7))
            .ReturnsAsync(new AuthResponse { Token = "jwt-test-token" });

        using var client = _factory.WithWebHostBuilder(webHostBuilder =>
        {
            webHostBuilder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddScoped(_ => authService.Object);

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        }).CreateClient();

        var response = await client.PostAsync(
            "/api/auth/employee/select-company/7",
            new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authService.Verify(service => service.SelectCompanyAsync(123, 7), Times.Once);
    }
}
