namespace BarrieraMoving.Shared.Dtos;

public record DirectMessageDto(
    int Id,
    int ConversationId,
    string Content,
    DateTime CreatedAt,
    string SenderUserId,
    string? SenderName,
    string? SenderRole,
    DateTime? CapturedAtUtc);

public record DirectConversationDto(
    int Id,
    List<UserSummaryDto> Participants,
    DirectMessageDto? LastMessage);

public record StartConversationRequest(string OtherUserId);

public record SendDirectMessageRequest(
    string Content,
    DateTime? CapturedAtUtc = null,
    string? IdempotencyKey = null);
