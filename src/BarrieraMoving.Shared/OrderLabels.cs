using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Shared;

// Nombres en español de los estados y prioridades. Los enums se quedan en inglés
// (son identidad de código y se guardan como int), pero al usuario NUNCA debe
// llegarle "Requested" ni "Medium": la app está en español de cabo a rabo.
// Fuente única para web y móvil, que si no cada pantalla se inventa los suyos.
public static class OrderLabels
{
    public static string Status(OrderStatus s) => s switch
    {
        OrderStatus.Requested => "Solicitada",
        OrderStatus.Assigned => "Asignada",
        OrderStatus.EnRoute => "En camino",
        OrderStatus.InProgress => "En curso",
        OrderStatus.PendingSignature => "Pendiente de firma",
        OrderStatus.Completed => "Completada",
        _ => s.ToString(),
    };

    // Qué significa el estado, en una línea. Va debajo del nombre para que nadie
    // tenga que adivinar la diferencia entre "En camino" y "En curso".
    public static string StatusHint(OrderStatus s) => s switch
    {
        OrderStatus.Requested => "Recibida, todavía sin equipo asignado.",
        OrderStatus.Assigned => "Ya tiene conductor asignado.",
        OrderStatus.EnRoute => "El equipo va de camino.",
        OrderStatus.InProgress => "La mudanza se está haciendo ahora.",
        OrderStatus.PendingSignature => "Falta la firma del cliente y el visto bueno de oficina.",
        OrderStatus.Completed => "Terminada y cerrada.",
        _ => "",
    };

    public static string Priority(OrderPriority p) => p switch
    {
        OrderPriority.Low => "Baja",
        OrderPriority.Medium => "Normal",
        OrderPriority.High => "Alta",
        OrderPriority.Urgent => "Urgente",
        _ => p.ToString(),
    };

    // Recargo por urgencia. Se muestra SIEMPRE junto a la opción "Urgente" para que
    // nadie la escoja pensando que es gratis y se entere al recibir la factura.
    public const decimal UrgentSurcharge = 40.00m;

    public static string? PriorityNote(OrderPriority p) =>
        p == OrderPriority.Urgent ? $"(${UrgentSurcharge:0.00})" : null;

    // Etiqueta lista para un <option>: "Urgente ($40.00)"
    public static string PriorityWithNote(OrderPriority p)
    {
        var note = PriorityNote(p);
        return note is null ? Priority(p) : $"{Priority(p)} {note}";
    }

    // Orden del flujo, para pintar el progreso de la mudanza paso a paso.
    public static readonly OrderStatus[] Flow =
    [
        OrderStatus.Requested,
        OrderStatus.Assigned,
        OrderStatus.EnRoute,
        OrderStatus.InProgress,
        OrderStatus.PendingSignature,
        OrderStatus.Completed,
    ];
}
