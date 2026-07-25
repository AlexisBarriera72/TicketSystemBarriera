using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Shared.Dtos;

public record SignatureDocumentDto(
    int Id,
    int OrderId,
    SignatureDocStatus Status,
    bool IsProvisional,          // true = firmado sin conexión (ceremonia en el dispositivo)
    string? SignerName,
    DateTime CreatedAtUtc,
    DateTime? SignedAtUtc,       // hora del SERVIDOR al recibir la firma
    DateTime? SignedCapturedAtUtc, // hora del dispositivo (metadato, no fiable)
    double? Latitude,
    double? Longitude,
    string? ContentHash,
    string? RejectReason,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    EmailDeliveryStatus EmailStatus);

public record RejectDocumentRequest(string Reason);

public record CreateSignatureRequest(string SignerName);

public record CreateSignatureResponse(int DocumentId, string SigningUrl);
