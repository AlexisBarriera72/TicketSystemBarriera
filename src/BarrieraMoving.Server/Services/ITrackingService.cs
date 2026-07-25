using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Lo ÚNICO que ve quien abre el enlace público de seguimiento. Deliberadamente
// mínimo: sin direcciones, sin teléfono del cliente, sin fotos ni documentos.
public record TrackingInfo(
    int OrderId,
    string Title,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? DriverFirstName);

public interface ITrackingService
{
    // Crea (o devuelve) el token de una orden. Solo lo llama la oficina.
    Task<string?> EnsureTokenAsync(int orderId);

    // Revoca el enlace: deja de funcionar inmediatamente.
    Task RevokeAsync(int orderId);

    Task<string?> GetTokenAsync(int orderId);

    // Resuelve un token. Devuelve null si no existe, fue revocado o caducó.
    Task<TrackingInfo?> ResolveAsync(string token);
}
