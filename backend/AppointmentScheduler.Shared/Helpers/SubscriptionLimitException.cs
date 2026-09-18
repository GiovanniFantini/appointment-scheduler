namespace AppointmentScheduler.Shared.Helpers;

public sealed class SubscriptionLimitException(string message) : Exception(message);
