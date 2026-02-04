using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Interfaccia per il servizio di gestione della bacheca aziendale
/// </summary>
public interface IBoardMessageService
{
    Task<IEnumerable<BoardMessageDto>> GetMerchantMessagesAsync(int merchantId);
    Task<IEnumerable<BoardMessageDto>> GetActiveMessagesForEmployeeAsync(int merchantId, int employeeId);
    Task<BoardMessageDto?> GetMessageByIdAsync(int id, int? employeeId = null);
    Task<BoardMessageDto> CreateMessageAsync(int merchantId, int authorUserId, CreateBoardMessageRequest request);
    Task<BoardMessageDto?> UpdateMessageAsync(int id, int merchantId, UpdateBoardMessageRequest request);
    Task<bool> DeleteMessageAsync(int id, int merchantId);
    Task<bool> MarkAsReadAsync(int messageId, int employeeId);
}
