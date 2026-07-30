namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

// Una orden = un trabajo de mudanza (antes "Ticket" en el sistema de soporte)
public class Order
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Requested;
    public OrderPriority Priority { get; set; } = OrderPriority.Medium;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }
    public string? AssignedDriverId { get; set; }

    // --- Datos del cliente ---
    // Muchas mudanzas las pide alguien que NO tiene cuenta (llama por teléfono o
    // llega por el formulario web). Antes esos datos se metían a mano dentro de
    // Description; en campos propios se pueden buscar, exportar y mostrar bien.
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }

    // --- Ruta y fecha ---
    public string? OriginZone { get; set; }
    public string? DestinationZone { get; set; }
    // DateTime (y no texto como en QuoteRequest): aquí sí hace falta ordenar y
    // filtrar por fecha ("mudanzas de hoy"), que con una cadena no se puede.
    public DateTime? ScheduledDate { get; set; }

    // --- Datos de carga (mismo formato que la solicitud de cotización) ---
    // Piso y ascensor ("3er piso · sin ascensor"): el conductor lo ve antes de salir.
    public string? Floor { get; set; }
    // Artículos del catálogo separados por "|" (ver MovingItems). Campos propios y
    // no texto libre para que lo que marcó el cliente llegue intacto a la cuadrilla.
    public string? Items { get; set; }

    // --- Enlace público de seguimiento (sin login) ---
    // Token aleatorio largo. Quien lo tenga ve SOLO estado y fecha: ni fotos, ni
    // documentos, ni direcciones, ni datos de otras órdenes. Se puede revocar
    // (poniéndolo a null) y deja de servir 30 días después de completar la orden.
    public string? TrackingToken { get; set; }
    public DateTime? TrackingTokenCreatedUtc { get; set; }
    public ApplicationUser? AssignedDriver { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
