using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppointmentScheduler.API.Middleware;

public sealed class ActivityResultFilter(ActivityContext activity) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Il login parte anonimo: l'identità confermata arriva dal servizio, mai dal body della request.
        if (context.Result is ObjectResult { Value: AuthResponse response } && response.UserId > 0)
        {
            activity.UserId = response.UserId;
            activity.MerchantId = response.MerchantId;
        }
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
