using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Shared.Dtos;

public record ComplaintDto(
    int Id,
    string Subject,
    string Description,
    ComplaintStatus Status,
    int? OrderId,
    string ClientUserId,
    string? ClientName,
    DateTime CreatedAtUtc,
    string? OfficeResponse,
    string? RespondedByName,
    DateTime? RespondedAtUtc);

public record CreateComplaintRequest(string Subject, string Description, int? OrderId = null);

public record RespondComplaintRequest(string Response, bool Resolve);
