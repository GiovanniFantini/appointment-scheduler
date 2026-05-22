namespace AppointmentScheduler.API.Tests.Controllers;

public class VersionControllerTests
{
    [Fact]
    public void GetVersion_UsesConfiguredBuildMetadata_WhenPresent()
    {
        var controller = CreateController(new Dictionary<string, string?>
        {
            ["VERSION"] = "1.2.3",
            ["GIT_COMMIT_SHA"] = "abc1234",
            ["BUILD_NUMBER"] = "42",
            ["BUILD_TIME"] = "2026-05-01T10:30:00Z"
        });

        var result = controller.GetVersion().Result;

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<VersionResponse>().Subject;
        payload.Version.Should().Be("1.2.3+abc1234");
        payload.Environment.Should().Be("Test");
        payload.ApiName.Should().Be("Appointment Scheduler API");
        DateTime.TryParse("2026-05-01T10:30:00Z", out var expectedBuildDate).Should().BeTrue();
        payload.BuildDate.Should().Be(expectedBuildDate);
    }

    [Fact]
    public void GetVersion_FallsBackToDefaults_WhenConfigurationIsMissing()
    {
        var controller = CreateController(new Dictionary<string, string?>());

        var result = controller.GetVersion().Result;

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<VersionResponse>().Subject;
        payload.Version.Should().StartWith("0.0.0+");
        payload.Environment.Should().Be("Test");
        payload.BuildDate.Should().Be(File.GetLastWriteTimeUtc(typeof(VersionController).Assembly.Location));
    }

    [Fact]
    public void GetVersion_FallsBackToAssemblyBuildDate_WhenBuildTimeCannotBeParsed()
    {
        var controller = CreateController(new Dictionary<string, string?>
        {
            ["VERSION"] = "2.0.0",
            ["GIT_COMMIT_SHA"] = "deadbee",
            ["BUILD_TIME"] = "not-a-date"
        });

        var result = controller.GetVersion().Result;

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<VersionResponse>().Subject;
        payload.Version.Should().Be("2.0.0+deadbee");
        payload.BuildDate.Should().Be(File.GetLastWriteTimeUtc(typeof(VersionController).Assembly.Location));
    }

    private static VersionController CreateController(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var environment = Mock.Of<IWebHostEnvironment>(env => env.EnvironmentName == "Test");
        return new VersionController(environment, configuration);
    }
}