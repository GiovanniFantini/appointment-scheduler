using System.Security.Cryptography;
using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

public sealed class PasswordResetTokenGenerator : IPasswordResetTokenGenerator
{
    public string Generate()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(rawBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}