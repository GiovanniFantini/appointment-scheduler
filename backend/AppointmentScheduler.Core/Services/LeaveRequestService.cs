using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione delle richieste di permesso/ferie
/// </summary>
public class LeaveRequestService : ILeaveRequestService
{
    private readonly ApplicationDbContext _context;

    public LeaveRequestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetMerchantLeaveRequestsAsync(int merchantId)
    {
#pragma warning disable CS8602
        var requests = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.Merchant)
            .Where(lr => lr.MerchantId == merchantId)
            .OrderByDescending(lr => lr.CreatedAt)
            .ToListAsync();
#pragma warning restore CS8602

        return requests.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetEmployeeLeaveRequestsAsync(int employeeId)
    {
#pragma warning disable CS8602
        var requests = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.Merchant)
            .Where(lr => lr.EmployeeId == employeeId)
            .OrderByDescending(lr => lr.CreatedAt)
            .ToListAsync();
#pragma warning restore CS8602

        return requests.Select(MapToDto);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id)
    {
#pragma warning disable CS8602
        var request = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.Merchant)
            .FirstOrDefaultAsync(lr => lr.Id == id);
#pragma warning restore CS8602

        return request == null ? null : MapToDto(request);
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(int employeeId, CreateLeaveRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.Merchant)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            throw new InvalidOperationException("Dipendente non trovato");

        // Calcola i giorni richiesti (escludendo i weekend)
        var daysRequested = CalculateBusinessDays(request.StartDate, request.EndDate);

        // Verifica disponibilità giorni (solo per ferie, ROL, PAR, ex-festività)
        if (request.LeaveType != LeaveType.Malattia && request.LeaveType != LeaveType.Altro)
        {
            var currentYear = request.StartDate.Year;
            var balance = await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId
                    && b.LeaveType == request.LeaveType
                    && b.Year == currentYear);

            if (balance != null && balance.RemainingDays < daysRequested)
            {
                throw new InvalidOperationException(
                    $"Giorni insufficienti. Disponibili: {balance.RemainingDays}, Richiesti: {daysRequested}");
            }
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            MerchantId = employee.MerchantId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            DaysRequested = daysRequested,
            Status = LeaveRequestStatus.Pending,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        await _context.Entry(leaveRequest).Reference(lr => lr.Employee).LoadAsync();
        await _context.Entry(leaveRequest).Reference(lr => lr.Merchant).LoadAsync();

        return MapToDto(leaveRequest);
    }

    public async Task<LeaveRequestDto?> RespondToLeaveRequestAsync(int leaveRequestId, int merchantId, RespondToLeaveRequest request)
    {
#pragma warning disable CS8602
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.Merchant)
            .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
#pragma warning restore CS8602

        if (leaveRequest == null)
            return null;

        if (leaveRequest.MerchantId != merchantId)
            throw new InvalidOperationException("Non autorizzato a rispondere a questa richiesta");

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("La richiesta è già stata processata");

        leaveRequest.Status = request.Status;
        leaveRequest.ResponseNotes = request.ResponseNotes;
        leaveRequest.ApprovedByMerchantId = merchantId;
        leaveRequest.ResponseAt = DateTime.UtcNow;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        // Se approvata, aggiorna il saldo giorni
        if (request.Status == LeaveRequestStatus.Approved)
        {
            await UpdateLeaveBalance(leaveRequest);
        }

        await _context.SaveChangesAsync();

        return MapToDto(leaveRequest);
    }

    public async Task<bool> CancelLeaveRequestAsync(int leaveRequestId, int employeeId)
    {
        var leaveRequest = await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

        if (leaveRequest == null)
            return false;

        if (leaveRequest.EmployeeId != employeeId)
            throw new InvalidOperationException("Non autorizzato a cancellare questa richiesta");

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("Puoi cancellare solo richieste in attesa");

        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<EmployeeLeaveBalanceDto>> GetEmployeeLeaveBalancesAsync(int employeeId, int year)
    {
#pragma warning disable CS8602
        var balances = await _context.EmployeeLeaveBalances
            .Include(b => b.Employee)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .OrderBy(b => b.LeaveType)
            .ToListAsync();
#pragma warning restore CS8602

        return balances.Select(MapBalanceToDto);
    }

    public async Task<IEnumerable<EmployeeLeaveBalanceDto>> GetMerchantEmployeeLeaveBalancesAsync(int merchantId, int year)
    {
#pragma warning disable CS8602
        var balances = await _context.EmployeeLeaveBalances
            .Include(b => b.Employee)
            .Where(b => b.Employee.MerchantId == merchantId && b.Year == year)
            .OrderBy(b => b.Employee.LastName)
            .ThenBy(b => b.Employee.FirstName)
            .ThenBy(b => b.LeaveType)
            .ToListAsync();
#pragma warning restore CS8602

        return balances.Select(MapBalanceToDto);
    }

    public async Task<EmployeeLeaveBalanceDto> UpsertEmployeeLeaveBalanceAsync(int merchantId, UpsertEmployeeLeaveBalanceRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee == null)
            throw new InvalidOperationException("Dipendente non trovato");

        if (employee.MerchantId != merchantId)
            throw new InvalidOperationException("Non autorizzato a modificare i saldi di questo dipendente");

        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId
                && b.LeaveType == request.LeaveType
                && b.Year == request.Year);

        if (balance == null)
        {
            // Crea nuovo saldo
            balance = new EmployeeLeaveBalance
            {
                EmployeeId = request.EmployeeId,
                LeaveType = request.LeaveType,
                Year = request.Year,
                TotalDays = request.TotalDays,
                UsedDays = 0,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            _context.EmployeeLeaveBalances.Add(balance);
        }
        else
        {
            // Aggiorna saldo esistente
            balance.TotalDays = request.TotalDays;
            balance.Notes = request.Notes;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _context.Entry(balance).Reference(b => b.Employee).LoadAsync();

        return MapBalanceToDto(balance);
    }

    public async Task<bool> DeleteEmployeeLeaveBalanceAsync(int balanceId, int merchantId)
    {
        var balance = await _context.EmployeeLeaveBalances
            .Include(b => b.Employee)
            .FirstOrDefaultAsync(b => b.Id == balanceId);

        if (balance == null)
            return false;

        if (balance.Employee.MerchantId != merchantId)
            throw new InvalidOperationException("Non autorizzato a eliminare questo saldo");

        _context.EmployeeLeaveBalances.Remove(balance);
        await _context.SaveChangesAsync();

        return true;
    }

    // Helper methods

    private decimal CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            return 0;

        decimal days = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            // Esclude sabato e domenica
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return days;
    }

    private async Task UpdateLeaveBalance(LeaveRequest leaveRequest)
    {
        // Solo per ferie, ROL, PAR, ex-festività aggiorniamo il saldo
        if (leaveRequest.LeaveType == LeaveType.Malattia || leaveRequest.LeaveType == LeaveType.Altro)
            return;

        var year = leaveRequest.StartDate.Year;
        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId
                && b.LeaveType == leaveRequest.LeaveType
                && b.Year == year);

        if (balance != null)
        {
            balance.UsedDays += leaveRequest.DaysRequested;
            balance.UpdatedAt = DateTime.UtcNow;
        }
    }

    private LeaveRequestDto MapToDto(LeaveRequest leaveRequest)
    {
        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeName = $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}",
            MerchantId = leaveRequest.MerchantId,
            LeaveType = leaveRequest.LeaveType,
            LeaveTypeName = GetLeaveTypeName(leaveRequest.LeaveType),
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            DaysRequested = leaveRequest.DaysRequested,
            Status = leaveRequest.Status,
            StatusName = GetStatusName(leaveRequest.Status),
            Notes = leaveRequest.Notes,
            ResponseNotes = leaveRequest.ResponseNotes,
            ApprovedByMerchantId = leaveRequest.ApprovedByMerchantId,
            ResponseAt = leaveRequest.ResponseAt,
            CreatedAt = leaveRequest.CreatedAt,
            UpdatedAt = leaveRequest.UpdatedAt
        };
    }

    private EmployeeLeaveBalanceDto MapBalanceToDto(EmployeeLeaveBalance balance)
    {
        return new EmployeeLeaveBalanceDto
        {
            Id = balance.Id,
            EmployeeId = balance.EmployeeId,
            EmployeeName = $"{balance.Employee.FirstName} {balance.Employee.LastName}",
            LeaveType = balance.LeaveType,
            LeaveTypeName = GetLeaveTypeName(balance.LeaveType),
            Year = balance.Year,
            TotalDays = balance.TotalDays,
            UsedDays = balance.UsedDays,
            RemainingDays = balance.RemainingDays,
            Notes = balance.Notes,
            CreatedAt = balance.CreatedAt,
            UpdatedAt = balance.UpdatedAt
        };
    }

    private string GetLeaveTypeName(LeaveType leaveType)
    {
        return leaveType switch
        {
            LeaveType.Ferie => "Ferie",
            LeaveType.ROL => "ROL",
            LeaveType.PAR => "PAR",
            LeaveType.Malattia => "Malattia",
            LeaveType.ExFestivita => "Ex-festività",
            LeaveType.Welfare => "Welfare",
            LeaveType.Permesso => "Permesso",
            LeaveType.Altro => "Altro",
            _ => leaveType.ToString()
        };
    }

    private string GetStatusName(LeaveRequestStatus status)
    {
        return status switch
        {
            LeaveRequestStatus.Pending => "In attesa",
            LeaveRequestStatus.Approved => "Approvata",
            LeaveRequestStatus.Rejected => "Rifiutata",
            LeaveRequestStatus.Cancelled => "Cancellata",
            _ => status.ToString()
        };
    }
}
