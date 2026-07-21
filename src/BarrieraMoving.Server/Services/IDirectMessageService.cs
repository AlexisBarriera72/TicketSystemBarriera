using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public interface IDirectMessageService
{
    // REGLA DE ACCESO ÚNICA: estás en la conversación o no existes para ella.
    Task<bool> IsParticipantAsync(int conversationId, string userId);

    // Solo Admin/Oficina inician (se valida en el endpoint); 1:1 dedupe
    Task<(DirectConversation? Conv, string? Error)> GetOrCreateAsync(string creatorUserId, string otherUserId);

    Task<List<DirectConversation>> GetMineAsync(string userId);
    Task<DirectMessage?> GetLastMessageAsync(int conversationId);
    Task<List<DirectMessage>> GetMessagesAsync(int conversationId, int take = 50, int? beforeId = null, int? afterId = null);

    Task<(DirectMessage? Message, string? Error)> SendAsync(int conversationId, string senderUserId,
        string? senderRole, string content, DateTime? capturedAtUtc, string? idempotencyKey);
}
