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
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Handled { get; set; }

    // Orden creada a partir de esta solicitud (si la oficina la convirtió). Deja
    // rastro de la conversión y evita crear dos órdenes por el mismo cliente.
    public int? ConvertedOrderId { get; set; }
    public Order? ConvertedOrder { get; set; }
}
