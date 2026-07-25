namespace BarrieraMoving.Shared.Dtos;

public record UserSummaryDto(
    string Id,
    string? DisplayName,
    string? Email,
    IReadOnlyList<string> Roles);
