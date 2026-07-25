using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public interface ISignatureService
{
    // Ruta ONLINE: crea el sobre en el proveedor → documento AwaitingSignature
    Task<(SignatureDocument? Doc, string? SigningUrl, string? Error)> CreateProviderRequestAsync(
        int orderId, string requestedByUserId, string signerName);

    // Ruta OFFLINE (ceremonia en el dispositivo, llega por la cola del móvil)
    Task<(SignatureDocument? Doc, string? Error)> CreateOfflineSignedAsync(
        int orderId, string requestedByUserId, string signerName, byte[] signaturePng,
        double? latitude, double? longitude, DateTime? capturedAtUtc, string? idempotencyKey);

    // Webhook del proveedor: sobre firmado → espejar PDF → Signed → email
    Task<bool> HandleEnvelopeCompletedAsync(string envelopeId);

    Task<List<SignatureDocument>> GetForOrderAsync(int orderId);
    Task<SignatureDocument?> GetWithOrderAsync(int documentId);
    Task<List<SignatureDocument>> GetPendingReviewAsync();

    Task<(bool Ok, string? Error)> ApproveAsync(int documentId, string reviewerUserId);
    Task<(bool Ok, string? Error)> RejectAsync(int documentId, string reviewerUserId, string reason);
    Task<(bool Ok, string? Error)> ResendEmailAsync(int documentId);
}
