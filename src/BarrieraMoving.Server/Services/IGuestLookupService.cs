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

// Resultado de intentar vincular una cotización de invitado a una cuenta recién
// creada. Se distingue "ya reclamada" de "no casa" a propósito: la primera le pasa
// a alguien que SÍ demostró saber código y teléfono, y merece una explicación.
public enum ClaimResult
{
    Ok,
    NotFound,        // el código y el teléfono no casan (no se dice cuál falla)
    AlreadyClaimed,  // otra cuenta ya la reclamó
}

public interface IGuestLookupService
{
    // Código único e irrepetible para una solicitud nueva.
    Task<string> GenerateReferenceCodeAsync();

    // Resuelve código + teléfono. Devuelve null si no casan: NUNCA se distingue
    // "código inexistente" de "teléfono incorrecto" (evita enumerar códigos).
    Task<GuestOrderStatus?> LookupAsync(string referenceCode, string phone);

    // Vincula una solicitud de invitado a una cuenta. Exige código Y teléfono por
    // el mismo motivo que LookupAsync: el código viaja por WhatsApp y capturas de
    // pantalla, así que por sí solo no puede bastar para adueñarse de una mudanza.
    // Si la solicitud ya se convirtió en orden, la orden cambia de dueño para que
    // aparezca en "Mis órdenes".
    Task<ClaimResult> ClaimAsync(string referenceCode, string phone, string userId);

    // Normaliza un teléfono a solo dígitos para comparar formatos distintos
    // ("787-555-1234", "(787) 555 1234", "+1 787 555 1234").
    static string NormalizePhone(string? phone) =>
        new((phone ?? "").Where(char.IsDigit).ToArray());
}
