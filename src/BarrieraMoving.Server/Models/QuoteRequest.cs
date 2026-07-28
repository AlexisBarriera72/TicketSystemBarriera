namespace BarrieraMoving.Server.Models;

// Solicitud de cotización enviada desde el sitio público. Se GUARDA siempre en la
// base de datos (nunca se pierde un cliente), y ADEMÁS se envía por email a la
// oficina cuando el SMTP está configurado.
public class QuoteRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? ServiceType { get; set; }
    public string? OriginZone { get; set; }
    public string? DestinationZone { get; set; }
    public string? PreferredDate { get; set; }
    public string? Details { get; set; }

    // Piso y ascensor ("3er piso · sin ascensor"). Cambia el personal y el precio,
    // así que se pregunta aparte y no enterrado en Details.
    public string? Floor { get; set; }

    // Artículos escogidos del catálogo, separados por "|" (ver MovingItems).
    // Se guarda el texto del catálogo, no un id: si mañana cambia la lista, las
    // cotizaciones viejas siguen diciendo lo que el cliente marcó ese día.
    public string? Items { get; set; }

    // Fotos que adjuntó el cliente (opcional). Nunca se sirven como estáticos.
    public ICollection<QuotePhoto> Photos { get; set; } = new List<QuotePhoto>();

    // Usuario que reclamó esta solicitud al crearse una cuenta con su código de
    // referencia. Desde ese momento la mudanza le pertenece: si la oficina la
    // convierte en orden, la orden nace ya a su nombre y le aparece en el portal.
    // null = sigue siendo una solicitud de invitado.
    public string? ClaimedByUserId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    // Código que se le da al cliente SIN cuenta para consultar su mudanza
    // (formato TC-XXXXXX). Aleatorio, no correlativo: un número de orden
    // secuencial dejaría adivinar los de los demás clientes.
    public string? ReferenceCode { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Handled { get; set; }

    // Orden creada a partir de esta solicitud (si la oficina la convirtió). Deja
    // rastro de la conversión y evita crear dos órdenes por el mismo cliente.
    public int? ConvertedOrderId { get; set; }
    public Order? ConvertedOrder { get; set; }
}
