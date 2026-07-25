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

    // --- Enlace público de seguimiento (sin login) ---
    // Token aleatorio largo. Quien lo tenga ve SOLO estado y fecha: ni fotos, ni
    // documentos, ni direcciones, ni datos de otras órdenes. Se puede revocar
    // (poniéndolo a null) y deja de servir 30 días después de completar la orden.
    public string? TrackingToken { get; set; }
    public DateTime? TrackingTokenCreatedUtc { get; set; }
    public ApplicationUser? AssignedDriver { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
