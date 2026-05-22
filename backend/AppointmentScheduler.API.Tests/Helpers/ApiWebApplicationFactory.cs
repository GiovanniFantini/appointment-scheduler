using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppointmentScheduler.API.Tests.Helpers;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["RUN_MIGRATIONS"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=appointment_scheduler_test;Username=test;Password=test",
                ["JwtSettings:SecretKey"] = "integration-tests-super-secret-key",
                ["JwtSettings:Issuer"] = "integration-tests",
                ["JwtSettings:Audience"] = "integration-tests",
                ["AzureBlobStorage:ContainerName"] = "hr-documents",
                ["AzureBlobStorage:SasTokenExpirationMinutes"] = "15",
                ["AzureCommunicationServices:ConnectionString"] = "endpoint=https://integration-tests.communication.azure.com/;accesskey=fake",
                ["AzureCommunicationServices:SenderAddress"] = "no-reply@example.test",
                ["AzureCommunicationServices:SenderDisplayName"] = "Integration Tests",
                ["AzureCommunicationServices:FrontendBaseUrls:Admin"] = "http://localhost:5175",
                ["AzureCommunicationServices:FrontendBaseUrls:Merchant"] = "http://localhost:5174",
                ["AzureCommunicationServices:FrontendBaseUrls:Employee"] = "http://localhost:5176",
                ["AzureCommunicationServices:FrontendBaseUrls:Default"] = "http://localhost:5173"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AzureBlobStorageOptions>();
            services.AddSingleton(new AzureBlobStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
                ContainerName = "hr-documents",
                SasTokenExpirationMinutes = 15
            });
        });
    }
}
