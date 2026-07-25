using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Shared.Dtos;

public record PaperworkDocumentDto(
    int Id,
    int OrderId,
    string SlotKey,
    PaperworkStatus Status,
    bool IsPdf,
    DateTime CreatedAtUtc,
    DateTime? CapturedAtUtc,
    double? Latitude,
    double? Longitude,
    string ContentHash,
    string? RejectReason,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc);

// Estado de UN slot configurado para una orden: definición + documento actual (si hay).
// El móvil pinta la lista de casillas con esto en una sola llamada.
public record PaperworkSlotStateDto(
    string Key,
    string Label,
    bool Required,
    PaperworkDocumentDto? Current);
