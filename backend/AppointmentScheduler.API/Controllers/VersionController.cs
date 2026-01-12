using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Shared.DTOs;
using System.Reflection;

namespace AppointmentScheduler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public VersionController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public ActionResult<VersionResponse> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        // Get build date from assembly
        var buildDate = GetBuildDate(assembly);

        var response = new VersionResponse
        {
            Version = version,
            Environment = _environment.EnvironmentName,
            BuildDate = buildDate,
            ApiName = "Appointment Scheduler API"
        };

        return Ok(response);
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        // Try to get build date from assembly metadata
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute != null && DateTime.TryParse(attribute.InformationalVersion, out var date))
        {
            return date;
        }

        // Fallback: use file creation time
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            return File.GetLastWriteTime(location);
        }

        return DateTime.UtcNow;
    }
}
