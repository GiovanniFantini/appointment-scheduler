using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio per la gestione della bacheca aziendale
/// </summary>
public class BoardMessageService : IBoardMessageService
{
    private readonly ApplicationDbContext _context;

    public BoardMessageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardMessageDto>> GetMerchantMessagesAsync(int merchantId)
    {
        var messages = await _context.BoardMessages
            .Include(m => m.Author)
            .Include(m => m.ReadReceipts)
            .Where(m => m.MerchantId == merchantId && m.IsActive)
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => MapToDto(m));
    }

    public async Task<IEnumerable<BoardMessageDto>> GetActiveMessagesForEmployeeAsync(int merchantId, int employeeId)
    {
        var now = DateTime.UtcNow;

        var messages = await _context.BoardMessages
            .Include(m => m.Author)
            .Include(m => m.ReadReceipts)
            .Where(m => m.MerchantId == merchantId
                && m.IsActive
                && (m.ExpiresAt == null || m.ExpiresAt > now))
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => MapToDto(m, employeeId));
    }

    public async Task<BoardMessageDto?> GetMessageByIdAsync(int id, int? employeeId = null)
    {
        var message = await _context.BoardMessages
            .Include(m => m.Author)
            .Include(m => m.ReadReceipts)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (message == null) return null;

        return MapToDto(message, employeeId);
    }

    public async Task<BoardMessageDto> CreateMessageAsync(int merchantId, int authorUserId, CreateBoardMessageRequest request)
    {
        var message = new BoardMessage
        {
            MerchantId = merchantId,
            AuthorUserId = authorUserId,
            Title = request.Title,
            Content = request.Content,
            Priority = request.Priority,
            Category = request.Category,
            IsPinned = request.IsPinned,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BoardMessages.Add(message);
        await _context.SaveChangesAsync();

        // Reload with includes
        var created = await _context.BoardMessages
            .Include(m => m.Author)
            .Include(m => m.ReadReceipts)
            .FirstAsync(m => m.Id == message.Id);

        return MapToDto(created);
    }

    public async Task<BoardMessageDto?> UpdateMessageAsync(int id, int merchantId, UpdateBoardMessageRequest request)
    {
        var message = await _context.BoardMessages
            .Include(m => m.Author)
            .Include(m => m.ReadReceipts)
            .FirstOrDefaultAsync(m => m.Id == id && m.MerchantId == merchantId && m.IsActive);

        if (message == null) return null;

        message.Title = request.Title;
        message.Content = request.Content;
        message.Priority = request.Priority;
        message.Category = request.Category;
        message.IsPinned = request.IsPinned;
        message.ExpiresAt = request.ExpiresAt;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<bool> DeleteMessageAsync(int id, int merchantId)
    {
        var message = await _context.BoardMessages
            .FirstOrDefaultAsync(m => m.Id == id && m.MerchantId == merchantId);

        if (message == null) return false;

        message.IsActive = false;
        message.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAsReadAsync(int messageId, int employeeId)
    {
        // Check if already read
        var existing = await _context.BoardMessageReads
            .FirstOrDefaultAsync(r => r.BoardMessageId == messageId && r.EmployeeId == employeeId);

        if (existing != null) return true; // Already read

        var readReceipt = new BoardMessageRead
        {
            BoardMessageId = messageId,
            EmployeeId = employeeId,
            ReadAt = DateTime.UtcNow
        };

        _context.BoardMessageReads.Add(readReceipt);
        await _context.SaveChangesAsync();

        return true;
    }

    private static BoardMessageDto MapToDto(BoardMessage message, int? currentEmployeeId = null)
    {
        return new BoardMessageDto
        {
            Id = message.Id,
            MerchantId = message.MerchantId,
            AuthorUserId = message.AuthorUserId,
            AuthorName = message.Author != null
                ? $"{message.Author.FirstName} {message.Author.LastName}"
                : "Sconosciuto",
            Title = message.Title,
            Content = message.Content,
            Priority = message.Priority,
            PriorityName = message.Priority switch
            {
                0 => "Normale",
                1 => "Importante",
                2 => "Urgente",
                _ => "Normale"
            },
            Category = message.Category,
            IsPinned = message.IsPinned,
            ExpiresAt = message.ExpiresAt,
            IsActive = message.IsActive,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            ReadCount = message.ReadReceipts?.Count ?? 0,
            IsReadByCurrentUser = currentEmployeeId.HasValue
                && message.ReadReceipts != null
                && message.ReadReceipts.Any(r => r.EmployeeId == currentEmployeeId.Value)
        };
    }
}
