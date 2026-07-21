namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

// Reclamación / atención al cliente. La crea el cliente; la oficina responde y
// la marca resuelta. ACL: el cliente ve SOLO las suyas; el personal ve todas.
public class Complaint
{
    public int Id { get; set; }
    public required string ClientUserId { get; set; }
    public ApplicationUser? Client { get; set; }

    // Opcionalmente ligada a una orden concreta
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public required string Subject { get; set; }
    public required string Description { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? OfficeResponse { get; set; }
    public string? RespondedByUserId { get; set; }
    public ApplicationUser? RespondedBy { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}
