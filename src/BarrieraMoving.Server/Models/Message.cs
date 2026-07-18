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
}
