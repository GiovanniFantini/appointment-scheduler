using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Shared.DTOs;
using System.Reflection;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public VersionController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<VersionResponse> GetVersion()
    {
        // Read from environment variables (injected by CI/CD)
        var version = _configuration["VERSION"] ?? GetGitTag() ?? "0.0.0";
        var commit = _configuration["GIT_COMMIT_SHA"] ?? GetGitCommit() ?? "dev";
        var buildNumber = _configuration["BUILD_NUMBER"] ?? "local";
        var buildTimeStr = _configuration["BUILD_TIME"];

        DateTime buildDate;
        if (!string.IsNullOrEmpty(buildTimeStr) && DateTime.TryParse(buildTimeStr, out var parsedDate))
        {
            buildDate = parsedDate;
        }
        else
        {
            buildDate = GetAssemblyBuildDate();
        }

        // Format version string: v{version}+{commit} (e.g., v1.2.3+abc1234)
        var fullVersion = $"{version}+{commit}";

        var response = new VersionResponse
        {
            Version = fullVersion,
            Environment = _environment.EnvironmentName,
            BuildDate = buildDate,
            ApiName = "Appointment Scheduler API"
        };

        return Ok(response);
    }

    private static string? GetGitCommit()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetGitTag()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "describe --tags --abbrev=0",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // Remove 'v' prefix if present
            if (!string.IsNullOrEmpty(result) && result.StartsWith("v"))
            {
                result = result.Substring(1);
            }

            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime GetAssemblyBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;

        if (!string.IsNullOrEmpty(location) && global::System.IO.File.Exists(location))
        {
            return global::System.IO.File.GetLastWriteTimeUtc(location);
        }

        return DateTime.UtcNow;
    }
}
