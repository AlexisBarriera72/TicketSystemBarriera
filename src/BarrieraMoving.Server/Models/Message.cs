namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;

// Mensaje del chat de una orden (antes "TicketComment")
public class Message
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Rol del remitente EN EL MOMENTO de enviar. Los roles cambian (conductor
    // ascendido a oficina); el historial del chat no debe reetiquetarse.
    public string? SenderRole { get; set; }

    // true = mensaje de sistema/auditoría (cambios de estado, asignaciones)
    public bool IsSystem { get; set; }
}
