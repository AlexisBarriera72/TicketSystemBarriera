using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Shared.Dtos;

public record OrderDto(
    int Id,
    string Title,
    string Description,
    OrderStatus Status,
    OrderPriority Priority,
    int CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string AuthorId,
    string? AuthorName,
    string? AssignedDriverId,
    string? AssignedDriverName);

public record MessageDto(
    int Id,
    int OrderId,
    string Content,
    DateTime CreatedAt,
    string UserId,
    string? UserName,
    bool IsSystem,
    string? SenderRole,
    bool HasAttachment,
    double? Latitude,
    double? Longitude,
    DateTime? CapturedAtUtc);

public record CategoryDto(int Id, string Name, string? Description);

public record CreateOrderRequest(
    string Title,
    string Description,
    int CategoryId,
    OrderPriority Priority);

public record AssignDriverRequest(string DriverId);

public record UpdateStatusRequest(OrderStatus NewStatus);

// CapturedAtUtc = hora del dispositivo (metadato NO fiable, solo informativo);
// IdempotencyKey evita duplicados cuando la cola offline reintenta un envío
// que en realidad sí llegó (timeout tras commit).
public record CreateMessageRequest(
    string Content,
    DateTime? CapturedAtUtc = null,
    string? IdempotencyKey = null);
