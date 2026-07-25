using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Lo que ve un cliente SIN cuenta al consultar su código. Igual de escueto que
// el enlace de seguimiento: estado y fecha, nada más.
public record GuestOrderStatus(
    string ReferenceCode,
    string CustomerName,
    DateTime RequestedAtUtc,
    string? ServiceType,
    string? OriginZone,
    string? DestinationZone,
    bool Converted,          // false = solicitud recibida, aún sin orden
    int? OrderId,
    OrderStatus? Status,
    string? DriverFirstName);

public interface IGuestLookupService
{
    // Código único e irrepetible para una solicitud nueva.
    Task<string> GenerateReferenceCodeAsync();

    // Resuelve código + teléfono. Devuelve null si no casan: NUNCA se distingue
    // "código inexistente" de "teléfono incorrecto" (evita enumerar códigos).
    Task<GuestOrderStatus?> LookupAsync(string referenceCode, string phone);

    // Normaliza un teléfono a solo dígitos para comparar formatos distintos
    // ("787-555-1234", "(787) 555 1234", "+1 787 555 1234").
    static string NormalizePhone(string? phone) =>
        new((phone ?? "").Where(char.IsDigit).ToArray());
}
