using System.Security.Principal;

namespace AppointmentScheduler.API.Tests.Helpers;

internal static class ControllerTestExtensions
{
    public static TController WithUser<TController>(this TController controller, params Claim[] claims)
        where TController : ControllerBase
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    public static T? GetValue<T>(this IActionResult result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.Value.Should().BeOfType<T>().Subject,
            _ => throw new Xunit.Sdk.XunitException($"Expected an ObjectResult carrying {typeof(T).Name}, got {result.GetType().Name}.")
        };
    }

    public static string? GetAnonymousString(this IActionResult result, string propertyName)
    {
        return GetAnonymousProperty(result, propertyName) as string;
    }

    public static bool? GetAnonymousBool(this IActionResult result, string propertyName)
    {
        return GetAnonymousProperty(result, propertyName) as bool?;
    }

    public static int? GetAnonymousInt(this IActionResult result, string propertyName)
    {
        return GetAnonymousProperty(result, propertyName) as int?;
    }

    private static object? GetAnonymousProperty(IActionResult result, string propertyName)
    {
        var objectResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.Value.Should().NotBeNull();

        var property = objectResult.Value!.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        property.Should().NotBeNull($"the result payload should expose a '{propertyName}' property");
        return property!.GetValue(objectResult.Value);
    }
}