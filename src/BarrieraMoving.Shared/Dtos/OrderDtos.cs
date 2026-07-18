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
    bool IsSystem);

public record CategoryDto(int Id, string Name, string? Description);

public record CreateOrderRequest(
    string Title,
    string Description,
    int CategoryId,
    OrderPriority Priority);

public record AssignDriverRequest(string DriverId);

public record UpdateStatusRequest(OrderStatus NewStatus);

public record CreateMessageRequest(string Content);
