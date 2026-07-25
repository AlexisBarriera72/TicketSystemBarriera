namespace BarrieraMoving.Shared.Dtos;

// Un elemento del feed de notificaciones (derivado de datos existentes, sin tabla propia).
// Type: "assignment" | "order-message" | "dm". El badge de "no leídos" lo calcula el
// cliente comparando CreatedAtUtc contra una marca "visto por última vez" local.
public record NotificationItemDto(
    string Type,
    string Title,
    string Body,
    DateTime CreatedAtUtc,
    int? OrderId = null,
    int? ConversationId = null);

// Una orden que necesita acción de la oficina (aprobar firma) o tiene papeleo
// pendiente. Pending = lista legible de lo que falta ("Firma pendiente de aprobar",
// "Papeleo rechazado: DNI", "Falta papeleo: Contrato").
public record ApprovalItemDto(
    int OrderId,
    string OrderTitle,
    string? DriverName,
    string Status,
    bool SignaturePending,
    DateTime? SignedAtUtc,
    List<string> Pending);
