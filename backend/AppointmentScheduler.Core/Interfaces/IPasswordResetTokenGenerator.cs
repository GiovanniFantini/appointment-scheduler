namespace AppointmentScheduler.Core.Interfaces;

public interface IPasswordResetTokenGenerator
{
    string Generate();
}