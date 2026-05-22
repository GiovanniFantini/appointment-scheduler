using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

public sealed class FrontendUrlOptions
{
    public string Admin { get; set; } = "http://localhost:5175";
    public string Merchant { get; set; } = "http://localhost:5174";
    public string Employee { get; set; } = "http://localhost:5176";
    public string Default { get; set; } = "http://localhost:5173";

    public string GetBaseUrl(AccountType accountType) => accountType switch
    {
        AccountType.Admin => Admin,
        AccountType.Merchant => Merchant,
        AccountType.Employee => Employee,
        _ => Default
    };
}